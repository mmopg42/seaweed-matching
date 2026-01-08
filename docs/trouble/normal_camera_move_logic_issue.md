# 일반 카메라 이동 로직 문제 분석 및 수정 방안

## 문서 정보
- **작성일**: 2025-01-06
- **상태**: 분석 완료
- **우선순위**: 높음

## 문제 개요

일반 카메라(NormalFolder) 데이터를 이동할 때, 원본 폴더명이 목적지 경로에 포함되지 않는 문제가 있습니다. 이로 인해 여러 그룹의 일반 카메라 데이터가 같은 폴더에 병합되거나 덮어씌워질 수 있습니다.

## 현재 동작 방식

### 1. 이동 로직 흐름

**파일**: `ChronoView/Core/FileOperations/FileGroupOperator.cs`

```27:37:ChronoView/Core/FileOperations/FileGroupOperator.cs
if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
    string? folderName = null;
    if (schema == PathSchema.QuarantineSchema)
    {
        folderName = Path.GetFileName(group.NormalFolder);
    }
    
    var destPath = BuildPath(targetBase, group, role, schema, null, folderName);
    _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
}
```

### 2. 경로 생성 로직

**파일**: `ChronoView/Core/FileOperations/FileGroupOperator.cs`

```91:109:ChronoView/Core/FileOperations/FileGroupOperator.cs
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
{
    var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
    if (schema == PathSchema.QuarantineSchema) {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseQuarantinePath = Path.Combine(basePath, today, subj, $"Line{group.LineNumber}", role);
        if (!string.IsNullOrEmpty(folderName))
        {
            return Path.Combine(baseQuarantinePath, folderName);
        }
        
        return baseQuarantinePath;
    } else {
        var nir = group.HasNir ? "with NIR" : "without NIR";
        if (role.StartsWith("cam")) return Path.Combine(basePath, subj, nir, "복합 카메라", role);
        if (role == "일반" || role == "일반2") return Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
        return Path.Combine(basePath, subj, nir, role);
    }
}
```

### 3. 현재 동작 분석

#### MoveSchema (일반 이동)의 경우

1. **role 결정**:
   - Line 1: `role = "일반"`
   - Line 2: `role = "일반2"`

2. **folderName 처리**:
   - `schema == PathSchema.QuarantineSchema`일 때만 `folderName` 추출
   - **문제**: `MoveSchema`일 때는 `folderName`이 `null`로 유지됨

3. **경로 생성** (`BuildPath`):
   - `schema != QuarantineSchema`이므로 `else` 블록 실행
   - `role == "일반"` 또는 `role == "일반2"`인 경우:
     ```csharp
     if (group.HasNir) 
         return Path.Combine(basePath, subj, nir, role);  // 예: ".../with NIR/일반"
     else 
         return Path.Combine(basePath, subj, nir, $"{role} 카메라");  // 예: ".../without NIR/일반 카메라"
     ```
   - **문제**: `folderName` 파라미터가 전달되더라도 사용되지 않음

4. **실제 이동**:
   - 원본: `C:\data\normal\C251203T155910_0`
   - 목적지: `{outputPath}\{subject}\{with/without NIR}\일반` 또는 `{outputPath}\{subject}\{with/without NIR}\일반 카메라`
   - **문제**: 원본 폴더명(`C251203T155910_0`)이 경로에 포함되지 않음

#### QuarantineSchema (삭제)의 경우

1. **role 결정**:
   - Line 1: `role = "일반1"`
   - Line 2: `role = "일반2"`

2. **folderName 처리**:
   - `folderName = Path.GetFileName(group.NormalFolder)` 추출
   - **정상**: `folderName`이 경로에 포함됨

3. **경로 생성**:
   - `{quarantinePath}\{오늘날짜}\{subject}\Line{라인번호}\{role}\{folderName}`
   - 예: `C:\trash\20250106\UnknownSubject\Line1\일반1\C251203T155910_0`
   - **정상**: 원본 폴더명이 포함됨

## 문제점 상세

### 문제 1: MoveSchema에서 원본 폴더명 누락

**현재 동작**:
```
원본: C:\data\normal\C251203T155910_0
목적지: {outputPath}\{subject}\with NIR\일반
```

**결과**:
- 여러 그룹의 일반 카메라 데이터가 같은 폴더(`일반`)로 이동
- 원본 폴더명이 사라짐
- 폴더 내 파일들이 병합되거나 덮어씌워질 수 있음

**예상되는 문제 시나리오**:
1. 그룹 A: `C251203T155910_0` → `{outputPath}\...\일반`로 이동
2. 그룹 B: `C251203T160000_0` → `{outputPath}\...\일반`로 이동
3. 결과: 두 폴더의 내용이 같은 위치에 병합되거나, 나중에 이동한 폴더가 먼저 이동한 폴더를 덮어씀

### 문제 2: BuildPath에서 folderName 파라미터 미사용

**현재 코드**:
```csharp
if (role == "일반" || role == "일반2") 
    return Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
```

**문제**:
- `folderName` 파라미터가 전달되더라도 `MoveSchema`에서는 사용되지 않음
- `QuarantineSchema`에서만 `folderName`이 경로에 포함됨

### 문제 3: 일관성 부족

**QuarantineSchema (삭제)**:
- 원본 폴더명 포함: `{quarantinePath}\...\일반1\C251203T155910_0` ✅

**MoveSchema (이동)**:
- 원본 폴더명 누락: `{outputPath}\...\일반` ❌

## 수정 방안

### 방안 1: MoveSchema에서도 folderName 포함 (권장)

**수정 위치**: `FileGroupOperator.ExecuteOpAsync`

```csharp
if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
    
    // MoveSchema에서도 folderName 추출
    string? folderName = null;
    if (schema == PathSchema.QuarantineSchema || schema == PathSchema.MoveSchema)
    {
        folderName = Path.GetFileName(group.NormalFolder);
    }
    
    var destPath = BuildPath(targetBase, group, role, schema, null, folderName);
    _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
}
```

**수정 위치**: `FileGroupOperator.BuildPath`

```csharp
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
{
    var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
    if (schema == PathSchema.QuarantineSchema) {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseQuarantinePath = Path.Combine(basePath, today, subj, $"Line{group.LineNumber}", role);
        if (!string.IsNullOrEmpty(folderName))
        {
            return Path.Combine(baseQuarantinePath, folderName);
        }
        
        return baseQuarantinePath;
    } else {
        var nir = group.HasNir ? "with NIR" : "without NIR";
        if (role.StartsWith("cam")) return Path.Combine(basePath, subj, nir, "복합 카메라", role);
        
        // MoveSchema에서 일반 카메라 경로 생성 시 folderName 포함
        if (role == "일반" || role == "일반2") 
        {
            var baseRolePath = Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
            if (!string.IsNullOrEmpty(folderName))
            {
                return Path.Combine(baseRolePath, folderName);
            }
            return baseRolePath;
        }
        
        return Path.Combine(basePath, subj, nir, role);
    }
}
```

**수정 후 동작**:
```
원본: C:\data\normal\C251203T155910_0
목적지: {outputPath}\{subject}\with NIR\일반\C251203T155910_0
```

**장점**:
- 원본 폴더명이 보존됨
- 여러 그룹의 데이터가 서로 다른 폴더로 이동하여 충돌 방지
- QuarantineSchema와 일관성 유지

### 방안 2: MoveDirectoryAtomicAsync에서 병합 로직 개선

현재 `MoveDirectoryAtomicAsync`는 목적지 폴더가 존재하면 병합하는 로직이 있습니다:

```119:145:ChronoView/Core/FileOperations/FileGroupOperator.cs
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
                File.Delete(file);
            }
            foreach (var subDir in Directory.GetDirectories(src)) {
                var destSubDir = Path.Combine(dest, Path.GetFileName(subDir));
                Directory.Move(subDir, destSubDir);
            }
            Directory.Delete(src);
        } else {
            Directory.Move(src, dest);
        }
    }, ct);
    tracking.Add((src, dest, true));
    _logger.LogInformation("Moved directory: {Src} -> {Dest}", src, dest);
}
```

**문제**: 목적지 폴더가 존재하면 병합하는데, `MoveSchema`에서 원본 폴더명이 경로에 포함되지 않으면 항상 같은 폴더로 이동하여 병합이 발생합니다.

**해결**: 방안 1을 적용하면 원본 폴더명이 경로에 포함되어 병합 문제가 해결됩니다.

## 수정 후 예상 결과

### 수정 전 (현재)
```
원본: C:\data\normal\C251203T155910_0
목적지: {outputPath}\{subject}\with NIR\일반
→ 여러 그룹이 같은 폴더로 이동하여 충돌 발생 가능
```

### 수정 후
```
원본: C:\data\normal\C251203T155910_0
목적지: {outputPath}\{subject}\with NIR\일반\C251203T155910_0
→ 각 그룹이 고유한 폴더로 이동하여 충돌 방지
```

## 테스트 시나리오

### 시나리오 1: MoveSchema - Line 1, HasNir = true
- **입력**: 
  - `group.NormalFolder = "C:\data\normal\C251203T155910_0"`
  - `group.LineNumber = 1`
  - `group.HasNir = true`
  - `schema = PathSchema.MoveSchema`
- **예상 결과**: `{outputPath}\{subject}\with NIR\일반\C251203T155910_0`

### 시나리오 2: MoveSchema - Line 1, HasNir = false
- **입력**: 
  - `group.NormalFolder = "C:\data\normal\C251203T155910_0"`
  - `group.LineNumber = 1`
  - `group.HasNir = false`
  - `schema = PathSchema.MoveSchema`
- **예상 결과**: `{outputPath}\{subject}\without NIR\일반 카메라\C251203T155910_0`

### 시나리오 3: MoveSchema - Line 2, HasNir = true
- **입력**: 
  - `group.NormalFolder = "C:\data\normal\C251203T160000_1"`
  - `group.LineNumber = 2`
  - `group.HasNir = true`
  - `schema = PathSchema.MoveSchema`
- **예상 결과**: `{outputPath}\{subject}\with NIR\일반2\C251203T160000_1`

### 시나리오 4: QuarantineSchema (기존 동작 유지)
- **입력**: 
  - `group.NormalFolder = "C:\data\normal\C251203T155910_0"`
  - `group.LineNumber = 1`
  - `schema = PathSchema.QuarantineSchema`
- **예상 결과**: `{quarantinePath}\{오늘날짜}\{subject}\Line1\일반1\C251203T155910_0`

## 구현 체크리스트

- [ ] `FileGroupOperator.ExecuteOpAsync`에서 `MoveSchema`일 때도 `folderName` 추출
- [ ] `FileGroupOperator.BuildPath`에서 `MoveSchema`일 때 `folderName`을 경로에 포함
- [ ] 테스트 시나리오 실행 및 검증
- [ ] 로그 확인: 이동 경로에 원본 폴더명이 포함되는지 확인

## 참고 사항

- 이 수정은 `QuarantineSchema`의 동작에는 영향을 주지 않습니다.
- 다른 스키마(예: `CopySchema`)가 있다면 동일한 로직을 적용해야 할 수 있습니다.
- 기존에 이미 이동된 데이터는 수동으로 정리해야 할 수 있습니다.

