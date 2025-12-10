# Task 4.6: SettingsDialog.xaml 경로 필드 추가 구현 계획

## 개요
**작성일자**: 2025-12-11
**작업명**: Task 4.6 - SettingsDialog.xaml 경로 필드 추가
**상태**: 📋 계획 단계 (구현 검증 중심)
**우선순위**: 높음 (설정 없이는 모니터링 불가)

---

## 1. 목적 및 배경
Line 2 지원 및 통합/분리 모드 지원을 위해 `SettingsDialog`에 추가적인 경로 설정 필드가 필요합니다.
- **Line 1**: NIR1, Normal1, Camera 1-3
- **Line 2**: NIR2, Normal2, Camera 4-6
- **Common**: Output Path, Quarantine Path (Soft Delete)

---

## 2. 현재 상태 분석

### 2.1 SettingsDialog.xaml
**파일 위치**: `ChronoView/UI/Views/SettingsDialog.xaml`
**현황**:
- ✅ **Line 1 Paths**: NIR1, Normal1, Camera 1~3 필드 및 Browse 버튼 구현됨
- ✅ **Line 2 Paths**: NIR2, Normal2, Camera 4~6 필드 및 Browse 버튼 구현됨
- ✅ **Common Paths**: Output, Quarantine(Trash) 필드 및 Browse 버튼 구현됨
- ✅ **UI 구조**: TabControl > Paths 탭 내에 그룹화되어 배치됨

### 2.2 SettingsDialogViewModel.cs
**파일 위치**: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`
**현황**:
- ✅ **Properties**: `NirPath`~`Camera6Path`, `OutputPath`, `DeleteQuarantinePath` 등 모든 필드에 대한 ViewModel 속성 존재
- ✅ **LoadLogic**: `LoadFromConfiguration()` 에서 `ApplicationConfiguration`의 값을 ViewModel로 매핑 구현됨
- ✅ **SaveLogic**: `SaveToConfiguration()` 에서 ViewModel의 값을 `ApplicationConfiguration` 및 `FolderPaths` 딕셔너리로 저장 구현됨
- ✅ **BrowseCommand**: `ExecuteBrowsePath(string pathType)` 메서드에서 모든 경로 타입(nir1~2, normal1~2, cam1~6, output, quarantine)에 대한 분기 처리 구현됨

### 2.3 ApplicationConfiguration.cs
**파일 위치**: `ChronoView/Models/ApplicationConfiguration.cs`
**현황**:
- ✅ `MatchingSettings` 내에 모든 경로 속성(`Nir1Path`, `Normal2Path` 등) 정의됨
- ✅ `WorkflowSettings` 내에 `DeleteQuarantinePath` 정의됨

---

## 3. 구현 계획 (Verification Focus)

현재 코드가 요구사항을 100% 충족하는 것으로 분석되므로, "신규 구현"보다는 **"검증 및 마감"** 프로세스로 진행합니다.

### 3.1 검증 항목
1. **UI 바인딩 확인**:
   - XAML의 Text 속성과 ViewModel의 Property 명칭 일치 여부 (완료)
   - Browse 버튼의 CommandParameter가 ViewModel의 switch문 케이스와 일치 여부 (완료)

2. **데이터 지속성(Persistence) 확인**:
   - 앱 실행 > Settings 진입 > 경로 변경 > Save > 앱 재시작 > Settings 진입 시 변경된 값 유지 확인

3. **기본값 로직 확인**:
   - `DeleteQuarantinePath`가 비어있을 때 `BasePath/Trash` 자동 설정 로직 동작 확인

### 3.2 필요 시 수정 사항 (Refinement)
- **UI 레이아웃 미세 조정**: 그룹 간 간격(Margin)이나 헤더 스타일 통일성 점검
- **유효성 검사**: 저장 시 필수 경로가 존재하는지 체크하는 로직 추가 (선택 사항, 현재는 `MainWindowViewModel.ExecuteStartAsync`에서 수행 중)

---

## 4. 테스트 계획

### 4.1 수동 테스트 시나리오
| ID | 테스트 항목 | 절차 | 예상 결과 |
|----|-----------|------|-----------|
| T4.6-1 | 경로 탐색 | 각 필드의 [Browse...] 버튼 클릭 | 폴더 선택 다이얼로그가 열리고, 선택 시 TextBox에 경로 반영 |
| T4.6-2 | Line 2 경로 저장 | Line 2 섹션의 경로(NIR2, Normal2 등) 입력 후 OK 클릭 | 로그에 오류 없이 저장됨 |
| T4.6-3 | 설정 로드 | 앱 재시작 후 Settings 다시 열기 | 입력했던 Line 2 경로가 그대로 표시됨 |
| T4.6-4 | Quarantine 경로 | Quarantine Path 변경 후 저장 | `tasks.json` 또는 설정 파일에 반영됨 |

---

## 5. 결론
Task 4.6은 **이미 구현 완료된 상태**로 확인됩니다.
별도의 코드 작성 없이, Task 4.5(Combined Tab) 구현 후 전체 통합 테스트 시점에 함께 검증하는 것으로 종결합니다.
