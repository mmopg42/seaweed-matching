# UI Service Integration Gaps - Implementation Tasks

## 개요

`design.md` 기반 구현 작업 순서입니다. 의존성을 고려하여 Phase 순서대로 진행합니다.

---

## Phase 1: 인프라 및 모델 확장

### Task 1.1: ApplicationConfiguration 확장
- [x] `MatchingSettings`에 Line2 경로 속성 추가
  - `Nir1Path`, `Nir2Path`
  - `Normal1Path`, `Normal2Path`
  - 기존 `Camera4Path~Camera6Path` 확인
  - `OutputPath` 추가 (필요 시)
- [x] `GetCameraLineNumber()` 헬퍼 메서드 추가


### Task 1.2: FileGroup 모델 확장
- [x] `LineNumber` 속성 추가 (int, default=1)
- [x] `GetLineNumberFromNormalFolder()` 정적 메서드 추가

### Task 1.3: FileGroupViewModel 확장
- [x] `IsAbnormal` 속성 추가
- [x] `AbnormalReason` 속성 추가
- [x] `StatusText` 속성 수정 (Abnormal 표시 포함)
- [x] `LineNumber` 속성 추가 (FileGroup에서 가져옴) - 이미 존재
- [x] `IsSelected` 속성 추가 (TwoWay 바인딩용) - 이미 존재
- [x] 생성자에 `IAbnormalDetector` 주입

### Task 1.4: MainWindowViewModel 인프라 확장
- [x] `Line1Groups`, `Line2Groups` 컬렉션 추가 - 이미 존재
- [x] `AbnormalCount` 속성 추가 - 이미 존재
- [x] `ProgressValue`, `IsOperationInProgress` 속성 추가
- [x] `ActiveTabIndex` 속성 추가
- [x] `SelectedLine1Group`, `SelectedLine2Group` 속성 추가
- [x] `SelectedGroup` 계산 속성 수정 (탭별)
- [x] CancellationToken 관리 패턴 적용
  - `_windowCts`, `_operationCts` 필드
  - `BeginOperation()`, `EndOperation()`, `CancelCurrentOperation()` 메서드
- [x] `AddFileGroup()`, `RemoveFileGroup()`, `ClearFileGroups()` 라인별 컬렉션 처리 - 이미 존재
- [x] `GetSelectedGroups()` 탭 기준 구현
- [x] `CreateProgressReporter()` 헬퍼 추가
- [x] `UpdateStatistics()`에 `AbnormalCount` 계산 추가 - 이미 존재

### Task 1.5: DI 컨테이너 등록
- [x] `IAbnormalDetector` → `AbnormalDetectorService` 등록
- [x] MainWindowViewModel 생성자에 `IAbnormalDetector` 주입

---

## Phase 2: 서비스 구현

### Task 2.1: FileOperationService 구현
- [x] `MoveFileGroupAsync` 구현
  - [x] 중복 파일명 처리 (충돌 해결 콜백)
  - [x] Progress 리포팅
  - [x] 롤백 로직 (실패 추적 포함)
  - [x] **Copy-then-Delete 방식 적용** (안정성 강화, 2025-12-10 완료)
  - [x] **Rollback Fix (Data Loss Prevention)**: Restore to source if source deleted (Verified, 2025-12-11)
- [x] `DeleteFileGroupAsync` 구현
  - [x] Progress 리포팅
  - [x] 부분 실패 처리
- [x] `GetUniqueDestFileName()` 헬퍼 구현
- [x] **삭제 소프트 삭제(Trash/Quarantine 이동) 적용**: 물리 삭제 대신 보관 폴더로 이동, 충돌 처리(Overwrite/Skip/Abort)는 동일 정책 적용

### Task 2.2: PathManagementService 구현
- [x] `GeneratePathsFromDate` 구현
  - Line1: NIR1, Normal1, Cam1-3
  - Line2: NIR2, Normal2, Cam4-6
  - Output 공통
- [x] `CreateSampleFoldersAsync` 구현
  - Line1 + Line2 전체 경로 지원
  - 이미 존재하는 폴더 스킵
- [x] `ValidatePaths` 구현
  - [x] 경로 유효성 검증
  - [x] 잘못된 문자 체크

### Task 2.3: UI Service Integration (Conflict Handling & Callbacks)
- [x] ViewModel과 Service 연결을 위한 Callback 구현
- [x] ConflictResolution 선택 UI 로직 (Overwrite/Skip/Abort)
- [x] Progress Reporting 연동 (IProgress<OperationProgress>)
- [x] UI Thread Dispatching 처리

---

## Phase 3: Command 구현

- 공통 가드
  - [ ] `IsOperationInProgress` 변경 시 Start/Stop/Move/Delete/Refresh `RaiseCanExecuteChanged()` 호출
  - [ ] `CanExecuteStart/Stop/Refresh`에 `!IsOperationInProgress` 추가 (장기 작업 중 중복 실행 방지)

### Task 3.1: ExecuteMove 비동기 구현 ✅ COMPLETED
- [x] `ExecuteMoveAsync()` 메서드 작성
- [x] `GetSelectedGroups()` 활용
- [x] `_fileOperationService.MoveFileGroupAsync()` 호출
- [x] Progress 리포팅 연결
- [x] 성공 시 `RemoveFileGroup()` 호출
- [x] 로그 메시지 처리
- [x] **Copy-then-Delete 방식 적용 및 빌드 완료** (안정성 확인, 2025-12-10 완료)

### Task 3.2: ExecuteDelete 비동기 구현 ✅ COMPLETED
- [x] `ExecuteDeleteAsync()` 메서드 작성
- [x] 삭제 확인 팝업 (MessageBox)
- [x] `_fileOperationService.DeleteFileGroupAsync()` 호출
- [x] Progress 리포팅 연결
- [x] 성공 시 `RemoveFileGroup()` 호출
- [x] 소프트 삭제 정책 반영(보관 폴더 이동 및 충돌 처리 연동)

### Task 3.3: ExecuteRefresh 비동기 구현
- [x] `ExecuteRefreshAsync()` 재작성 (모니터링 OFF 가드, BeginOperation 전 경고 후 중단)
- [x] 컬렉션 직접 초기화 제거 (GroupRemoved/Created 이벤트에 의존)
- [x] `_orchestrator.RefreshAsync(cancellationToken)` 호출로 토큰 전달
- [x] Orchestrator 내부 `PerformInitialScanAsync` 및 루프에 토큰 전파/취소 처리 확인
- [x] 취소 및 버튼 비활성 동작 검증

### Task 3.4: ExecutePathAutoConfig 비동기 구현 ✅ COMPLETED
- [x] `ExecutePathAutoConfigAsync()` 메서드 작성
- [x] 날짜 형식 검증 (오늘 날짜 자동 사용)
- [x] `GeneratePathsFromDate()` 호출
- [x] `ApplyGeneratedPaths()` 구현 (NIR, Normal, Camera, Output 경로 업데이트)
- [x] 설정 저장
- [x] 모니터링 재시작 안내 메시지 제공

### Task 3.5: ExecuteCreateSampleFolder 비동기 구현 ✅ COMPLETED
- [x] `ExecuteCreateSampleFolderAsync()` 메서드 작성
- [x] 타임스탬프 기반 폴더명 자동 생성
- [x] `CreateSampleFoldersAsync()` 호출
- [x] 성공/실패 메시지 처리
- [x] 취소 지원 및 진행 상태 표시

---

## Phase 4: UI 연결

### Task 4.1: DragSelectBehavior 연결 ✅ COMPLETED
- [x] `Microsoft.Xaml.Behaviors.Wpf` 패키지 확인/설치 (v1.1.135 설치 확인됨)
- [x] MainWindow.xaml에 `xmlns:i` 네임스페이스 추가
- [x] Line1 DataGrid에 Behavior 연결
- [x] Line2 DataGrid에 Behavior 연결

### Task 4.2: SettingsDialog Browse 버튼 구현 ✅ COMPLETED
- [x] SettingsDialogViewModel에 Browse 커맨드들 추가 (BrowsePathCommand 사용)
  - `BrowsePathCommand` 통합 커맨드 활용 (CommandParameter로 구분)
- [x] `ExecuteBrowsePath()` 메서드 활용
- [x] XAML에서 각 Browse 버튼에 Command 바인딩
- [x] `UseWindowsForms` 프로젝트 설정 확인 (System.Windows.Forms 사용 중)

### Task 4.3: MainWindow.xaml 수정 ✅ COMPLETED
- [x] StatusBar에 ProgressBar 추가 (ProgressValue, IsOperationInProgress 바인딩)
- [x] TabControl에 `SelectedIndex="{Binding ActiveTabIndex}"` 바인딩 (이미 존재)
- [x] Line1 탭 DataGrid에 `SelectedItem="{Binding SelectedLine1Group}"` 바인딩 (이미 존재)
- [x] Line2 탭 DataGrid에 `SelectedItem="{Binding SelectedLine2Group}"` 바인딩 (이미 존재)
- [x] DataGridCheckBoxColumn에 `IsSelected` 바인딩 (이미 존재, TwoWay 기본값)
- [x] DataGridRow Style에 `IsSelected` 양방향 바인딩 추가 (모든 DataGrid에 적용) (이미 존재, TwoWay 기본값)

### Task 4.4: Line2 탭 구현 ✅ COMPLETED
- [x] Line1과 동일한 DataGrid 구조 복제
- [x] `ItemsSource="{Binding Line2Groups}"` 바인딩
- [x] `SelectedItem="{Binding SelectedLine2Group}"` 바인딩
- [x] DragSelectBehavior 연결
- [x] ProgressBar 인디케이터 추가 (Line 1과 일관성)

### Task 4.5: Combined 탭 구현 ✅ COMPLETED
- [x] Grid 레이아웃 (2열 + GridSplitter)
- [x] Line1 DataGrid 배치 (전체 컬럼)
- [x] Line2 DataGrid 배치 (전체 컬럼)
- [x] 각각 적절한 바인딩 설정
- [x] GetSelectedGroups() 메서드 업데이트

### Task 4.6: SettingsDialog.xaml 경로 필드 추가
- [x] Line 1 섹션: NIR1, Normal1, Cam1-3
- [x] Line 2 섹션: NIR2, Normal2, Cam4-6
- [x] 각 경로에 Browse 버튼 연결

---

## Phase 5: 테스트 및 검증

### Task 5.1: Unit Tests
- [ ] `FileGroupViewModel.IsAbnormal` 테스트
- [ ] `MainWindowViewModel.AbnormalCount` 테스트
- [ ] `FileGroup.GetLineNumberFromNormalFolder()` 테스트
- [ ] `GetUniqueDestFileName()` 테스트

### Task 5.2: Integration Tests
- [x] ExecuteMove 파일 이동 및 롤백 테스트 (Rollback Data Loss Fix Verified)
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
