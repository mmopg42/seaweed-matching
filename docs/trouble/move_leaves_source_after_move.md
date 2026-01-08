# 이동 후 소스 파일/폴더 잔존 문제 분석

## 문서 정보
- **작성일**: 2025-01-06
- **상태**: 원인 분석 완료
- **우선순위**: 높음

## 문제 현상

이동 작업을 실행한 후, 일반 카메라 폴더, 복합 카메라 이미지 파일들, NIR 파일들이 소스 경로에 그대로 남아있습니다. 마치 복사 작업처럼 동작하여 소스 파일이 삭제되지 않습니다.

### 영향받는 데이터 타입
- **일반 카메라 폴더** (NormalFolder)
- **복합 카메라 이미지** (Cam1~6)
- **NIR 파일** (.spc, .txt, A.txt)

## 현재 구현 분석

### 1. 파일 이동 로직

**파일**: `ChronoView/Core/FileOperations/FileGroupOperator.cs`

**메서드**: `MoveFileAtomicAsync` (Line 123-129)

```csharp
private async Task MoveFileAtomicAsync(string src, string dest, List<(string, string, bool)> tracking, CancellationToken ct)
{
    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
    await Task.Run(() => File.Copy(src, dest, true), ct);
    tracking.Add((src, dest, false));
    File.Delete(src);  // ⚠️ 삭제 실패 시 소스 파일이 남음
}
```

**동작 방식**:
1. 목적지 디렉터리 생성
2. `File.Copy`로 파일 복사
3. 추적 목록에 추가
4. `File.Delete`로 소스 파일 삭제

**문제점**:
- `File.Delete(src)`가 실패하면 소스 파일이 남음
- 삭제 실패 시 예외가 발생하지 않으면 롤백되지 않음
- 파일이 다른 프로세스에 의해 잠겨있거나 권한 문제가 있을 수 있음

### 2. 디렉터리 이동 로직

**메서드**: `MoveDirectoryAtomicAsync` (Line 131-157)

```csharp
private async Task MoveDirectoryAtomicAsync(string src, string dest, List<(string, string, bool)> tracking, CancellationToken ct)
{
    // Ensure parent directory exists
    var parentDir = Path.GetDirectoryName(dest);
    if (!string.IsNullOrEmpty(parentDir)) Directory.CreateDirectory(parentDir);
    
    // Move entire directory at once (atomic if on same volume)
    await Task.Run(() => {
        if (Directory.Exists(dest)) {
            // If destination exists, merge by moving contents
            foreach (var file in Directory.GetFiles(src)) {
                var destFile = Path.Combine(dest, Path.GetFileName(file));
                File.Copy(file, destFile, true);
                File.Delete(file);  // ⚠️ 개별 파일 삭제 실패 가능
            }
            foreach (var subDir in Directory.GetDirectories(src)) {
                var destSubDir = Path.Combine(dest, Path.GetFileName(subDir));
                Directory.Move(subDir, destSubDir);
            }
            Directory.Delete(src);  // ⚠️ 소스 폴더 삭제 실패 가능
        } else {
            Directory.Move(src, dest);  // ⚠️ 다른 드라이브면 실패 가능
        }
    }, ct);
    tracking.Add((src, dest, true));
    _logger.LogInformation("Moved directory: {Src} -> {Dest}", src, dest);
}
```

**동작 방식**:

#### 케이스 1: 목적지 폴더가 존재하는 경우 (병합)
1. 소스 폴더 내 모든 파일을 복사 후 삭제 (Line 141-144)
2. 소스 폴더 내 모든 하위 폴더를 이동 (Line 146-148)
3. 빈 소스 폴더 삭제 (Line 150)

#### 케이스 2: 목적지 폴더가 없는 경우
1. `Directory.Move(src, dest)`로 원자적 이동

**문제점**:

1. **병합 시 개별 파일 삭제 실패** (Line 144):
   - 파일이 다른 프로세스에 의해 열려있으면 삭제 실패
   - 권한 문제로 삭제 실패
   - 일부 파일만 삭제되고 나머지는 소스에 남음

2. **소스 폴더 삭제 실패** (Line 150):
   - 폴더 내 파일이 일부 남아있으면 삭제 실패
   - 폴더가 다른 프로세스에 의해 사용 중이면 삭제 실패
   - 소스 폴더가 그대로 남음

3. **Directory.Move 실패** (Line 152):
   - 같은 드라이브 내에서는 원자적 이동이 가능
   - 다른 드라이브 간 이동 시 `Directory.Move`는 실패할 수 있음
   - 실패 시 예외가 발생하지만, 예외 처리 후에도 소스가 남을 수 있음

### 3. 예외 처리 및 롤백

**메서드**: `ExecuteOpAsync` (Line 21-59)

```csharp
public async Task<OperationResult> ExecuteOpAsync(...)
{
    var result = new OperationResult();
    var movedItems = new List<(string source, string dest, bool isDirectory)>();
    try {
        // ... 이동 작업들
        result.Success = true;
    } catch (Exception ex) {
        _logger.LogError(ex, "ExecuteOpAsync failed, rolling back {Count} items", movedItems.Count);
        await RollbackAsync(movedItems);
        result.Success = false; result.ErrorMessage = ex.Message;
    }
    return result;
}
```

**문제점**:
- `File.Delete`나 `Directory.Delete`가 실패해도 예외가 발생하지 않으면 롤백되지 않음
- 삭제 실패가 조용히 무시되면 소스 파일이 남음
- 롤백 로직이 있지만, 삭제 실패가 예외로 전파되지 않으면 작동하지 않음

**롤백 메서드**: `RollbackAsync` (Line 159-164)

```csharp
private async Task RollbackAsync(List<(string source, string dest, bool isDirectory)> items)
{
    foreach (var item in Enumerable.Reverse(items)) {
        try { 
            if (item.isDirectory) 
                Directory.Move(item.dest, item.source); 
            else { 
                File.Copy(item.dest, item.source, true); 
                File.Delete(item.dest); 
            } 
        } catch { }  // ⚠️ 롤백 실패도 조용히 무시
    }
}
```

**문제점**:
- 롤백 실패도 조용히 무시됨 (`catch { }`)
- 롤백 실패 시 사용자에게 알림이 없음

## 소스 잔존 발생 시나리오

### 시나리오 1: 파일이 다른 프로세스에 의해 잠김
- **원인**: 파일이 다른 애플리케이션(이미지 뷰어, 탐색기 등)에 의해 열려있음
- **결과**: `File.Delete` 실패 → 소스 파일 남음
- **재현**: 이미지 파일을 미리보기로 열어둔 상태에서 이동 실행

### 시나리오 2: 권한 문제
- **원인**: 소스 파일/폴더에 대한 삭제 권한이 없음
- **결과**: `File.Delete` 또는 `Directory.Delete` 실패 → 소스 남음
- **재현**: 읽기 전용 파일이나 보호된 폴더를 이동 시도

### 시나리오 3: 병합 시 충돌
- **원인**: 목적지 폴더가 이미 존재하고, 일부 파일만 성공적으로 이동됨
- **결과**: 일부 파일은 이동되었지만 나머지는 소스에 남음
- **재현**: 같은 이름의 폴더가 이미 존재하는 경우

### 시나리오 4: 다른 드라이브 간 이동
- **원인**: `Directory.Move`는 같은 드라이브에서만 원자적 이동 가능
- **결과**: 다른 드라이브 간 이동 시 실패 가능 → 소스 남음
- **재현**: C: 드라이브에서 D: 드라이브로 이동

### 시나리오 5: 디렉터리 내 파일이 일부 남음
- **원인**: 병합 과정에서 일부 파일 삭제 실패
- **결과**: 소스 폴더가 비어있지 않아 `Directory.Delete` 실패 → 소스 폴더 남음
- **재현**: 폴더 내 일부 파일이 잠겨있는 상태에서 이동

## 로그 확인 포인트

다음 로그를 확인하여 문제 원인을 파악할 수 있습니다:

1. **이동 시작 로그**:
   ```
   ExecuteOpAsync started: GroupId={GroupId}, TargetBase={TargetBase}, ...
   Moving NormalFolder: {Src} -> {Dest}
   Moving camera file {Cam}: {Src} -> {Dest}
   ```

2. **이동 완료 로그**:
   ```
   ExecuteOpAsync completed successfully: {Count} items moved
   Moved directory: {Src} -> {Dest}
   ```

3. **예외 발생 로그**:
   ```
   ExecuteOpAsync failed, rolling back {Count} items
   ```

4. **롤백 로그**: 현재는 롤백 실패가 조용히 무시되어 로그가 없음

## 임시 대응 방안

### 1. 수동 확인 및 정리
- 이동 작업 후 소스 경로를 확인하여 남은 파일/폴더를 수동으로 삭제
- 이동 전 파일이 다른 프로세스에 의해 열려있지 않은지 확인

### 2. 재시도
- 이동 실패 후 소스 파일이 남아있는 경우, 다시 이동을 시도
- 이번에는 파일이 잠겨있지 않을 수 있음

### 3. 파일 잠금 확인
- 이동 전에 파일이 다른 프로세스에 의해 사용 중인지 확인
- Windows의 경우 `FileShare` 옵션으로 확인 가능

## 개선 방안

### 1. 삭제 실패 시 예외 발생
- `File.Delete`와 `Directory.Delete` 실패 시 예외를 명시적으로 확인
- 삭제 실패 시 `IOException` 또는 `UnauthorizedAccessException` 발생

### 2. 재시도 로직 추가
- 삭제 실패 시 일정 시간 대기 후 재시도
- 최대 재시도 횟수 제한

### 3. 파일 잠금 확인
- 이동 전에 파일이 잠겨있는지 확인
- 잠겨있으면 사용자에게 알림

### 4. 원자적 이동 우선 사용
- 같은 드라이브 내에서는 `File.Move`와 `Directory.Move` 우선 사용
- 다른 드라이브 간 이동 시에만 Copy+Delete 사용

### 5. 롤백 로직 개선
- 롤백 실패 시에도 로그 기록
- 롤백 실패한 항목을 별도로 추적하여 사용자에게 알림

### 6. 상세한 에러 로깅
- 삭제 실패 시 구체적인 원인 로깅 (파일 잠김, 권한 문제 등)
- 사용자에게 명확한 에러 메시지 제공

## 참고 사항

- 현재 구현은 "Copy-then-Delete" 패턴을 사용하여 안정성을 높이려고 했지만, 삭제 단계에서 실패할 수 있음
- Windows에서는 파일이 다른 프로세스에 의해 열려있으면 삭제가 실패할 수 있음
- 네트워크 드라이브나 외장 드라이브에서는 권한 문제가 더 자주 발생할 수 있음

