# Normal과 NIR 파일 매칭 실패 문제

**작성일**: 2025-12-18  
**상태**: 분석 완료, 해결 방안 제시  
**심각도**: 높음

---

## 1. 문제 현상

### 1.1 증상

실시간 모니터링 중 Cam1, Cam2, Cam3 파일은 정상적으로 그룹에 매칭되지만, **Normal과 NIR 파일이 `DetermineFileType`에서 `Unknown`으로 판별되어 처리되지 않음**.

**로그 분석 (2025-12-18 업데이트)**:
```
info: Normal folder detected: C251201T140609_0, Timestamp: 2025-12-01 14:06:09
dbug: Unknown file type: Z:\윤태경\seaweed\program\data\시뮬\normal\C251201T140609_0
dbug: Unknown file type: Z:\윤태경\seaweed\program\data\시뮬\nir\run_120251201T140609A.txt
```

**관찰된 문제점**:
- ✅ Normal 폴더는 `FileWatcherService`에서 정상적으로 감지됨 (`"Normal folder detected"` 메시지 있음)
- ✅ NIR 파일도 `FileSystemWatcher`에서 이벤트 발생함
- ❌ 하지만 `DetermineFileType`에서 `Unknown`으로 판별되어 처리되지 않음
- ❌ `"Folder Identified as Normal"` 또는 `"File Identified as NIR"` 메시지 없음

### 1.2 예상 동작

데이터 시퀀스 순서에 따르면:
1. **NIR 파일**이 먼저 감지되어야 함 (Order:1)
2. **Normal 폴더**가 감지되어야 함 (Order:2)
3. **Cam1, Cam2, Cam3**이 감지되어야 함 (Order:3, 4, 5)

하지만 실제로는 Cam1이 먼저 그룹을 생성하고 있어, Normal과 NIR이 **감지되지 않았거나 필터링되어 버렸음**을 의미합니다.

---

## 2. 원인 분석

### 2.1 Normal 폴더 감지 실패 원인 (업데이트)

#### 2.1.1 이벤트 감지 상태

**✅ 정상 동작**: `FileWatcherService`에서 Normal 폴더가 정상적으로 감지됨
```
info: Normal folder detected: C251201T140609_0, Timestamp: 2025-12-01 14:06:09
```

**❌ 문제 발생**: `DetermineFileType`에서 `Unknown`으로 판별됨
```
dbug: Unknown file type: Z:\윤태경\seaweed\program\data\시뮬\normal\C251201T140609_0
```

#### 2.1.2 근본 원인 분석

**원인 1: `Directory.Exists(filePath)` 체크 실패 (가능성 높음)**

코드 위치: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2298`

```csharp
if (Directory.Exists(filePath))
{
    // This is a folder - treat as Normal folder event
    var folderName = Path.GetFileName(filePath);
    if (!string.IsNullOrEmpty(folderName) && folderName.StartsWith("C"))
    {
        _logger.LogInformation("Folder Identified as Normal: {Path}", filePath);
        return FileType.Normal;
    }
}
```

**문제점**:
- 폴더 생성 이벤트가 발생한 직후 `Directory.Exists`를 체크할 때, 파일 시스템 지연으로 인해 폴더가 아직 존재하지 않을 수 있음
- 네트워크 드라이브(Z:\)에서 특히 지연이 발생할 수 있음
- Race condition: 폴더 생성 이벤트와 실제 디스크 반영 사이의 타이밍 이슈

**원인 2: 경로 설정 불일치 (가능성 중간)**

로그에서 확인:
```
Checking Normal: Path=Z:\윤태경\seaweed\program\data\시뮬\normal\C251201T140609_0, N1=Z:\윤태경\seaweed\program\data\시뮬\nir
```

- `N1` (Normal1Path)가 `\nir`로 표시됨 (로그 메시지 오류일 수 있음)
- 실제 Normal1Path가 `Z:\윤태경\seaweed\program\data\시뮬\normal`이어야 함
- 경로 비교(`StartsWith`)가 실패할 수 있음

### 2.2 NIR 파일 감지 실패 원인 (업데이트)

#### 2.2.1 이벤트 감지 상태

**✅ 정상 동작**: `FileSystemWatcher`에서 NIR 파일 이벤트 발생
```
dbug: FileSystemEvent detected: Created - Z:\윤태경\seaweed\program\data\시뮬\nir\run_120251201T140609A.txt
```

**❌ 문제 발생**: `DetermineFileType`에서 `Unknown`으로 판별됨
```
dbug: Unknown file type: Z:\윤태경\seaweed\program\data\시뮬\nir\run_120251201T140609A.txt
```

#### 2.2.2 근본 원인 분석

**원인 1: 경로 비교 실패 (가능성 높음)**

코드 위치: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2321-2332`

```csharp
if ((!string.IsNullOrEmpty(settings.Nir1Path) && filePath.StartsWith(settings.Nir1Path, StringComparison.OrdinalIgnoreCase)) ||
    (!string.IsNullOrEmpty(settings.Nir2Path) && filePath.StartsWith(settings.Nir2Path, StringComparison.OrdinalIgnoreCase)))
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    if (ext == ".txt" || ext == ".csv")
    {
        _logger.LogInformation("File Identified as NIR: {Path}", filePath);
        return FileType.Nir;
    }
    return FileType.Unknown;
}
```

**문제점**:
- 파일 경로: `Z:\윤태경\seaweed\program\data\시뮬\nir\run_120251201T140609A.txt`
- `Nir1Path` 설정이 실제 경로와 일치하지 않을 수 있음
- 경로 끝의 백슬래시(`\`) 차이로 인한 비교 실패 가능성
- 예: 설정이 `Z:\윤태경\seaweed\program\data\시뮬\nir\`로 끝나면 `StartsWith`가 실패할 수 있음

**원인 2: 확장자 추출 문제 (가능성 낮음)**

- 파일명: `run_120251201T140609A.txt`
- `Path.GetExtension`은 `.txt`를 정상적으로 반환해야 함
- 하지만 경로 비교가 먼저 실패하면 이 단계에 도달하지 않음

---

## 3. 해결 방안

### 3.1 즉시 확인 사항

#### 3.1.1 로그 레벨 확인

현재 로그 레벨이 `Debug` 이상인지 확인:
- `"Checking Normal: Path={Path}, N1={N1}"` 메시지가 있는지 확인
- 이 메시지가 없다면 `DetermineFileType`이 호출되지 않았음을 의미

#### 3.1.2 경로 설정 확인

`ApplicationConfiguration`에서 다음 설정 확인:
- `Normal1Path`, `Normal2Path`가 올바르게 설정되어 있는지
- `Nir1Path`, `Nir2Path`가 올바르게 설정되어 있는지
- 실제 파일 경로와 일치하는지

#### 3.1.3 파일 시스템 이벤트 확인

`FileWatcherService`의 이벤트 로그 확인:
- `"FileSystemEvent detected: {ChangeType} - {Path}"` 메시지 확인
- Normal/NIR 경로에 대한 이벤트가 발생하는지 확인

### 3.2 코드 수정 방안

#### 3.2.1 Normal 폴더 감지 개선 (업데이트)

**문제**: `Directory.Exists(filePath)` 체크가 폴더 생성 직후 실패할 수 있음 (Race condition)

**해결책 1**: 폴더 경로를 직접 사용 (Directory.Exists 체크 제거 또는 재시도 로직 추가)

```csharp
// Normal 폴더 경로 체크 개선
if ((!string.IsNullOrEmpty(settings.Normal1Path) && filePath.StartsWith(settings.Normal1Path, StringComparison.OrdinalIgnoreCase)) ||
    (!string.IsNullOrEmpty(settings.Normal2Path) && filePath.StartsWith(settings.Normal2Path, StringComparison.OrdinalIgnoreCase)))
{
    // Check if this is a Normal folder itself
    // FIX: 폴더 경로인 경우 확장자가 없으므로, 확장자 체크로 폴더 여부 판단
    bool isFolder = !Path.HasExtension(filePath) || Directory.Exists(filePath);
    
    if (isFolder)
    {
        var folderName = Path.GetFileName(filePath);
        if (!string.IsNullOrEmpty(folderName) && folderName.StartsWith("C"))
        {
            _logger.LogInformation("Folder Identified as Normal: {Path}", filePath);
            return FileType.Normal;
        }
    }

    // Only consider image files as triggers for Normal groups
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
    {
        _logger.LogInformation("File Identified as Normal: {Path}", filePath);
        return FileType.Normal;
    }
    return FileType.Unknown;
}
```

**위치**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2294-2318`

#### 3.2.2 ShouldProcessEvent 필터링 로직 수정

**문제**: Normal 폴더 내부 파일이 필터링되기 전에 `stitched_original.png` 변환이 이루어져야 함

**해결책**: `ShouldProcessEvent`에서 `stitched_original.png`를 예외 처리

```csharp
private bool ShouldProcessEvent(FileSystemEventArgs e)
{
    // ... existing filters ...

    // SPECIAL CASE: stitched_original.png should be converted to folder event
    // Check this BEFORE filtering files inside Normal folders
    var pathFileName = Path.GetFileName(e.FullPath);
    if (pathFileName.Equals("stitched_original.png", StringComparison.OrdinalIgnoreCase))
    {
        // Allow through - will be converted in HandleFileCreatedEvent
        _logger.LogDebug("Allowing stitched_original.png for folder conversion: {Path}", e.FullPath);
        return true;
    }

    // Check if this is a FOLDER event (no extension) that looks like a Normal folder
    bool isNormalFolderEvent = !Path.HasExtension(e.FullPath) &&
                                !string.IsNullOrEmpty(pathFileName) &&
                                pathFileName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                                pathFileName.Contains('T');

    if (isNormalFolderEvent)
    {
        _logger.LogDebug("Detected Normal folder event: {Path}", e.FullPath);
        return true;
    }
    else if (!string.IsNullOrEmpty(pathFileName) && Path.HasExtension(e.FullPath))
    {
        // This is a FILE (has extension), check if it's inside a Normal folder
        var parentDirName = Path.GetFileName(Path.GetDirectoryName(e.FullPath));
        if (!string.IsNullOrEmpty(parentDirName) &&
            parentDirName.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
            parentDirName.Contains('T'))
        {
            // This is a file inside a Normal folder (except stitched_original.png)
            _logger.LogDebug("Skipping file inside Normal folder: {Path}", e.FullPath);
            return false;
        }
    }

    // ... rest of deduplication logic ...
}
```

**위치**: `ChronoView/Core/FileWatching/FileWatcherService.cs:491`

#### 3.2.3 NIR 파일 경로 비교 개선

**문제**: 경로 끝의 백슬래시 차이로 인한 `StartsWith` 비교 실패

**해결책**: 경로 정규화 후 비교

```csharp
// NIR 경로 비교 개선
private bool IsPathUnderNirPath(string filePath, string nirPath)
{
    if (string.IsNullOrEmpty(nirPath)) return false;
    
    // 경로 정규화: 끝의 백슬래시 제거 후 비교
    var normalizedNirPath = nirPath.TrimEnd('\\', '/');
    var normalizedFilePath = filePath.TrimEnd('\\', '/');
    
    return normalizedFilePath.StartsWith(normalizedNirPath, StringComparison.OrdinalIgnoreCase);
}

// DetermineFileType에서 사용
if (IsPathUnderNirPath(filePath, settings.Nir1Path) || 
    IsPathUnderNirPath(filePath, settings.Nir2Path))
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    if (ext == ".txt" || ext == ".csv")
    {
        _logger.LogInformation("File Identified as NIR: {Path}", filePath);
        return FileType.Nir;
    }
    return FileType.Unknown;
}
```

**위치**: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2320-2333`

### 3.3 폴링 활성화 (임시 해결책)

**문제**: FileSystemWatcher가 이벤트를 감지하지 못하는 경우

**해결책**: 폴링을 임시로 활성화하여 누락된 파일 감지

```csharp
// FileWatcherOptions에서 EnablePolling = true 설정
// PollingIntervalMs = 2000 (2초)
```

**주의**: 폴링은 리소스 사용이 증가하므로, 근본 원인 해결 후 비활성화해야 함

---

## 4. 진단 체크리스트

다음 항목을 순서대로 확인하여 문제 원인을 좁혀가세요:

### 4.1 이벤트 발생 확인

- [ ] `FileWatcherService` 로그에서 Normal/NIR 경로에 대한 `"FileSystemEvent detected"` 메시지 확인
- [ ] `"Normal folder detected"` 메시지 확인
- [ ] `"Detected stitched_original.png"` 메시지 확인

### 4.2 파일 타입 판별 확인

- [ ] `"Checking Normal: Path={Path}, N1={N1}"` 메시지 확인
- [ ] `"File Identified as Normal"` 메시지 확인
- [ ] `"File Identified as NIR"` 메시지 확인
- [ ] `"File Unknown"` 메시지 확인 (경로 불일치 가능성)

### 4.3 설정 확인

- [ ] `ApplicationConfiguration`에서 `Normal1Path`, `Normal2Path` 설정 확인
- [ ] `ApplicationConfiguration`에서 `Nir1Path`, `Nir2Path` 설정 확인
- [ ] 실제 파일 경로와 설정 경로 일치 여부 확인

### 4.4 파일 시스템 확인

- [ ] Normal 폴더가 실제로 생성되는지 확인
- [ ] `stitched_original.png` 파일이 생성되는지 확인
- [ ] NIR 파일 (`.txt` 또는 `.csv`)이 생성되는지 확인
- [ ] 파일이 생성되자마자 삭제/이동되는지 확인 (race condition)

---

## 5. 관련 문서

- `docs/trouble/event_vs_polling_analysis.md`: 이벤트 기반 vs 폴링 기반 분석
- `docs/spec/event_processing_optimization/01_requirements.md`: 이벤트 처리 최적화 요구사항
- `docs/spec/event_processing_optimization/04_design_review.md`: 설계 리뷰
- `docs/trouble/matching_logic_explanation.md`: 매칭 로직 설명

---

## 6. 해결 완료 (2025-12-18)

### 6.1 적용된 수정 사항

**수정 1: Normal 폴더 감지 개선**
- `Directory.Exists` 체크 전에 `Path.HasExtension`으로 폴더 여부 먼저 판단
- 네트워크 드라이브에서 `Directory.Exists` 지연 문제 해결
- 위치: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2297-2307`

**수정 2: NIR 파일 경로 비교 개선**
- 경로 끝의 백슬래시 차이로 인한 비교 실패 방지
- `TrimEnd('\\', '/')`로 경로 정규화 후 비교
- 위치: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2320-2340`

**수정 3: Normal 폴더 중복 처리 방지 개선** ⭐
- **문제**: 폴더 생성 이벤트와 `stitched_original.png` 파일 이벤트가 모두 큐에 들어가 중복 처리 발생 가능
- **해결**: `processKey`를 항상 폴더 경로로 통일하여 중복 방지
- 폴더 이벤트: `processKey = "Z:\...\normal\C251201T140609_0"`
- `stitched_original.png` 파일 이벤트: 폴더 이벤트로 변환 후 `processKey = "Z:\...\normal\C251201T140609_0"` (같은 키)
- `processedPaths` HashSet으로 배치 내 중복 방지
- 위치: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2101-2114`

### 6.3 중요: 파일 이벤트 스킵 vs 이미지 로딩

**질문**: Normal 폴더 내부 이미지 파일이 스킵 처리되는데, 이미지 로더는 어떻게 동작하나요?

**답변**: **완전히 별개입니다** ✅

#### 파일 이벤트 스킵 (이벤트 처리 레벨)
- **위치**: `FileWatcherService.cs:535` - `ShouldProcessEvent`
- **목적**: Normal 폴더 내부의 개별 이미지 파일 이벤트를 스킵하여 중복 처리 방지
- **효과**: 파일 이벤트가 큐에 들어가는 것을 막음
- **예시**: `Z:\...\normal\C251201T140609_0\stitched_original.png` 파일 이벤트 → 스킵

#### 이미지 로딩 (UI 레벨)
- **위치**: `FileGroupViewModel.cs:538` - `LoadThumbnailsAsync`
- **동작 방식**: 
  1. `CreateGroupFromSingleFile`에서 Normal 폴더 처리 시 `MainImagePath = Path.Combine(filePath, "stitched_original.png")` 설정 (1309번 라인)
  2. `FileGroupViewModel`이 `MainImagePath`를 사용하여 직접 파일 시스템에서 이미지 로드
  3. 폴더 경로에서 `stitched_original.png`를 직접 읽어서 썸네일 생성
- **결과**: 파일 이벤트를 스킵해도 이미지는 정상적으로 로드됨

**요약**:
- 파일 이벤트 스킵 = 중복 이벤트 처리 방지 (이벤트 큐 레벨)
- 이미지 로딩 = 폴더 경로에서 직접 파일 읽기 (UI 레벨)
- 두 작업은 독립적으로 동작하며, 파일 이벤트를 스킵해도 이미지는 정상적으로 표시됨

### 6.2 다음 단계

1. **즉시**: 애플리케이션 재시작 후 테스트
2. **확인**: Normal 폴더와 NIR 파일이 정상적으로 매칭되는지 확인
3. **모니터링**: 로그에서 `"Folder Identified as Normal"` 및 `"File Identified as NIR"` 메시지 확인
4. **장기**: Race condition 완전 해결 (폴더 이름 기반 타임스탬프 추출)

---

## 7. 참고 코드 위치

- `ChronoView/Core/FileWatching/FileWatcherService.cs:277-328` - `HandleFolderCreatedEvent`
- `ChronoView/Core/FileWatching/FileWatcherService.cs:330-357` - `HandleFileCreatedEvent` (stitched_original.png 변환)
- `ChronoView/Core/FileWatching/FileWatcherService.cs:491-538` - `ShouldProcessEvent` (필터링)
- `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2283-2353` - `DetermineFileType`
- `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:1000-1130` - `CreateOrUpdateGroupAsync`
- `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs:2514-2580` - `TryMatchPendingNirToGroup`

