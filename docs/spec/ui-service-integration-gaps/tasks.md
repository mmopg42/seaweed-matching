# UI Service Integration Gaps - Implementation Tasks

## 개요

`design.md` 기반 구현 작업 순서입니다. 의존성을 고려하여 Phase 순서대로 진행합니다.

---

## Phase 1: 인프라 및 모델 확장

### Task 1.1: ApplicationConfiguration 확장
- [ ] `MatchingSettings`에 Line2 경로 속성 추가
  - `Nir1Path`, `Nir2Path`
  - `Normal1Path`, `Normal2Path`
  - 기존 `Camera4Path~Camera6Path` 확인
  - `OutputPath` 추가 (필요 시)
- [ ] `GetCameraLineNumber()` 헬퍼 메서드 추가

### Task 1.2: FileGroup 모델 확장
- [ ] `LineNumber` 속성 추가 (int, default=1)
- [ ] `GetLineNumberFromNormalFolder()` 정적 메서드 추가

### Task 1.3: FileGroupViewModel 확장
- [ ] `IsAbnormal` 속성 추가
- [ ] `AbnormalReason` 속성 추가
- [ ] `StatusText` 속성 수정 (Abnormal 표시 포함)
- [ ] `LineNumber` 속성 추가 (FileGroup에서 가져옴)
- [ ] `IsSelected` 속성 추가 (TwoWay 바인딩용)
- [ ] 생성자에 `IAbnormalDetector` 주입

### Task 1.4: MainWindowViewModel 인프라 확장
- [ ] `Line1Groups`, `Line2Groups` 컬렉션 추가
- [ ] `AbnormalCount` 속성 추가
- [ ] `ProgressValue`, `IsOperationInProgress` 속성 추가
- [ ] `ActiveTabIndex` 속성 추가
- [ ] `SelectedLine1Group`, `SelectedLine2Group` 속성 추가
- [ ] `SelectedGroup` 계산 속성 수정 (탭별)
- [ ] CancellationToken 관리 패턴 적용
  - `_windowCts`, `_operationCts` 필드
  - `BeginOperation()`, `CancelCurrentOperation()` 메서드
- [ ] `AddFileGroup()`, `RemoveFileGroup()`, `ClearFileGroups()` 라인별 컬렉션 처리
- [ ] `GetSelectedGroups()` 탭 기준 구현
- [ ] `CreateProgressReporter()` 헬퍼 추가
- [ ] `UpdateStatistics()`에 `AbnormalCount` 계산 추가

### Task 1.5: DI 컨테이너 등록
- [ ] `IAbnormalDetector` → `AbnormalDetectorService` 등록
- [ ] MainWindowViewModel 생성자에 `IAbnormalDetector` 주입

---

## Phase 2: 서비스 구현

### Task 2.1: FileOperationService 구현
- [ ] `MoveFileGroupAsync` 구현
  - 중복 파일명 처리 (`GetUniqueDestFileName`)
  - Progress 리포팅
  - 롤백 로직 (실패 추적 포함)
- [ ] `DeleteFileGroupAsync` 구현
  - Progress 리포팅
  - 부분 실패 처리
- [ ] `GetUniqueDestFileName()` 헬퍼 구현

### Task 2.2: PathManagementService 구현
- [ ] `GeneratePathsFromDate` 구현
  - Line1: NIR1, Normal1, Cam1-3
  - Line2: NIR2, Normal2, Cam4-6
  - Output 공통
- [ ] `CreateSampleFoldersAsync` 구현
  - Line1 + Line2 전체 경로 지원
  - 이미 존재하는 폴더 스킵

---

## Phase 3: Command 구현

### Task 3.1: ExecuteMove 비동기 구현
- [ ] `ExecuteMoveAsync()` 메서드 작성
- [ ] `GetSelectedGroups()` 활용
- [ ] `_fileOperationService.MoveFileGroupAsync()` 호출
- [ ] Progress 리포팅 연결
- [ ] 성공 시 `RemoveFileGroup()` 호출
- [ ] 로그 메시지 처리

### Task 3.2: ExecuteDelete 비동기 구현
- [ ] `ExecuteDeleteAsync()` 메서드 작성
- [ ] 삭제 확인 다이얼로그
- [ ] `_fileOperationService.DeleteFileGroupAsync()` 호출
- [ ] Progress 리포팅 연결
- [ ] 성공 시 `RemoveFileGroup()` 호출

### Task 3.3: ExecuteRefresh 비동기 구현
- [ ] `ExecuteRefreshAsync()` 메서드 작성
- [ ] `ClearFileGroups()` 호출
- [ ] `_orchestrator.RefreshAsync()` 호출
- [ ] 취소 처리

### Task 3.4: ExecutePathAutoConfig 비동기 구현
- [ ] `ExecutePathAutoConfigAsync()` 메서드 작성
- [ ] 날짜 형식 검증
- [ ] `GeneratePathsFromDate()` 호출
- [ ] `ApplyGeneratedPaths()` 구현
- [ ] 설정 저장
- [ ] 모니터링 재시작 옵션 제공

### Task 3.5: ExecuteCreateSampleFolder 비동기 구현
- [ ] `ExecuteCreateSampleFolderAsync()` 메서드 작성
- [ ] 폴더명 유효성 검사
- [ ] `CreateSampleFoldersAsync()` 호출
- [ ] 성공/실패 메시지 처리

---

## Phase 4: UI 연결

### Task 4.1: DragSelectBehavior 연결
- [ ] `Microsoft.Xaml.Behaviors.Wpf` 패키지 확인/설치
- [ ] MainWindow.xaml에 `xmlns:i` 네임스페이스 추가
- [ ] Line1 DataGrid에 Behavior 연결
- [ ] Line2 DataGrid에 Behavior 연결

### Task 4.2: SettingsDialog Browse 버튼 구현
- [ ] SettingsDialogViewModel에 Browse 커맨드들 추가
  - `BrowseNir1PathCommand`, `BrowseNir2PathCommand`
  - `BrowseNormal1PathCommand`, `BrowseNormal2PathCommand`
  - `BrowseCamera1~6PathCommand`
  - `BrowseOutputPathCommand`
- [ ] `BrowseFolder()` 공통 메서드 구현
- [ ] XAML에서 각 Browse 버튼에 Command 바인딩
- [ ] `<UseWindowsForms>true</UseWindowsForms>` 프로젝트 설정 (또는 WindowsAPICodePack)

### Task 4.3: MainWindow.xaml 수정
- [ ] StatusBar에 ProgressBar 추가
- [ ] TabControl에 `SelectedIndex="{Binding ActiveTabIndex}"` 바인딩
- [ ] Line1 탭 DataGrid에 `SelectedItem="{Binding SelectedLine1Group}"` 바인딩
- [ ] DataGridRow에 `IsSelected="{Binding IsSelected, Mode=TwoWay}"` 바인딩

### Task 4.4: Line2 탭 구현
- [ ] Line1과 동일한 DataGrid 구조 복제
- [ ] `ItemsSource="{Binding Line2Groups}"` 바인딩
- [ ] `SelectedItem="{Binding SelectedLine2Group}"` 바인딩
- [ ] DragSelectBehavior 연결

### Task 4.5: Combined 탭 구현
- [ ] Grid 레이아웃 (2열 + GridSplitter)
- [ ] Line1 DataGrid 배치
- [ ] Line2 DataGrid 배치
- [ ] 각각 적절한 바인딩 설정

### Task 4.6: SettingsDialog.xaml 경로 필드 추가
- [ ] Line 1 섹션: NIR1, Normal1, Cam1-3
- [ ] Line 2 섹션: NIR2, Normal2, Cam4-6
- [ ] 각 경로에 Browse 버튼 연결

---

## Phase 5: 테스트 및 검증

### Task 5.1: Unit Tests
- [ ] `FileGroupViewModel.IsAbnormal` 테스트
- [ ] `MainWindowViewModel.AbnormalCount` 테스트
- [ ] `FileGroup.GetLineNumberFromNormalFolder()` 테스트
- [ ] `GetUniqueDestFileName()` 테스트

### Task 5.2: Integration Tests
- [ ] ExecuteMove 파일 이동 및 롤백 테스트
- [ ] ExecuteDelete 파일 삭제 테스트
- [ ] ExecuteRefresh 컬렉션 갱신 테스트
- [ ] PathManagementService 경로 생성 테스트

### Task 5.3: Manual Tests
- [ ] DragSelectBehavior 다중 선택 UX
- [ ] Browse 버튼 폴더 선택 다이얼로그
- [ ] Line1/Line2/Combined 탭 전환
- [ ] Progress 표시 확인

---

## 의존성 그래프

```
Phase 1 (인프라) ──┬──> Phase 2 (서비스) ──> Phase 3 (Command)
                  │
                  └──> Phase 4 (UI)
                  
Phase 3 + Phase 4 ──> Phase 5 (테스트)
```

---

## 예상 소요 시간

| Phase | 예상 시간 | 비고 |
|-------|----------|------|
| Phase 1 | 2-3시간 | 모델 확장, ViewModel 인프라 |
| Phase 2 | 2-3시간 | 서비스 핵심 로직 |
| Phase 3 | 2-3시간 | 비동기 Command 구현 |
| Phase 4 | 2-3시간 | XAML 수정, 바인딩 |
| Phase 5 | 2-3시간 | 테스트 작성 및 검증 |
| **Total** | **10-15시간** | |
