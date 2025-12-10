# UI Service Integration - 누락된 구현 항목 요구사항

## 개요

`.kiro/specs/ui-service-integration/tasks.md`에서 완료(✅)로 표시되었으나 실제 코드에서 구현되지 않은 항목들을 분석하여 정리한 문서입니다.

## 분석 배경

- **분석 일자**: 2025-12-10
- **분석 대상**: `ChronoView` WPF 프로젝트
- **참조 스펙**: `.kiro/specs/ui-service-integration/` (tasks.md, design.md, requirements.md)

---

## 누락된 구현 항목

### 1. Command 구현 미완료 (Tasks 11~18 관련)

tasks.md에서 Task 10.1까지 완료로 표시되어 있으나, 다음 Command 메서드들은 **TODO 주석만 있고 실제 로직이 구현되지 않음**:

#### 1.1 ExecuteMove (Move Command)
- **파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` (Line 665-671)
- **현재 상태**:
  ```csharp
  private void ExecuteMove()
  {
      if (SelectedGroup == null) return;
      AddLogMessage(LogSeverity.Info, "FileOperation", $"Moving group {SelectedGroup.GroupId}");
      // TODO: Implement move operation
  }
  ```
- **필요 구현**: 
  - FileOperationService를 통한 파일 그룹 이동
  - Progress 모달/인디케이터 표시
  - 성공 시 그룹 목록에서 제거
  - 롤백 지원

#### 1.2 ExecuteDelete (Delete Command)
- **파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` (Line 678-684)
- **현재 상태**:
  ```csharp
  private void ExecuteDelete()
  {
      if (SelectedGroup == null) return;
      AddLogMessage(LogSeverity.Warning, "FileOperation", $"Deleting group {SelectedGroup.GroupId}");
      // TODO: Implement delete operation
  }
  ```
- **필요 구현**:
  - 삭제 확인 다이얼로그
  - FileOperationService를 통한 파일 삭제
  - Progress 인디케이터
  - 성공 시 그룹 목록에서 제거

#### 1.3 ExecuteRefresh (Refresh Command)
- **파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` (Line 686-690)
- **현재 상태**:
  ```csharp
  private void ExecuteRefresh()
  {
      AddLogMessage(LogSeverity.Info, "System", "Refreshing data");
      // TODO: Implement refresh logic
  }
  ```
- **필요 구현**:
  - MonitoringOrchestrator.RefreshAsync() 호출
  - FileGroups 컬렉션 초기화
  - 전체 디렉토리 스캔 수행

#### 1.4 ExecutePathAutoConfig (Path Auto Config Command)
- **파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` (Line 692-696)
- **현재 상태**:
  ```csharp
  private void ExecutePathAutoConfig()
  {
      AddLogMessage(LogSeverity.Info, "Configuration", "Auto-configuring paths");
      // TODO: Implement path auto-configuration
  }
  ```
- **필요 구현**:
  - DateInput 속성에서 날짜 읽기
  - PathManagementService.GeneratePathsFromDate() 호출
  - 설정 업데이트

#### 1.5 ExecuteCreateSampleFolder (Create Sample Folder Command)
- **파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs` (Line 698-702)
- **현재 상태**:
  ```csharp
  private void ExecuteCreateSampleFolder()
  {
      AddLogMessage(LogSeverity.Info, "FileOperation", "Creating sample folder");
      // TODO: Implement sample folder creation
  }
  ```
- **필요 구현**:
  - SampleFolderName 속성에서 이름 읽기
  - PathManagementService.CreateSampleFoldersAsync() 호출
  - 성공/실패 메시지 로그

---

### 2. Abnormal Detection UI 통합 미완료 (Task 10.2 관련)

#### 2.1 IsAbnormal 속성 누락
- **파일**: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
- **문제**: `IsAbnormal` 속성이 정의되어 있지 않음
- **영향**: `MainWindow.xaml`의 `FileGroupRowStyle`에서 `IsAbnormal` 바인딩이 작동하지 않음
- **필요 구현**:
  - FileGroupViewModel에 `IsAbnormal` 속성 추가
  - AbnormalDetectorService와 연동

#### 2.2 AbnormalCount 속성 누락
- **파일**: `ChronoView/UI/ViewModels/MainWindowViewModel.cs`
- **문제**: `AbnormalCount` 속성이 정의되어 있지 않음
- **영향**: `MainWindow.xaml`의 통계 바에서 "Abnormal:" 바인딩이 작동하지 않음
- **필요 구현**:
  - MainWindowViewModel에 `AbnormalCount` 속성 추가
  - 비정상 그룹 카운트 로직 구현

---

### 3. DragSelectBehavior 미적용 (Task 10.1 관련)

#### 3.1 Behavior 미연결
- **파일**: `ChronoView/UI/Behaviors/DragSelectBehavior.cs` - 구현됨
- **파일**: `ChronoView/MainWindow.xaml` - 연결되지 않음
- **문제**: DragSelectBehavior가 DataGrid에 attached되지 않음
- **필요 구현**:
  - MainWindow.xaml에 `xmlns:i="http://schemas.microsoft.com/xaml/behaviors"` 추가
  - DataGrid에 `<i:Interaction.Behaviors><behaviors:DragSelectBehavior/></i:Interaction.Behaviors>` 추가

---

### 4. Settings Dialog Browse 버튼 미구현 (Task 10.1, 19 관련)

#### 4.1 Browse 버튼 기능 없음
- **파일**: `ChronoView/UI/Views/SettingsDialog.xaml`
- **문제**: 모든 "Browse..." 버튼에 Click 이벤트 또는 Command가 없음
- **영향**: 사용자가 폴더 선택을 할 수 없음
- **필요 구현**:
  - FolderBrowserDialog를 통한 폴더 선택 기능
  - 또는 OpenFileDialog/CommonOpenFileDialog 사용

---

### 5. Line2 및 Combined 탭 미구현

#### 5.1 Line2 탭
- **파일**: `ChronoView/MainWindow.xaml` (Line 415-417)
- **현재 상태**: "Line 2 view (to be implemented)" 텍스트만 표시
- **필요 구현**:
  - Line1과 동일한 DataGrid 구조
  - Line2Groups 컬렉션 바인딩

#### 5.2 Combined 탭
- **파일**: `ChronoView/MainWindow.xaml` (Line 418-429)
- **현재 상태**: 단순 그리드 분할만 표시
- **필요 구현**:
  - 두 개의 DataGrid (Line1, Line2)
  - 동시 표시 레이아웃

---

## 우선순위 권장

| 순위 | 항목 | 이유 |
|------|------|------|
| 1 | IsAbnormal/AbnormalCount | UI에 이미 바인딩되어 있으나 작동하지 않음 |
| 2 | ExecuteMove/ExecuteDelete | 핵심 파일 조작 기능 |
| 3 | ExecuteRefresh | 사용자 경험에 중요한 기능 |
| 4 | Browse 버튼 | 설정 편의성 |
| 5 | DragSelectBehavior 연결 | 다중 선택 UX 향상 |
| 6 | Line2/Combined 탭 | 분리 모드 지원 |
| 7 | PathAutoConfig/CreateSampleFolder | 편의 기능 |

---

## 연관 태스크 (tasks.md 기준)

완료로 표시되었으나 실제로는 미완료 상태인 태스크:

- Task 10.1: DragSelectBehavior 연결 미완료
- Task 10.2: Abnormal Detection UI 통합 미완료 (IsAbnormal, AbnormalCount 속성 누락)

미완료로 표시된 태스크 중 관련 항목:

- Task 11: Statistics Event Handlers (실제로 AbnormalCount가 없음)
- Task 12: FileOperationService (Move/Delete용)
- Task 13-14: Move/Delete Command
- Task 15-17: PathManagementService
- Task 18: Refresh Command
- Task 19: Settings Dialog Save (Browse 버튼 포함)
