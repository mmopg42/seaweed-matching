# UI 로그 패널 가이드

## 개요

UI 로그 패널은 ChronoView 애플리케이션의 실시간 로그를 표시하고 관리하는 컴포넌트입니다. 시스템 상태, 파일 처리, 에러 등 다양한 정보를 모니터링할 수 있습니다.

## 로그 레벨 (LogSeverity)

| 레벨 | 설명 | 표시 색상 |
|------|------|----------|
| **Debug** | 디버깅 정보 (상세 동작 추적) | 기본 색상 |
| **Info** | 일반 정보 메시지 | 파란색 (#3b82f6) |
| **Warning** | 경고 메시지 | 주황색 (#f59e0b) |
| **Error** | 에러 메시지 | 빨간색 (#dc2626), 굵게 |

## 로그 소스 (Source)

시스템의 다양한 컴포넌트에서 로그를 생성합니다:

- **System**: 전체 시스템 레벨 메시지
- **GroupManager**: 파일 그룹 관리 메시지
- **FileWatcher**: 파일 감시 메시지
- **FileMatching**: 파일 매칭 메시지
- **ImageProcessing**: 이미지 처리 메시지
- **AbnormalDetect**: 이상 감지 메시지
- **FileOperation**: 파일 작업(이동/삭제) 메시지

## 로그 구조

각 로그 메시지는 다음 정보를 포함합니다:

```csharp
{
    Timestamp: DateTime,    // 시간 (HH:mm:ss.fff)
    Severity: LogSeverity,  // 로그 레벨
    Source: string,         // 로그 소스
    Message: string,        // 메시지 내용
    LineNumber: int?        // 생산 라인 번호 (1 또는 2, null=시스템)
}
```

## 로그 필터링

### 1. 검색 (Search)
- 로그 메시지 또는 소스 이름으로 검색
- 대소문자 구분 없음

### 2. 레벨 필터 (Level Filter)
- 전체 보기 (All)
- Debug만 보기
- Info만 보기
- Warning만 보기
- Error만 보기

### 3. 라인 필터 (Line Filter)
- **Line 1 탭**: Line 1 + 시스템 로그만 표시
- **Line 2 탭**: Line 2 + 시스템 로그만 표시
- **통합 탭**: 모든 로그 표시

## 주요 로그 메시지

### 시스템 시작/종료

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `ChronoView application starting...` | Info | 애플리케이션 시작 |
| `Showing Setup Window` | Info | 설정 창 표시 |
| `Setup completed. Showing Main Window` | Info | 메인 창 표시 |
| `MainWindow loaded` | Info | 메인 창 로드 완료 |
| `Application exiting with code {ExitCode}` | Info | 애플리케이션 종료 |

### 감시/모니터링

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Starting monitoring` | Info | 모니터링 시작 |
| `Monitoring started successfully` | Info | 모니터링 시작 성공 |
| `Stopping monitoring` | Info | 모니터링 중지 |
| `Monitoring stopped successfully` | Info | 모니터링 중지 성공 |
| `Monitoring is already active` | Warning | 이미 모니터링 중 |
| `Monitoring is not active` | Warning | 모니터링 중 아님 |
| `새로고침 중...` | Info | 새로고침 시작 |
| `새로고침 완료 (감시 중지됨)` | Info | 새로고침 완료 |
| `감시 중인 경로: {Count}개` | Info | 감시 경로 수 |

### 파일 감시 (FileWatcher)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Starting hybrid file watcher (Polling: {EnablePolling})` | Info | 파일 감시 시작 |
| `Started watching {Path}` | Info | 감시 경로 시작 |
| `Polling enabled every {Interval}ms` | Info | 폴링 간격 설정 |
| `Directory does not exist or invalid: {Path}` | Warning | 유효하지 않은 디렉토리 |
| `Event queued: {Path}` | Debug | 이벤트 큐에 추가 |
| `Failed to queue event: {Path}` | Warning | 이벤트 큐 추가 실패 |

### 이벤트 처리 (EventProcessor)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Event processor started with {Count} worker` | Info | 이벤트 프로세서 시작 |
| `Stopping event processor` | Info | 이벤트 프로세서 중지 |
| `Event processor stopped` | Info | 이벤트 프로세서 중지 완료 |
| `Worker {Id} failed processing {Path}` | Error | 워커 처리 실패 |
| `Worker {Id} critical error` | Error | 워커 치명적 오류 |

### 초기 스캔 (Initial Scan)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `=== INITIAL SCAN START ===` | Info | 초기 스캔 시작 |
| `=== DEEP REFRESH START ===` | Info | 깊은 새로고침 시작 |
| `=== DEEP REFRESH COMPLETE (Monitoring STOPPED) ===` | Info | 새로고침 완료 |
| `Stopped monitoring services for refresh` | Info | 새로고침을 위한 감시 중지 |
| `Configuration reloaded from disk` | Info | 설정 다시 로드 |
| `Starting sequential initial scan with timestamp-ordered processing` | Info | 순차 초기 스캔 시작 |
| `Processing files in chronological order (earliest to latest)` | Info | 시간순 파일 처리 |
| `Processed {Count} files` | Debug | 처리된 파일 수 |
| `Pre-loading Normal images into cache...` | Info | 이미지 캐시 프리로드 |
| `✅ Pre-loaded {CachedCount} images in {Ms}ms ({FailedCount} failed)` | Info | 캐시 프리로드 완료 |
| `Sequential initial scan complete: {FilesScanned} files, {GroupsCreated} groups` | Info | 초기 스캔 완료 |
| `Failed to process {DataType} file: {Path}` | Warning | 파일 처리 실패 |

### 파일 그룹 (GroupManager)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Common: File already processed completely, skipping: {Path}` | Debug | 이미 처리된 파일 스킵 |
| `Re-process skip: 이미지 없음 - {Path}` | Debug | 이미지 없음으로 재처리 스킵 |
| `Re-process: 이미지 발견! - {Path}` | Info | 이미지 발견, 재처리 진행 |
| `[이미지체크] 이미지 없음: {FileName} (재처리 대기)` | Debug | 이미지 체크 - 대기 |
| `[이미지체크] 이미지 발견!: {FileName} (업데이트 진행)` | Debug | 이미지 체크 - 업데이트 |
| `Removing file from group {GroupId}: {Path}` | Info | 그룹에서 파일 제거 |
| `Removing {Path} from tracking` | Debug | 파일 추적 제거 |

### 파일 매칭 (FileMatching)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Using matching strategy: {Strategy}` | Info | 매칭 전략 사용 |
| `[DIAGNOSTIC] Final group count: Line1={Line1}, Line2={Line2}, Total={Total}` | Info | 최종 그룹 수 |
| `[DIAGNOSTIC] Group composition: NormalOnly={NO}, NormalWithCam={NWC}, CamOnly={CO}, NirOnly={NIO}` | Info | 그룹 구성 |
| `[BUILD-GROUPS] Line {Line}: Using DataSequenceSettings Order: {Order}` | Info | 데이터 시퀀스 순서 |
| `[BUILD-GROUPS] {DataType}(Order={Order}): Processing {Count} files` | Info | 데이터 타입 처리 |
| `[BUILD-GROUPS] {DataType}(Order={Order}): No files, skip` | Debug | 파일 없음 스킵 |
| `[MATCH] {DataType} file {File} ts={Ts:HH:mm:ss}: Looking for {PrevType} within {Min}~{Max}s` | Debug | 매칭 검색 |
| `[MATCH] ✓ Merged {DataType} {File} into Group {GroupId}` | Debug | 그룹 병합 성공 |
| `[MATCH] ✗ Created new group for {DataType} {File}` | Debug | 새 그룹 생성 |
| `[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: Queue empty, no match` | Debug | 큐 비어있음, 매칭 실패 |
| `[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: Sequential match -> {File}` | Debug | 순차 매칭 |
| `[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: No match found in queue` | Debug | 큐에서 매칭 없음 |

### NIR 매칭

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `[MATCH-NIR] NIR {NirKey} ts={NirTs:HH:mm:ss.fff}: Searching {GroupCount} groups (maxDiff={Max}s)` | Debug | NIR 매칭 검색 |
| `[MATCH-NIR]   Group[{Index}]: Already has NIR, skip` | Debug | 이미 NIR 있음 스킵 |
| `[MATCH-NIR]   Group[{Index}]: ts={GroupTs:HH:mm:ss.fff} diff={Diff:F3}s (abs={AbsDiff:F3}s)` | Debug | 그룹 시간차 |
| `[MATCH-NIR]   → New best match: Group[{Index}] absDiff={AbsDiff:F3}s` | Debug | 새로운 최적 매칭 |
| `[MATCH-NIR]   ✗ REJECTED: absDiff={AbsDiff:F3}s > maxDiff={Max}s` | Debug | 최대차 초과 거부 |
| `[MATCH-NIR] ✓ MATCHED: NIR {NirKey} → Group[{Idx}] absDiff={Diff:F3}s file={File}` | Debug | NIR 매칭 성공 |
| `[MATCH-NIR] ✗ NO MATCH: Creating NIR-only group for {NirKey}` | Debug | NIR 전용 그룹 생성 |
| `Created NIR-only group for {NirKey} ts={Ts} path={Path}` | Debug | NIR 전용 그룹 생성 완료 |

### 이미지 처리 (ImageProcessing)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `ImageProcessingService initialized with cache size: {CacheSizeMB}MB, Concurrency: {Limit}` | Info | 이미지 처리 서비스 초기화 |
| `Image file not found: {ImagePath}` | Warning | 이미지 파일 없음 |
| `Thumbnail cache hit for: {ImagePath}` | Debug | 썸네일 캐시 히트 |
| `Generated thumbnail for: {ImagePath} ({Size} bytes)` | Debug | 썸네일 생성 |
| `Error generating thumbnail for: {ImagePath}` | Error | 썸네일 생성 실패 |
| `Generated thumbnail with dimensions for: {ImagePath} ({OriginalW}x{OriginalH})` | Debug | 썸네일+차원 생성 |
| `Error generating thumbnail with dimensions for: {ImagePath}` | Error | 썸네일+차원 생성 실패 |
| `Image file not found for metadata extraction: {ImagePath}` | Warning | 메타데이터 추출용 이미지 없음 |
| `Metadata cache hit for: {ImagePath}` | Debug | 메타데이터 캐시 히트 |
| `Extracted metadata for: {ImagePath} ({Width}x{Height})` | Debug | 메타데이터 추출 |
| `Error extracting metadata for: {ImagePath}` | Error | 메타데이터 추출 실패 |
| `Error creating placeholder image` | Error | 플레이스홀더 이미지 생성 실패 |
| `Image caches cleared` | Info | 이미지 캐시 삭제 |
| `Failed to identify image dimensions for: {ImagePath}` | Error | 이미지 차원 식별 실패 |

### 이상 감지 (AbnormalDetector)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Building baseline for context {Context}: {Count}/{Min} samples` | Debug | 기준선 구축 중 |
| `Abnormal detected for {Context}: Ratio={R:F3} vs Median={M:F3}, Diff={D:F3}, Threshold={T:F2}` | Warning | 이상 감지 |
| `Normal for {Context}: Ratio={R:F3}, Diff={D:F3}` | Debug | 정상 |
| `Group {GroupId} detected as NIR-only abnormal` | Debug | NIR 전용 그룹 이상 감지 |
| `AbnormalDetectorService reset` | Info | 이상 감지 서비스 리셋 |

### 파일 작업 (FileOperation)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `ExecuteOpAsync started: GroupId={GroupId}, TargetBase={TargetBase}, OpType={OpType}, Schema={Schema}, Subject={Subject}` | Info | 파일 작업 시작 |
| `Moving NormalFolder: {Src} -> {Dest}` | Info | 일반 폴더 이동 |
| `Moving NIR files to: {Dest}` | Info | NIR 파일 이동 |
| `NIR file not found, skipping: {File}` | Warning | NIR 파일 없음 스킵 |
| `Moving camera file {Cam}: {Src} -> {Dest}` | Info | 카메라 파일 이동 |
| `ExecuteOpAsync completed successfully: {Count} items moved` | Info | 파일 작업 완료 |
| `ExecuteOpAsync failed, rolling back {Count} items` | Error | 파일 작업 실패, 롤백 |
| `DeleteComponentsAsync started: GroupId={GroupId}, Components={Components}, QuarantinePath={QuarantinePath}` | Info | 삭제 작업 시작 |
| `Moving Normal folder: {Src} -> {Dest}` | Debug | 일반 폴더 이동 (삭제 중) |
| `NIR file not found during delete, skipping: {File}` | Warning | NIR 파일 없음 스킵 (삭제 중) |
| `Moving NIR file: {Src} -> {Dest}` | Debug | NIR 파일 이동 (삭제 중) |
| `Moving Camera file ({Cam}): {Src} -> {Dest}` | Debug | 카메라 파일 이동 (삭제 중) |
| `DeleteComponentsAsync completed: {Count} items moved to quarantine` | Info | 삭제 작업 완료 |
| `DeleteComponentsAsync failed, rolling back {Count} items` | Error | 삭제 작업 실패, 롤백 |
| `NIR Limit reached ({Limit}). Stripping NIR from {GroupId}` | Info | NIR 제한 도달, NIR 제거 |
| `Moved directory: {Src} -> {Dest} (CrossVolume={CrossVolume})` | Info | 디렉토리 이동 (볼륨간) |

### 로그 정리 (LogCleanup)

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Log cleanup is disabled (retentionDays <= 0)` | Info | 로그 정리 비활성화 |
| `Skipping non-date folder: {FolderPath}` | Debug | 날짜 폴더 아님 스킵 |
| `Deleted old log file: {FilePath} (Folder date: {FolderDate})` | Info | 오래된 로그 파일 삭제 |
| `Failed to delete file: {FilePath}` | Warning | 파일 삭제 실패 |
| `Deleted empty date folder: {FolderPath} (Folder date: {FolderDate})` | Info | 빈 날짜 폴더 삭제 |
| `Failed to delete date folder: {FolderPath}` | Warning | 날짜 폴더 삭제 실패 |
| `Error processing date folder: {FolderPath}` | Warning | 날짜 폴더 처리 오류 |
| `Log cleanup completed: {DeletedCount} files deleted, {DeletedFolderCount} folders deleted (retention: {RetentionDays} days)` | Info | 로그 정리 완료 |
| `Error during log cleanup` | Error | 로그 정리 오류 |

### 설정 관리

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Opening settings dialog` | Info | 설정 다이얼로그 열기 |
| `Settings saved` | Info | 설정 저장 |
| `Settings cancelled` | Info | 설정 취소 |
| `Settings saved successfully` | Info | 설정 저장 성공 |
| `Settings applied and monitoring restarted` | Info | 설정 적용 및 모니터링 재시작 |
| `Display settings applied` | Info | 표시 설정 적용 |

### 에러/예외

| 메시지 | 레벨 | 설명 |
|--------|------|------|
| `Failed to start monitoring` | Error | 모니터링 시작 실패 |
| `Failed to stop monitoring` | Error | 모니터링 중지 실패 |
| `Failed to refresh` | Error | 새로고침 실패 |
| `Error starting file system watchers` | Error | 파일 감시 시작 실패 |
| `CRITICAL ERROR in {Source}: {Message}` | Error | 치명적 오류 |
| `Error during move operation or refresh` | Error | 이동 작업 또는 새로고침 오류 |
| `Failed to open preview for {Path}` | Error | 미리보기 열기 실패 |
| `Failed to load abnormal history (format changed?), resetting history` | Warning | 이상 기록 로드 실패 |
| `Failed to save abnormal history to {Path}` | Error | 이상 기록 저장 실패 |

## 로그 관리 기능

### 1. 로그 저장
- **빠른 저장**: 필터링된 로그를 기본 경로에 저장
- **파일 형식**: 텍스트 (.txt) 또는 CSV (.csv)
- **파일명**: `ChronoView_UI_Export_YYYYMMDD_HHMMSS.txt`

### 2. 로그 폴더 열기
- 로그 파일이 저장된 폴더를 파일 탐색기로 열기

### 3. 로그 클리어
- 화면에 표시된 모든 로그 메시지 삭제
- 확인 다이얼로그로 확인 후 삭제

### 4. 자동 스크롤
- 새 로그가 추가될 때 자동으로 최신 로그로 스크롤
- 기본적으로 활성화됨

## 로그 패널 UI

### 컨트롤 요소
- **로그 레벨 필터**: 드롭다운으로 레벨 선택
- **검색 상자**: 텍스트로 로그 검색
- **자동 스크롤 체크박스**: 자동 스크롤 활성/비활성
- **지우기 버튼**: 모든 로그 삭제
- **로그 저장 버튼**: 현재 필터된 로그 저장
- **폴더 열기 버튼**: 로그 폴더 열기
- **닫기 버튼**: 로그 패널 닫기

### 데이터 그리드 컬럼
- **심각도 (Severity)**: 로그 레벨
- **시간 (Time)**: 로그 생성 시간 (HH:mm:ss.fff)
- **소스 (Source)**: 로그 생성 컴포넌트
- **메시지 (Message)**: 로그 메시지 내용

## 로그 파일 위치

### UI 로그 파일
- **경로**: `%APPDATA%/ChronoView/Logs/{YYYYMMDD}/`
- **파일명**: `ChronoView_{YYYYMMDD_HHMMSS}.log`

### 디버그 로그 파일
- **경로**: `%APPDATA%/ChronoView/Logs/{YYYYMMDD}/`
- **파일명**: `ChronoView_Debug_{YYYYMMDD_HHMMSS}.log`

### 치명적 오류 로그
- **경로**: `%APPDATA%/ChronoView/Logs/{YYYYMMDD}/`
- **파일명**: `ChronoView_Critical_{YYYYMMDD_HHMMSS}.log`

## 로그 라인 식별

로그 메시지는 자동으로 라인 번호를 식별합니다:

| 식별 패턴 | 라인 번호 |
|----------|----------|
| Line 1, Line1, Nir1, Normal1, Cam1-3, Camera1-3 | 1 |
| Line 2, Line2, Nir2, Normal2, Cam4-6, Camera4-6 | 2 |
| 그 외 | null (시스템) |

로그 메시지에 `[Line X]` 접두사가 자동으로 추가됩니다.

## 제한 사항

- **최대 로그 메시지 수**: 5,000개
- 초과 시 가장 오래된 메시지부터 삭제
- 파일 시스템 오류 시 로그 파일 저장 실패 (무시됨)

## 사용 팁

1. **디버깅**: Debug 레벨을 활성화하여 상세한 동작 추적
2. **문제 해결**: Error/Warning 레벨 필터로 문제만 표시
3. **성능 모니터링**: FileWatcher, EventProcessor 로그 모니터링
4. **파일 처리 추적**: FileMatching, GroupManager 로그 확인
5. **이상 감지**: AbnormalDetect 로그에서 비정상 패턴 감지

## 관련 파일

- `ChronoView/UI/Controls/LogPanel.xaml`: 로그 패널 UI 정의
- `ChronoView/UI/Controls/LogPanel.xaml.cs`: 로그 패널 로직
- `ChronoView/UI/ViewModels/LogMessage.cs`: 로그 메시지 모델
- `ChronoView/Infrastructure/Logging/UILoggerProvider.cs`: UI 로그 제공자
- `ChronoView/Core/Logging/LogCleanupService.cs`: 로그 정리 서비스
