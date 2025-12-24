# Normal Folder Polling Implementation Issue

## 날짜
2025-12-19

## 문제 상황

### 증상
- **폴링 적용 전**: FileSystemWatcher가 WSL 경로에서 동작하지 않았지만, 새로고침(Refresh)을 하면 Normal 폴더가 늦게나마 표시됨
- **폴링 적용 후**: 실시간으로 Normal 폴더의 **행조차 생기지 않음** (완전히 감지 실패)
- NIR, Camera 이미지도 불러와지지 않음 (매칭 실패로 추정)

### 초기 문제 인식
1. Normal 카메라 폴더가 실시간 감지되지 않음 (WSL 경로: `\\wsl.localhost\Ubuntu\...`)
2. FileSystemWatcher가 WSL 경로에서 동작하지 않음 (9P 프로토콜 비호환)
3. 새로고침은 정상 동작 (폴더 스캔 방식)

## 구현한 해결책: 0.1초 폴링 방식

### 설계 의도
사용자 요구사항:
> "실시간성이 중요하니까 0.1초마다 폴링해. 폴더 찾아서, 만약 있으면 행만들고, 안에 스티칭 이미지파일까지 있으면 썸네일 표시, 없으면 그 폴더를 확인할 폴더 딕셔너리에 넣고, 다음 폴링 때마다 그 폴더 안에 스티칭 파일이 있는지 확인, 있으면 썸네일 추가하고 목록에서 제거. 없으면 다시 확인"

### 구현 내용

#### 1. 폴링 인프라 추가 (MonitoringOrchestrator.cs)

**추가된 필드** (51-55번째 줄):
```csharp
// Polling infrastructure for WSL Normal folder detection
private System.Timers.Timer? _normalPollingTimer;
private readonly HashSet<string> _processedNormalFolders = new(); // Track processed folders
private readonly Dictionary<string, DateTime> _pendingNormalFolders = new(); // Folders waiting for stitched_original.png
private readonly object _pollingLock = new();
```

#### 2. 폴링 시작/중지 로직

**StartAsync 수정** (264번째 줄):
```csharp
// Start Normal folder polling for WSL paths
StartNormalFolderPolling();
```

**StopAsync 수정** (289-290번째 줄):
```csharp
// Stop Normal folder polling
StopNormalFolderPolling();
```

#### 3. 폴링 메서드 구현 (2818-3147번째 줄)

**StartNormalFolderPolling()** (2818-2842):
- 100ms(0.1초) 간격 타이머 생성
- Normal1Path, Normal2Path가 설정되어 있을 때만 시작
- `PollNormalFoldersAsync()` 호출

**PollNormalFoldersAsync()** (2871-2919):
- Normal1Path, Normal2Path 디렉토리 스캔
- 정규식 패턴 매칭: `^C\d{6}T\d{6}_\d+$`
- 각 폴더에 대해 `ProcessNormalFolderAsync()` 호출
- 펼딩 폴더에 대해 `CheckPendingFoldersAsync()` 호출

**ProcessNormalFolderAsync()** (2924-2968):
- 이미 처리된 폴더는 스킵 (`_processedNormalFolders` 확인)
- `CreateGroupFromNormalFolderAsync()` 호출하여 그룹 생성
- `stitched_original.png` 존재 여부 확인
- 없으면 `_pendingNormalFolders`에 추가

**CreateGroupFromNormalFolderAsync()** (3012-3038):
- 폴더명에서 타임스탬프 파싱 (`ParseTimestampFromFolderName`)
- `CreateGroupFromSingleFile(folderPath, FileType.Normal)` 호출
- 그룹 생성 성공 시 UI 로그 출력

**CheckPendingFoldersAsync()** (2973-3007):
- 펜딩 폴더 목록을 순회
- `stitched_original.png` 생성 여부 확인
- 생성되었으면 `UpdateGroupThumbnailAsync()` 호출
- 펜딩 목록에서 제거

**UpdateGroupThumbnailAsync()** (3043-3076):
- `_activeGroups`에서 해당 Normal 폴더를 포함한 그룹 찾기
- `LoadThumbnailForNormalAsync()`로 썸네일 로드
- `MainImageThumbnail`, `MainImagePath` 업데이트
- `OnGroupUpdated()` 이벤트 발생

#### 4. 매칭 버그 수정

**문제 발견**:
- `CreateGroupFromSingleFile`에서 Normal 폴더 처리 시, `NormalFolder`에 폴더 **이름만** 저장
- 기존 이벤트 기반 처리는 **전체 경로** 저장
- `FindMatchingExistingGroup`의 매칭이 실패

**수정 내용** (1370번째 줄):
```csharp
// Before:
group.NormalFolder = folderKey;  // "C251216T200720_0"

// After:
group.NormalFolder = filePath;   // "\\wsl.localhost\Ubuntu\...\C251216T200720_0"
```

## 발생한 문제

### 증상
1. **폴링 적용 후 Normal 폴더 행이 전혀 생성되지 않음**
2. NIR, Camera 이미지도 불러와지지 않음
3. 폴링 적용 전에는 늦더라도 새로고침으로 확인 가능했음

### 추정 원인

#### 가능성 1: 폴더 감지 실패
- 폴링이 실제로 폴더를 찾지 못함
- WSL 경로 접근 권한 문제
- 정규식 패턴 불일치

#### 가능성 2: 그룹 생성 실패
- `CreateGroupFromSingleFile`이 `null` 반환
- `ExtractTimestamp` 실패
- `ParseTimestampFromFolderName` 실패

#### 가능성 3: 매칭 로직 문제
- Normal 폴더 그룹은 생성되지만 UI에 표시되지 않음
- `OnGroupCreated` 이벤트가 발생하지 않음
- ViewModel에서 그룹을 받지 못함

#### 가능성 4: 타임스탬프/시퀀스 검증 실패
- `CreateGroupFromSingleFile`에서 생성된 그룹이 검증 단계에서 필터링됨
- DataSequenceSettings 검증 실패
- 시간 범위(tolerance) 검증 실패

## 관련 코드 위치

### 수정된 파일
1. **ChronoView/Core/FileWatching/MonitoringOrchestrator.cs**
   - 51-55: 폴링 인프라 필드 추가
   - 264: StartAsync에서 폴링 시작 호출
   - 289-290: StopAsync에서 폴링 중지 호출
   - 1370: NormalFolder 경로 수정 (이름 → 전체 경로)
   - 2812-3149: Normal 폴더 폴링 로직 (#region Normal Folder Polling)

### 참조 로직
1. **기존 이벤트 기반 처리** (CreateOrUpdateGroupAsync): 1034-1190줄
2. **그룹 매칭 로직** (FindMatchingExistingGroup): 1428-1681줄
3. **그룹 병합 로직** (MergeGroups): 1688-1791줄
4. **새로고침 로직** (PerformInitialScanAsync): 초기 스캔에서는 정상 동작

## 디버깅 필요 사항

### 로그 확인
1. 폴링 시작 로그: "Started Normal folder polling (100ms interval)"
2. 폴더 감지 로그: "Polling detected new Normal folder: {Folder}"
3. 그룹 생성 로그: "Created group {GroupId} from polling Normal folder"
4. 타임스탬프 파싱 실패 로그: "Failed to parse timestamp from Normal folder"
5. UnmatchedFiles 생성 실패 로그: "Failed to create UnmatchedFiles for Normal folder"

### 확인 포인트
1. `Directory.GetDirectories(basePath)`가 실제로 폴더를 반환하는가?
2. 정규식이 폴더명과 매칭되는가? (`C\d{6}T\d{6}_\d+$`)
3. `ParseTimestampFromFolderName`이 정상적으로 DateTime을 반환하는가?
4. `CreateGroupFromSingleFile`이 null을 반환하는가?
5. `OnGroupCreated` 이벤트가 실제로 발생하는가?
6. ViewModel의 이벤트 핸들러가 호출되는가?

### 비교 테스트
1. **새로고침 동작**과 **폴링 동작**의 차이점 확인
   - 새로고침: `PerformInitialScanAsync` → `FileGroupMatcher.MatchFilesAsync`
   - 폴링: `CreateGroupFromSingleFile` → 직접 그룹 생성
2. 새로고침에서는 `FileGroupMatcher`를 사용하는데, 폴링에서는 사용하지 않음
3. 이 차이가 문제의 원인일 가능성

## 원본 동작 방식 (폴링 적용 전)

### FileSystemWatcher 방식
- NIR, Camera: 이벤트 기반 감지 (Z: 드라이브, 정상 동작)
- Normal: WSL 경로에서 이벤트 미발생
- 새로고침: 모든 경로를 스캔하여 `FileGroupMatcher` 사용

### 새로고침 로직의 Normal 처리
1. `Directory.GetDirectories(normalPath)` 호출
2. 각 폴더에 대해 `stitched_original.png` 찾기
3. UnmatchedFiles 구조체에 추가
4. `FileGroupMatcher.MatchFilesAsync()` 호출
5. 반환된 FileGroup을 `_activeGroups`에 추가
6. UI 이벤트 발생

### 폴링 로직의 Normal 처리 (현재)
1. `Directory.GetDirectories(normalPath)` 호출
2. 각 폴더에 대해 `CreateGroupFromSingleFile` 직접 호출
3. 반환된 FileGroup에 GroupId 할당
4. `_activeGroups`에 추가... **여기서 실패 추정**
5. UI 이벤트 발생... **여기까지 도달 못함**

## 추가 의심 사항

### CreateGroupFromSingleFile의 한계
```csharp
private FileGroup? CreateGroupFromSingleFile(string filePath, FileType fileType)
{
    // ...
    case FileType.Normal:
        var normalTimestamp = ExtractTimestamp(filePath, fileType);

        if (!normalTimestamp.HasValue)
        {
            _logger.LogWarning("Could not extract timestamp from Normal folder: {Path}", filePath);
            return null;  // ⚠️ 여기서 null 반환 가능성
        }

        group.NormalFolder = filePath;
        group.MainImagePath = Path.Combine(filePath, "stitched_original.png");
        group.Timestamp = normalTimestamp.Value;
        group.CreatedAt = normalTimestamp.Value;
        group.Status = GroupStatus.Complete;
        break;
}
```

**문제점**:
- `ExtractTimestamp`가 Normal 폴더 경로에서 타임스탬프를 추출할 수 있는가?
- 폴더명 패턴: `C251216T200720_0`
- `ExtractTimestamp`는 **파일명**에서 추출하도록 설계되었을 가능성

### ExtractTimestamp 확인 필요
- Normal 폴더의 경우 `filePath`가 전체 경로 (예: `\\wsl.localhost\...\C251216T200720_0`)
- `Path.GetFileName(filePath)`로 폴더명만 추출해야 하는가?
- 아니면 `ExtractTimestamp`가 이미 처리하는가?

## 권장 해결 방안

### 1단계: 로그 확인
- 애플리케이션 실행 후 콘솔/파일 로그 확인
- "Started Normal folder polling" 메시지 확인
- "Polling detected new Normal folder" 메시지 확인
- "Failed to parse timestamp" 또는 "Could not extract timestamp" 경고 확인

### 2단계: ExtractTimestamp 검증
- Normal 폴더 경로에서 타임스탬프 추출이 제대로 되는지 확인
- 필요시 `ParseTimestampFromFolderName` 사용으로 전환

### 3단계: FileGroupMatcher 사용 검토
폴링에서도 새로고침과 동일하게 `FileGroupMatcher`를 사용하도록 변경:
```csharp
private async Task CreateGroupFromNormalFolderAsync(string folderPath)
{
    // UnmatchedFiles 생성
    var unmatchedFiles = CreateUnmatchedFilesForSingleFile(folderPath, FileType.Normal);

    // FileGroupMatcher 사용
    var groups = await _fileGroupMatcher.MatchFilesAsync(unmatchedFiles);
    var group = groups.FirstOrDefault();

    // ...
}
```

### 4단계: 이벤트 발생 확인
- `OnGroupCreated` 이벤트가 실제로 발생하는지 확인
- ViewModel이 이벤트를 수신하는지 확인
- UI 스레드에서 업데이트되는지 확인 (Dispatcher 필요 여부)

## 참고 설정

### config.json Normal 경로
```json
{
  "folderPaths": {
    "normal": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\normal"
  },
  "matchingSettings": {
    "normal1Path": "Z:\\윤태경\\seaweed\\program\\data\\시뮬\\normal"
  }
}
```

**실제 WSL 경로**: `\\wsl.localhost\Ubuntu\home\tmax\gim\camera` (로그에서 확인)

### Normal 폴더 명명 규칙
- 패턴: `C{YYMMDD}T{HHMMSS}_{0|1}`
- 예시: `C251216T200720_0` (2025-12-16 20:07:20, Line 1)
- 예시: `C251216T200720_1` (2025-12-16 20:07:20, Line 2)
- Suffix `_0` = Line 1, Suffix `_1` = Line 2

### 폴더 내부 구조
- `stitched_original.png`: 메인 이미지 (ML 모델이 생성, 빠르게 삭제될 수 있음)
- 폴더 자체는 유지됨 (ML 모델이 삭제하지 않음)

## 결론

폴링 구현은 논리적으로는 타당하지만, 실제로 **Normal 폴더 행이 전혀 생성되지 않는** 것으로 보아:
1. 폴더 감지 자체가 실패하거나
2. 타임스탬프 파싱이 실패하거나
3. 그룹 생성 후 UI 업데이트가 실패하거나
4. 이벤트 발생/수신 단계에서 문제가 있음

**가장 가능성 높은 원인**: `ExtractTimestamp`가 Normal 폴더 경로를 제대로 처리하지 못하고 `null`을 반환하여, `CreateGroupFromSingleFile`이 계속 `null`을 반환하는 것으로 추정됩니다.

새로고침 로직이 `FileGroupMatcher`를 사용하는 반면, 폴링 로직은 `CreateGroupFromSingleFile`을 직접 호출하는 차이점이 핵심 문제일 가능성이 높습니다.
