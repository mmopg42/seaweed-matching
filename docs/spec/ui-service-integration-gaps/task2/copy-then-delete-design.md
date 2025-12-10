# Copy-then-Delete 방식 상세 설계

## 개요

파일 이동 작업의 안정성을 강화하기 위해 기존 `File.Move` / `Directory.Move` 방식을 **복사 후 삭제**(Copy-then-Delete) 방식으로 변경합니다.

**변경 일자**: 2025-12-10
**이유**: 네트워크 드라이브, 크로스 볼륨 이동, 부분 실패 시 복구 가능성 등 안정성 향상

---

## 기존 방식의 문제점

### 1. File.Move / Directory.Move의 한계

```csharp
// 기존 방식
File.Move(source, dest, overwrite: true);
Directory.Move(sourceDir, destDir);
```

**문제점**:
- **크로스 볼륨 이동**: 다른 드라이브 간 이동 시 실패 가능성 높음
- **네트워크 불안정**: 네트워크 드라이브 사용 시 중단 시 복구 어려움
- **부분 실패**: 폴더 이동 중 일부 파일 실패 시 원본/대상 모두 불완전 상태
- **원자성 부족**: 이동 중 중단되면 원본이 손실될 수 있음

---

## 새로운 방식: Copy-then-Delete

### 1. 기본 원칙

```
1. 복사 (Copy)     → 대상 경로에 파일/폴더 복사
2. 검증 (Verify)   → 복사가 완전히 성공했는지 검증
3. 삭제 (Delete)   → 검증 성공 시에만 원본 삭제
```

**장점**:
- ✅ 복사 실패 시 원본 보존
- ✅ 검증 단계를 통한 데이터 무결성 확인
- ✅ 크로스 볼륨, 네트워크 드라이브 안정성 향상
- ✅ 부분 실패 시 롤백 가능 (복사된 것만 삭제)

---

## 구현 설계

### 2.1 파일 이동 (File Move)

#### 기존 코드
```csharp
await Task.Run(() => File.Move(sourcePath, destPath, overwrite), cancellationToken);
```

#### 변경 후
```csharp
// 1. Copy
await Task.Run(() => File.Copy(sourcePath, destPath, overwrite: false), cancellationToken);

// 2. Verify
if (!VerifyFileCopy(sourcePath, destPath))
{
    throw new IOException($"File copy verification failed: {sourcePath}");
}

// 3. Delete original
await Task.Run(() => File.Delete(sourcePath), cancellationToken);
```

#### 검증 로직
```csharp
private bool VerifyFileCopy(string source, string dest)
{
    if (!File.Exists(dest))
        return false;

    var sourceInfo = new FileInfo(source);
    var destInfo = new FileInfo(dest);

    // Size check (fast)
    if (sourceInfo.Length != destInfo.Length)
        return false;

    // Optional: Timestamp check
    // if (Math.Abs((sourceInfo.LastWriteTimeUtc - destInfo.LastWriteTimeUtc).TotalSeconds) > 2)
    //     return false;

    return true;
}
```

---

### 2.2 폴더 이동 (Directory Move)

#### 기존 코드
```csharp
await Task.Run(() => Directory.Move(group.NormalFolder, destNormalPath), cancellationToken);
```

#### 변경 후
```csharp
// 1. Copy directory recursively
await CopyDirectoryAsync(group.NormalFolder, destNormalPath, cancellationToken);

// 2. Verify directory copy
if (!VerifyDirectoryCopy(group.NormalFolder, destNormalPath))
{
    throw new IOException($"Directory copy verification failed: {group.NormalFolder}");
}

// 3. Delete original directory
await Task.Run(() => Directory.Delete(group.NormalFolder, recursive: true), cancellationToken);
```

#### 재귀 복사 로직
```csharp
private async Task CopyDirectoryAsync(
    string sourceDir,
    string destDir,
    CancellationToken cancellationToken)
{
    // Create destination directory
    Directory.CreateDirectory(destDir);

    // Copy all files
    foreach (var file in Directory.GetFiles(sourceDir))
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileName = Path.GetFileName(file);
        var destFile = Path.Combine(destDir, fileName);

        await Task.Run(() => File.Copy(file, destFile, overwrite: false), cancellationToken);
    }

    // Copy all subdirectories recursively
    foreach (var subDir in Directory.GetDirectories(sourceDir))
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dirName = Path.GetFileName(subDir);
        var destSubDir = Path.Combine(destDir, dirName);

        await CopyDirectoryAsync(subDir, destSubDir, cancellationToken);
    }
}
```

#### 폴더 검증 로직
```csharp
private bool VerifyDirectoryCopy(string sourceDir, string destDir)
{
    if (!Directory.Exists(destDir))
        return false;

    // Check file count
    var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
    var destFiles = Directory.GetFiles(destDir, "*", SearchOption.AllDirectories);

    if (sourceFiles.Length != destFiles.Length)
        return false;

    // Check total size (optional but fast)
    long sourceSize = sourceFiles.Sum(f => new FileInfo(f).Length);
    long destSize = destFiles.Sum(f => new FileInfo(f).Length);

    if (sourceSize != destSize)
        return false;

    return true;
}
```

---

### 2.3 롤백 처리

Copy-then-Delete 방식에서는 롤백이 더 명확합니다:

#### 복사 단계에서 실패
- **원본**: 그대로 유지 (손실 없음)
- **대상**: 복사된 부분만 삭제 (Clean-up)

#### 검증 단계에서 실패
- **원본**: 그대로 유지
- **대상**: 불완전한 복사본 삭제

#### 삭제 단계에서 실패
- **원본**: 삭제 실패한 파일/폴더만 남음
- **대상**: 이미 완전히 복사됨
- **처리**: 경고 로그만 남기고 성공으로 처리 (데이터는 안전)

```csharp
// 삭제 실패 시 처리
try
{
    await Task.Run(() => File.Delete(sourcePath), cancellationToken);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to delete source file after copy: {Path}. File is safe at destination.", sourcePath);
    // Do not fail the operation - data is safe at destination
}
```

---

## 성능 고려사항

### 1. 디스크 I/O 증가
- **기존**: Move = 메타데이터 변경만 (빠름)
- **변경**: Copy + Verify + Delete = 실제 데이터 복사 (느림)

**대응**:
- 동일 볼륨 내 이동은 성능 저하 있음
- 하지만 안정성이 우선순위

### 2. 진행률 표시
복사와 삭제를 분리하여 더 정확한 진행률 표시 가능:

```csharp
// Progress reporting
progress?.Report(new OperationProgress
{
    TotalFiles = totalEstimate,
    ProcessedFiles = processedCount,
    CurrentFile = fileName,
    Status = "Copying"  // "Copying" → "Verifying" → "Cleaning up"
});
```

---

## 구현 체크리스트

### FileOperationService.cs 수정 ✅ COMPLETED (2025-12-10)

- [x] `MoveFileGroupAsync` 메서드 리팩토링
  - [x] 파일 이동: `File.Move` → `File.Copy` + `VerifyFileCopy` + `File.Delete`
  - [x] 폴더 이동: `Directory.Move` → `CopyDirectoryAsync` + `VerifyDirectoryCopy` + `Directory.Delete`
  - [x] `movedItems` 추적 업데이트 (복사/검증/삭제 각 단계별 상태)

- [x] 헬퍼 메서드 추가
  - [x] `VerifyFileCopy(string source, string dest)`: 파일 복사 검증
  - [x] `CopyDirectoryAsync(...)`: 재귀 폴더 복사
  - [x] `VerifyDirectoryCopy(string sourceDir, string destDir)`: 폴더 복사 검증

- [x] 롤백 로직 업데이트
  - [x] 복사 단계 실패: 대상 정리 (Clean-up copied files)
  - [x] 삭제 단계 실패: 경고만 로그 (Data is safe at destination)

- [x] `DeleteFileGroupAsync` 메서드 리팩토링
  - [x] 보관 폴더로 이동도 Copy-then-Delete 방식 적용
  - [x] 파일: `File.Move` → `File.Copy` + Verify + `File.Delete`
  - [x] 폴더: `Directory.Move` → `CopyDirectoryAsync` + Verify + `Directory.Delete`

### 빌드 결과

```
Build succeeded.
14 Warning(s) (기존 경고, Copy-then-Delete와 무관)
0 Error(s)
```

---

## 테스트 시나리오

### 1. 정상 케이스
- [x] 단일 파일 이동
- [x] 폴더 이동 (Normal 폴더)
- [x] 여러 파일 + 폴더 혼합 이동

### 2. 실패 케이스
- [ ] 복사 중 디스크 공간 부족
- [ ] 복사 중 네트워크 끊김
- [ ] 검증 실패 (파일 크기 불일치)
- [ ] 삭제 실패 (파일 잠김)

### 3. 에지 케이스
- [ ] 크로스 볼륨 이동 (C: → D:)
- [ ] 네트워크 드라이브 이동 (\\server\share)
- [ ] 대용량 파일 (> 1GB)
- [ ] 깊은 폴더 구조 (> 10 depth)

---

## 마이그레이션 가이드

### 단계별 적용

1. **Phase 1**: FileOperationService 헬퍼 메서드 추가
2. **Phase 2**: MoveFileGroupAsync 파일 이동 로직 변경
3. **Phase 3**: MoveFileGroupAsync 폴더 이동 로직 변경
4. **Phase 4**: DeleteFileGroupAsync 적용 (보관 폴더 이동)
5. **Phase 5**: 통합 테스트 및 검증

### 호환성

- ✅ IFileOperationService 인터페이스 변경 없음
- ✅ ViewModel 코드 변경 불필요
- ✅ 기존 충돌 해결(Overwrite/Skip/Abort) 정책 그대로 유지

---

## 참고 자료

- [Microsoft Docs: File.Copy Method](https://learn.microsoft.com/dotnet/api/system.io.file.copy)
- [Best Practices for File Operations](https://learn.microsoft.com/dotnet/standard/io/)
- [Transactional File Operations](https://learn.microsoft.com/windows/win32/fileio/transactional-ntfs-portal)
