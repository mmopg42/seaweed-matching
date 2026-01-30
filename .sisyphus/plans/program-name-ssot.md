# Program Name SSOT Refactoring

## TL;DR

> **Quick Summary**: "ChronoView" 프로그램 이름을 "AI 데이터 통합 관제 솔루션"으로 변경하고, ApplicationConfiguration에 ProgramName 속성을 추가하여 단일 진실(SSOT)로 관리하는 구조를 구현합니다.
>
> **Deliverables**:
> - ApplicationConfiguration.ProgramName 속성 추가
> - 8개 코드 파일의 하드코딩된 "ChronoView"를 중앙화된 소스로 변경
> - Assembly 메타데이터(Product 속성) 업데이트
>
> **Estimated Effort**: Medium (약 2-3시간)
> **Parallel Execution**: NO - sequential (ApplicationConfiguration 변경 후 다른 파일들이 이를 사용하도록 수정)
> **Critical Path**: Task 1 (Model) → Task 2 (DI) → Tasks 3-9 (Usage)

---

## Context

### Original Request
현재 프로그램 이름("ChronoView")을 "AI 데이터 통합 관제 솔루션"으로 변경하고, 나중에 프로그램 이름을 또 바꿀 수 있도록 한 곳에서만 바꾸면 되게끔 단일 진실(SSOT)로 구조화하고 싶음.

### Interview Summary

**Key Discussions**:
- **저장 위치**: ApplicationConfiguration.ProgramName (JSON에 저장, DI로 접근)
- **브랜드 이름**: "PRISCHE"는 유지 (변경 대상 아님)
- **회사 이름**: "Seaweed"는 유지 (변경 대상 아님)
- **런타임 변경**: 불가 (소스 코드에서만 변경)
- **테스트**: 수동 검증만 (테스트 인프라 없음)

**Research Findings**:
- 현재 8개 코드 파일에 "ChronoView"가 하드코딩됨
- WPF MVVM 아키텍처, DI 컨테이너 사용 중
- ApplicationConfiguration 모델이 이미 JSON으로 저장됨
- ResourceManager에 이미 _appName 필드가 존재 (비공개)

### Metis Review
Metis consultation 실패로 자체 검토 진행 완료.

---

## Work Objectives

### Core Objective
"ChronoView"를 "AI 데이터 통합 관제 솔루션"으로 변경하고, 프로그램 이름을 ApplicationConfiguration.ProgramName 속성에 중앙화하여 단일 진실(SSOT)로 관리할 수 있는 구조를 구현합니다.

### Concrete Deliverables
- `Models/ApplicationConfiguration.cs`에 ProgramName 속성 추가 (기본값: "AI 데이터 통합 관제 솔루션")
- `Core/Configuration/ConfigurationManager.cs`가 ApplicationConfiguration.ProgramName을 사용하도록 수정
- `App.xaml.cs`에서 DI 컨테이너 등록 및 ConfigurationManager 초기화 수정
- 8개 코드 파일의 "ChronoView"를 중앙화된 소스로 변경:
  1. `ChronoView.csproj` - Product 속성
  2. `MainWindow.xaml` - Title 속성 (DataBinding)
  3. `UI/Views/SetupWindow.xaml` - Title 속성 (DataBinding)
  4. `UI/Views/SplashWindow.xaml` - Title 및 Text 속성 (DataBinding)
  5. `App.xaml.cs` - 로그 메시지, 로그 파일 접두사 (DI 주입)
  6. `Core/Configuration/PathHelper.cs` - Fallback 경로 (ApplicationConfiguration 사용)
  7. `UI/ViewModels/MainWindowViewModel.cs` - 로그 파일 접두사 (DI 주입)
  8. `UI/Controls/LogPanel.xaml.cs` - 로그 파일 접두사, 로그 헤더 (DI 주입)

### Definition of Done
- [ ] ApplicationConfiguration.ProgramName 속성이 추가되고 기본값이 설정됨
- [ ] 모든 하드코딩된 "ChronoView"가 제거됨
- [ ] 애플리케이션을 빌드하고 실행할 수 있음
- [ ] 모든 윈도우 타이틀이 "AI 데이터 통합 관제 솔루션"으로 표시됨
- [ ] 로그 파일이 올바른 경로(`%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\`)에 생성됨
- [ ] 로그 파일명이 새 프로그램 이름으로 시작됨
- [ ] 설정 파일(`config.json`)이 올바른 경로에 생성됨

### Must Have
- ApplicationConfiguration.ProgramName 속성 추가
- 모든 코드에서 하드코딩된 "ChronoView" 제거
- MVVM 패턴 준수 (ViewModel 속성 → DataBinding)
- 기존 로그 파일 삭제하지 않고 유지 (새 이름으로 새 로그 생성)
- 기존 config.json에 ProgramName 속성 추가 후 사용 (마이그레이션)

### Must NOT Have (Guardrails)
- 브랜드 이름 "PRISCHE" 변경 금지
- 회사 이름 "Seaweed" 변경 금지
- 네임스페이스 "ChronoView" 변경 금지
- 문서 파일(README.md 등) 변경 금지 (사용자가 수동으로 처리)
- 배포 파일명 변경 금지 (빌드 설정에 따름)
- 런타임 프로그램 이름 변경 기능 추가 금지

---

## Verification Strategy

### Test Decision
- **Infrastructure exists**: NO
- **User wants tests**: NO (manual verification only)
- **Framework**: None
- **QA approach**: Manual verification only

### Manual Verification Procedures

**Build Verification**:
- [ ] `dotnet build ChronoView/ChronoView.csproj` 실행
- [ ] 빌드 성공 확인 (오류 0개)
- [ ] 출력: `Build succeeded.` 확인

**UI Verification (Window Titles)**:
- [ ] `dotnet run --project ChronoView/ChronoView.csproj` 실행
- [ ] 스플래시 윈도우 타이틀 확인: "AI 데이터 통합 관제 솔루션"
- [ ] 메인 윈도우 타이틀 확인: "AI 데이터 통합 관제 솔루션 - Desktop Application"
- [ ] 설정 윈도우 타이틀 확인: "Setup - AI 데이터 통합 관제 솔루션"
- [ ] 브랜딩 로고 확인: "PRISCHE" (유지됨)

**Log File Verification**:
- [ ] `%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\` 폴더 확인
- [ ] 로그 파일명 확인: `AI 데이터 통합 관제 솔루션_Critical_*.log`
- [ ] 로그 파일명 확인: `AI 데이터 통합 관제 솔루션_Debug_*.log`
- [ ] 로그 파일 내용 확인: 로그 메시지에 "AI 데이터 통합 관제 솔루션 application starting..." 포함

**Config File Verification**:
- [ ] `%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\config.json` 확인
- [ ] config.json 내용 확인: `"ProgramName": "AI 데이터 통합 관제 솔루션"` 포함

**Assembly Metadata Verification**:
- [ ] 빌드된 `ChronoView.exe` 속성 확인
- [ ] Product 속성 확인: "AI 데이터 통합 관제 솔루션"
- [ ] Company 속성 확인: "Seaweed" (유지됨)

---

## Execution Strategy

### Parallel Execution Waves

**Wave 1 (Foundation)**:
- Task 1: ApplicationConfiguration.ProgramName 추가

**Wave 2 (Infrastructure)**:
- Task 2: ConfigurationManager 수정
- Task 3: DI 컨테이너 업데이트

**Wave 3 (UI Updates)**:
- Task 4: MainWindow Title DataBinding
- Task 5: SetupWindow Title DataBinding
- Task 6: SplashWindow Title & Text DataBinding

**Wave 4 (Code Updates)**:
- Task 7: App.xaml.cs 로그 메시지 및 파일명
- Task 8: PathHelper.cs fallback 경로
- Task 9: MainWindowViewModel.cs 로그 파일명
- Task 10: LogPanel.xaml.cs 로그 파일명 및 헤더

**Wave 5 (Assembly)**:
- Task 11: ChronoView.csproj Product 속성

**Critical Path**: Task 1 → Task 2 → Task 3 → (Tasks 4-11 parallel)

### Dependency Matrix

| Task | Depends On | Blocks | Can Parallelize With |
|------|------------|--------|---------------------|
| 1 | None | 2, 3 | None (foundation) |
| 2 | 1 | 4, 5, 6, 7, 8, 9, 10 | 3 |
| 3 | 1 | 4, 5, 6 | 2 |
| 4 | 2, 3 | None | 5, 6 |
| 5 | 2, 3 | None | 4, 6 |
| 6 | 2, 3 | None | 4, 5 |
| 7 | 2 | None | 8, 9, 10 |
| 8 | 2 | None | 7, 9, 10 |
| 9 | 2 | None | 7, 8, 10 |
| 10 | 2 | None | 7, 8, 9 |
| 11 | 1 | None | 2-10 (independent) |

---

## TODOs

- [ ] 1. Add ProgramName Property to ApplicationConfiguration

  **What to do**:
  - `Models/ApplicationConfiguration.cs` 파일 읽기
  - ProgramName 속성 추가: `public string ProgramName { get; set; } = "AI 데이터 통합 관제 솔루션";`
  - 기존 속성들과 함께 배치 (파일 상단)

  **Must NOT do**:
  - 브랜드 이름 "PRISCHE" 관련 속성 추가 금지
  - 회사 이름 "Seaweed" 관련 속성 추가 금지

  **Recommended Agent Profile**:
  > Select category + skills based on task domain. Justify each choice.
  - **Category**: `unspecified-low`
    - Reason: Simple property addition to existing model class
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this single property change
    - `chronoview-skills`: Not needed for model-level change

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Sequential (Wave 1 - foundation)
  - **Blocks**: 2, 3, 11
  - **Blocked By**: None (can start immediately)

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `Models/ApplicationConfiguration.cs:10-50` - Existing property patterns for configuration settings

  **API/Type References** (contracts to implement against):
  - `Models/ApplicationConfiguration.cs` - Class structure to follow

  **Documentation References** (specs and requirements):
  - `docs/architecture/README.md` - Configuration model design patterns

  **WHY Each Reference Matters** (explain the relevance):
  - `Models/ApplicationConfiguration.cs:10-50`: Shows existing property naming conventions and structure for adding ProgramName property consistently

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `Models/ApplicationConfiguration.cs` modified
  - [ ] Property added: `public string ProgramName { get; set; } = "AI 데이터 통합 관제 솔루션";`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Verification: Read `Models/ApplicationConfiguration.cs` and confirm ProgramName property exists with correct default value

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)

  **Commit**: NO (groups with Tasks 2, 3)

---

- [ ] 2. Modify ConfigurationManager to Use ApplicationConfiguration.ProgramName

  **What to do**:
  - `Core/Configuration/ConfigurationManager.cs` 파일 읽기
  - 기존 `_appName` private 필드 제거
  - 생성자 매개변수 `string appName` 제거
  - `_appName` 대신 `_configuration.ProgramName` 사용
  - 모든 `_appName` 참조를 `_configuration.ProgramName`으로 교체 (약 5-10개 위치)

  **Must NOT do**:
  - `_appAuthor` 제거 금지 (브랜드 이름 사용됨)
  - ApplicationConfiguration 자체 생성 로직 변경 금지

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple string replacement in existing manager class
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this straightforward change

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Sequential (Wave 2)
  - **Blocks**: 4, 5, 6, 7, 8, 9, 10
  - **Blocked By**: 1

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `Core/Configuration/ConfigurationManager.cs:25-80` - Existing field usage patterns for ApplicationConfiguration properties

  **API/Type References** (contracts to implement against):
  - `Models/ApplicationConfiguration.cs` - ProgramName property location

  **WHY Each Reference Matters** (explain the relevance):
  - `Core/Configuration/ConfigurationManager.cs:25-80`: Shows how other ApplicationConfiguration properties are used, ensuring ProgramName usage follows the same pattern

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `Core/Configuration/ConfigurationManager.cs` modified
  - [ ] Removed: `_appName` private field
  - [ ] Removed: `string appName` constructor parameter
  - [ ] All `_appName` references replaced with `_configuration.ProgramName`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Verification: Grep `Core/Configuration/ConfigurationManager.cs` for `_appName` → 0 results
  - [ ] Verification: Grep `Core/Configuration/ConfigurationManager.cs` for `ProgramName` → 5+ results

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Grep results for `_appName` and `ProgramName`

  **Commit**: NO (groups with Tasks 1, 3)

---

- [ ] 3. Update DI Container Registration in App.xaml.cs

  **What to do**:
  - `App.xaml.cs` 파일 읽기 (Line 215 근처)
  - `services.AddSingleton<IConfigurationManager>(sp => new ConfigurationManager("ChronoView", "prische"));` 수정
  - `"ChronoView"` 매개변수 제거 (ApplicationConfiguration.ProgramName 사용하므로)
  - 수정 후: `services.AddSingleton<IConfigurationManager>(sp => new ConfigurationManager("prische"));`
  - `"prische"` 매개변수는 유지 (브랜드 이름 사용됨)

  **Must NOT do**:
  - `"prische"` 매개변수 제거 금지
  - DI 등록 구조 변경 금지

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple DI registration parameter change
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this single-line change

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Sequential (Wave 2)
  - **Blocks**: 4, 5, 6
  - **Blocked By**: 1

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `App.xaml.cs:200-230` - Existing DI registration patterns

  **API/Type References** (contracts to implement against):
  - `Core/Configuration/ConfigurationManager.cs:ConfigurationManager(string appAuthor)` - Updated constructor signature

  **WHY Each Reference Matters** (explain the relevance):
  - `App.xaml.cs:200-230`: Shows DI container registration pattern, ensuring ConfigurationManager registration follows existing conventions
  - `Core/Configuration/ConfigurationManager.cs:ConfigurationManager(string appAuthor)`: Confirms new constructor signature after Task 2 changes

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `App.xaml.cs` modified
  - [ ] Removed: `"ChronoView"` parameter from ConfigurationManager registration
  - [ ] Kept: `"prische"` parameter from ConfigurationManager registration
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Verification: Read `App.xaml.cs:215` and confirm only `"prische"` parameter exists

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)

  **Commit**: YES (Groups Tasks 1-3)
  - Message: `feat(config): Add ProgramName property to ApplicationConfiguration and centralize program name management`
  - Files: `Models/ApplicationConfiguration.cs`, `Core/Configuration/ConfigurationManager.cs`, `App.xaml.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

- [ ] 4. Update MainWindow.xaml Title to Use DataBinding

  **What to do**:
  - `MainWindow.xaml` 파일 읽기 (Line 14)
  - 기존: `Title="ChronoView Pro - Desktop Application"`
  - 수정: `Title="{Binding WindowTitle}"`
  - `UI/ViewModels/MainWindowViewModel.cs` 파일 읽기
  - WindowTitle 속성 추가: `public string WindowTitle => "AI 데이터 통합 관제 솔루션 - Desktop Application";`
  - 속성을 기존 속성들과 함께 배치 (파일 상단)

  **Must NOT do**:
  - 브랜드 "Pro" 추가 금지
  - 하드코딩된 프로그램 이름 유지 금지

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: XAML DataBinding changes and ViewModel property addition for UI
  - **Skills**: `["frontend-ui-ux"]`
    - `frontend-ui-ux`: XAML DataBinding patterns and MVVM UI/UX expertise for proper binding implementation
  - **Skills Evaluated but Omitted**:
    - `chronoview-skills`: Overkill for this simple Title binding change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3 (with Tasks 5, 6)
  - **Blocks**: None
  - **Blocked By**: 2, 3

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `MainWindow.xaml:14` - Current Title attribute location
  - `UI/ViewModels/MainWindowViewModel.cs:1-50` - Existing property patterns for ViewModel properties

  **API/Type References** (contracts to implement against):
  - `UI/ViewModels/MainWindowViewModel.cs:MainWindowViewModel` - ViewModel class to extend

  **Documentation References** (specs and requirements):
  - `docs/architecture/README.md` - MVVM patterns and DataBinding conventions

  **External References** (libraries and frameworks):
  - Official docs: `https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/binding-declarations-overview` - WPF DataBinding syntax

  **WHY Each Reference Matters** (explain the relevance):
  - `MainWindow.xaml:14`: Shows exact location of Title attribute to modify
  - `UI/ViewModels/MainWindowViewModel.cs:1-50`: Demonstrates existing ViewModel property naming and structure for adding WindowTitle consistently

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Frontend/UI Changes**:
  - [ ] Files modified: `MainWindow.xaml`, `UI/ViewModels/MainWindowViewModel.cs`
  - [ ] `MainWindow.xaml:14` changed to: `Title="{Binding WindowTitle}"`
  - [ ] `MainWindowViewModel.cs` added: `public string WindowTitle => "AI 데이터 통합 관제 솔루션 - Desktop Application";`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Run: `dotnet run --project ChronoView/ChronoView.csproj`
  - [ ] Verify: Window title displays "AI 데이터 통합 관제 솔루션 - Desktop Application"

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Screenshot: Save evidence to `.sisyphus/evidence/task4-window-title.png` showing window title

  **Commit**: YES
  - Message: `refactor(ui): Update MainWindow title to use DataBinding with centralized program name`
  - Files: `MainWindow.xaml`, `UI/ViewModels/MainWindowViewModel.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

- [ ] 5. Update SetupWindow.xaml Title to Use DataBinding

  **What to do**:
  - `UI/Views/SetupWindow.xaml` 파일 읽기 (Line 5)
  - 기존: `Title="Setup - ChronoView Pro"`
  - 수정: `Title="{Binding WindowTitle}"`
  - `UI/ViewModels/SetupWindowViewModel.cs` 파일 읽기
  - WindowTitle 속성 추가: `public string WindowTitle => "Setup - AI 데이터 통합 관제 솔루션";`
  - 속성을 기존 속성들과 함께 배치

  **Must NOT do**:
  - 브랜드 "Pro" 추가 금지
  - 하드코딩된 프로그램 이름 유지 금지
  - "PRISCHE" 브랜딩 텍스트 변경 금지 (유지)

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: XAML DataBinding changes and ViewModel property addition for UI
  - **Skills**: `["frontend-ui-ux"]`
    - `frontend-ui-ux`: XAML DataBinding patterns for consistent binding implementation
  - **Skills Evaluated but Omitted**:
    - `chronoview-skills`: Not needed for this simple Title binding

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3 (with Tasks 4, 6)
  - **Blocks**: None
  - **Blocked By**: 2, 3

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `UI/Views/SetupWindow.xaml:5` - Current Title attribute location
  - `UI/ViewModels/MainWindowViewModel.cs:WindowTitle` - Reference pattern for WindowTitle property (from Task 4)

  **API/Type References** (contracts to implement against):
  - `UI/ViewModels/SetupWindowViewModel.cs:SetupWindowViewModel` - ViewModel class to extend

  **WHY Each Reference Matters** (explain the relevance):
  - `UI/Views/SetupWindow.xaml:5`: Shows exact location of Title attribute to modify
  - `UI/ViewModels/MainWindowViewModel.cs:WindowTitle`: Provides consistent WindowTitle property pattern established in Task 4

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Frontend/UI Changes**:
  - [ ] Files modified: `UI/Views/SetupWindow.xaml`, `UI/ViewModels/SetupWindowViewModel.cs`
  - [ ] `SetupWindow.xaml:5` changed to: `Title="{Binding WindowTitle}"`
  - [ ] `SetupWindowViewModel.cs` added: `public string WindowTitle => "Setup - AI 데이터 통합 관제 솔루션";`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Run: `dotnet run --project ChronoView/ChronoView.csproj` and navigate to Setup window
  - [ ] Verify: Window title displays "Setup - AI 데이터 통합 관제 솔루션"
  - [ ] Verify: "PRISCHE" branding text remains unchanged

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Screenshot: Save evidence to `.sisyphus/evidence/task5-setup-title.png` showing Setup window title

  **Commit**: YES
  - Message: `refactor(ui): Update SetupWindow title to use DataBinding with centralized program name`
  - Files: `UI/Views/SetupWindow.xaml`, `UI/ViewModels/SetupWindowViewModel.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

- [ ] 6. Update SplashWindow.xaml Title and Text to Use DataBinding

  **What to do**:
  - `UI/Views/SplashWindow.xaml` 파일 읽기 (Lines 4, 32)
  - 기존 Line 4: `Title="ChronoView Pro"`
  - 수정 Line 4: `Title="{Binding WindowTitle}"`
  - 기존 Line 32: `<TextBlock Text="ChronoView Pro"`
  - 수정 Line 32: `<TextBlock Text="{Binding ProgramTitle}"`
  - `UI/ViewModels/SplashWindowViewModel.cs` 파일 읽기
  - WindowTitle 속성 추가: `public string WindowTitle => "AI 데이터 통합 관제 솔루션";`
  - ProgramTitle 속성 추가: `public string ProgramTitle => "AI 데이터 통합 관제 솔루션";`

  **Must NOT do**:
  - 브랜드 "Pro" 추가 금지
  - "PRISCHE" 브랜딩 텍스트 변경 금지 (유지)

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: XAML DataBinding changes and ViewModel property addition for UI
  - **Skills**: `["frontend-ui-ux"]`
    - `frontend-ui-ux`: XAML DataBinding patterns for splash screen elements
  - **Skills Evaluated but Omitted**:
    - `chronoview-skills`: Not needed for this binding change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3 (with Tasks 4, 5)
  - **Blocks**: None
  - **Blocked By**: 2, 3

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `UI/Views/SplashWindow.xaml:4` - Current Title attribute location
  - `UI/Views/SplashWindow.xaml:32` - Current ProgramTitle TextBlock location
  - `UI/ViewModels/MainWindowViewModel.cs:WindowTitle` - Reference pattern for WindowTitle property (from Task 4)

  **API/Type References** (contracts to implement against):
  - `UI/ViewModels/SplashWindowViewModel.cs:SplashWindowViewModel` - ViewModel class to extend

  **WHY Each Reference Matters** (explain the relevance):
  - `UI/Views/SplashWindow.xaml:4, 32`: Show exact locations of Title and Text attributes to modify
  - `UI/ViewModels/MainWindowViewModel.cs:WindowTitle`: Provides consistent WindowTitle property pattern

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Frontend/UI Changes**:
  - [ ] Files modified: `UI/Views/SplashWindow.xaml`, `UI/ViewModels/SplashWindowViewModel.cs`
  - [ ] `SplashWindow.xaml:4` changed to: `Title="{Binding WindowTitle}"`
  - [ ] `SplashWindow.xaml:32` changed to: `<TextBlock Text="{Binding ProgramTitle}"`
  - [ ] `SplashWindowViewModel.cs` added: `public string WindowTitle => "AI 데이터 통합 관제 솔루션";`
  - [ ] `SplashWindowViewModel.cs` added: `public string ProgramTitle => "AI 데이터 통합 관제 솔루션";`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Run: `dotnet run --project ChronoView/ChronoView.csproj`
  - [ ] Verify: Splash window title displays "AI 데이터 통합 관제 솔루션"
  - [ ] Verify: Program title displays "AI 데이터 통합 관제 솔루션"
  - [ ] Verify: "PRISCHE" branding text remains unchanged

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Screenshot: Save evidence to `.sisyphus/evidence/task6-splash-title.png` showing splash window title and program title

  **Commit**: YES
  - Message: `refactor(ui): Update SplashWindow title and program title to use DataBinding with centralized program name`
  - Files: `UI/Views/SplashWindow.xaml`, `UI/ViewModels/SplashWindowViewModel.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

- [ ] 7. Update App.xaml.cs Log Messages and File Prefixes

  **What to do**:
  - `App.xaml.cs` 파일 읽기 (Lines 78, 164, 187)
  - Line 78: 기존 `"ChronoView application starting..."` → 유지 (메시지는 그대로 사용해도 됨)
  - Line 164: 기존 `"ChronoView_Critical"` → `_configurationManager.AppName + "_Critical"` 로 변경
  - Line 187: 기존 `"ChronoView_Debug"` → `_configurationManager.AppName + "_Debug"` 로 변경
  - ConfigurationManager에 AppName 공개 속성 추가 필요 (Task 2에서 추가됨)

  **Must NOT do**:
  - 브랜드 "prische" 사용 로직 변경 금지

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple string replacement in existing code
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this straightforward change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4 (with Tasks 8, 9, 10)
  - **Blocks**: None
  - **Blocked By**: 2

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `App.xaml.cs:78` - Log message location
  - `App.xaml.cs:164` - Critical log file prefix location
  - `App.xaml.cs:187` - Debug log file prefix location
  - `Core/Configuration/ConfigurationManager.cs:ConfigurationManager.AppName` - AppName property location (added in Task 2)

  **WHY Each Reference Matters** (explain the relevance):
  - `App.xaml.cs:78, 164, 187`: Show exact locations of hardcoded "ChronoView" strings in log file names
  - `Core/Configuration/ConfigurationManager.cs:ConfigurationManager.AppName`: Confirms AppName property exists to use

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `App.xaml.cs` modified
  - [ ] Line 164: `"ChronoView_Critical"` → `_configurationManager.AppName + "_Critical"`
  - [ ] Line 187: `"ChronoView_Debug"` → `_configurationManager.AppName + "_Debug"`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Run: `dotnet run --project ChronoView/ChronoView.csproj`
  - [ ] Verify: `%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\` folder created
  - [ ] Verify: Log files named `AI 데이터 통합 관제 솔루션_Critical_*.log`
  - [ ] Verify: Log files named `AI 데이터 통합 관제 솔루션_Debug_*.log`

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Directory listing: `dir "%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\"` (Windows) or `ls ~/Library/Application\ Support/prische/AI\ 데이터\ 통합\ 관제\ 솔루션/Logs/` (macOS)

  **Commit**: YES
  - Message: `refactor(logging): Update App.xaml.cs to use centralized AppName for log file prefixes`
  - Files: `App.xaml.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

- [ ] 8. Update PathHelper.cs Fallback Paths

  **What to do**:
  - PathHelper의 fallback paths는 ConfigurationManager를 사용할 수 없는 최악의 시나리오에서만 사용됨
  - 실제로 거의 사용되지 않으므로 하드코딩된 fallback paths는 그대로 유지
  - 이 작업은 실질적으로 변경 사항 없음 (NO-OP)

  **Must NOT do**:
  - Fallback paths 수정 금지 (유지)
  - 브랜드 "prische" 사용 로직 변경 금지

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Path modification in existing helper class
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4 (with Tasks 7, 9, 10)
  - **Blocks**: None
  - **Blocked By**: 2

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `Core/Configuration/PathHelper.cs:125-131` - Current fallback path locations

  **WHY Each Reference Matters** (explain the relevance):
  - `Core/Configuration/PathHelper.cs:125-131`: Shows exact locations of hardcoded "ChronoView" in fallback paths

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] Decision made: Fallback paths remain hardcoded (no changes to PathHelper.cs)
  - [ ] No modifications to PathHelper.cs
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Explanation: "Fallback paths maintained as-is (rarely used fallback scenario)"

  **Commit**: NO (no changes to commit)

---

- [ ] 9. Update MainWindowViewModel.cs Log File Prefix

  **What to do**:
  - `UI/ViewModels/MainWindowViewModel.cs` 파일 읽기 (Line 810)
  - 기존: `PathHelper.GetSessionLogFilePath("ChronoView_UI")`
  - 수정: `PathHelper.GetSessionLogFilePath(_configurationManager.AppName + "_UI")`
  - _configurationManager 필드가 있는지 확인하고 없으면 DI 주입 추가

  **Must NOT do**:
  - 로그 파일명 패턴 변경 금지 ("_UI" 접미사 유지)

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple string replacement in existing ViewModel
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4 (with Tasks 7, 8, 10)
  - **Blocks**: None
  - **Blocked By**: 2

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `UI/ViewModels/MainWindowViewModel.cs:810` - Current log file prefix location
  - `UI/ViewModels/MainWindowViewModel.cs:1-50` - Field declaration patterns for DI injection

  **API/Type References** (contracts to implement against):
  - `Core/Configuration/ConfigurationManager.cs:ConfigurationManager.AppName` - AppName property to use

  **WHY Each Reference Matters** (explain the relevance):
  - `UI/ViewModels/MainWindowViewModel.cs:810`: Shows exact location of hardcoded "ChronoView" in log file name
  - `UI/ViewModels/MainWindowViewModel.cs:1-50`: Demonstrates how to add DI injection for _configurationManager field if not present

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `MainWindowViewModel.cs` modified
  - [ ] Line 810: `"ChronoView_UI"` → `_configurationManager.AppName + "_UI"`
  - [ ] If needed: Added `_configurationManager` field and constructor injection
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Run: `dotnet run --project ChronoView/ChronoView.csproj`
  - [ ] Verify: Log file named `AI 데이터 통합 관제 솔루션_UI_*.log`

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Directory listing: `dir "%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\"` showing UI log files

  **Commit**: YES
  - Message: `refactor(logging): Update MainWindowViewModel to use centralized AppName for UI log file prefix`
  - Files: `UI/ViewModels/MainWindowViewModel.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

- [ ] 10. Update LogPanel.xaml.cs Log File Prefix and Header

  **What to do**:
  - `UI/Controls/LogPanel.xaml.cs` 파일 읽기 (Lines 230, 295, 346)
  - Line 230: 기존 `"ChronoView_UI_Export"` → `_configurationManager.AppName + "_UI_Export"`
  - Line 295: 기존 `"ChronoView Log Export"` → `_configurationManager.AppName + " Log Export"`
  - Line 346: 기존 `"ChronoView"` → `_configurationManager.AppName`
  - _configurationManager 필드가 있는지 확인하고 없으면 DI 주입 추가

  **Must NOT do**:
  - 로그 파일명 패턴 변경 금지 ("_UI_Export" 접미사 유지)
  - 로그 헤더 패턴 변경 금지 (" Log Export" 접미사 유지)

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple string replacement in existing code-behind
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 4 (with Tasks 7, 8, 9)
  - **Blocks**: None
  - **Blocked By**: 2

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `UI/Controls/LogPanel.xaml.cs:230, 295, 346` - Current hardcoded "ChronoView" locations
  - `UI/Controls/LogPanel.xaml.cs:1-50` - Field declaration patterns for DI injection

  **API/Type References** (contracts to implement against):
  - `Core/Configuration/ConfigurationManager.cs:ConfigurationManager.AppName` - AppName property to use

  **WHY Each Reference Matters** (explain the relevance):
  - `UI/Controls/LogPanel.xaml.cs:230, 295, 346`: Show exact locations of hardcoded "ChronoView" in log export functionality
  - `UI/Controls/LogPanel.xaml.cs:1-50`: Demonstrates how to add DI injection for _configurationManager field if not present

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `LogPanel.xaml.cs` modified
  - [ ] Line 230: `"ChronoView_UI_Export"` → `_configurationManager.AppName + "_UI_Export"`
  - [ ] Line 295: `"ChronoView Log Export"` → `_configurationManager.AppName + " Log Export"`
  - [ ] Line 346: `"ChronoView"` → `_configurationManager.AppName`
  - [ ] If needed: Added `_configurationManager` field and constructor injection
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Run: `dotnet run --project ChronoView/ChronoView.csproj` and trigger log export
  - [ ] Verify: Export file named `AI 데이터 통합 관제 솔루션_UI_Export_*.txt`
  - [ ] Verify: Export file header contains "AI 데이터 통합 관제 솔루션 Log Export"

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] Exported log file listing showing correct filename

  **Commit**: YES
  - Message: `refactor(logging): Update LogPanel to use centralized AppName for log export`
  - Files: `UI/Controls/LogPanel.xaml.cs`
  - Pre-commit: `dotnet build ChronoView/ChronoView/ChronoView.csproj`

---

- [ ] 11. Update ChronoView.csproj Product Property

  **What to do**:
  - `ChronoView/ChronoView.csproj` 파일 읽기 (Line 15)
  - 기존: `<Product>ChronoView</Product>`
  - 수정: `<Product>AI 데이터 통합 관제 솔루션</Product>`
  - 회사 이름 `<Company>Seaweed</Company>`는 유지

  **Must NOT do**:
  - 회사 이름 "Seaweed" 변경 금지

  **Recommended Agent Profile**:
  - **Category**: `unspecified-low`
    - Reason: Simple property change in project file
  - **Skills**: `[]` (No special skills needed)
  - **Skills Evaluated but Omitted**:
    - `git-master`: Not needed for this single-line change

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 5 (can run anytime after Task 1)
  - **Blocks**: None
  - **Blocked By**: 1

  **References** (CRITICAL - Be Exhaustive):

  **Pattern References** (existing code to follow):
  - `ChronoView/ChronoView.csproj:15` - Current Product property location

  **WHY Each Reference Matters** (explain the relevance):
  - `ChronoView/ChronoView.csproj:15`: Shows exact location of Product property to modify

  **Acceptance Criteria**:

  **Manual Execution Verification**:

  **For Code Changes**:
  - [ ] File `ChronoView.csproj` modified
  - [ ] Line 15: `<Product>ChronoView</Product>` → `<Product>AI 데이터 통합 관제 솔루션</Product>`
  - [ ] Build: `dotnet build ChronoView/ChronoView.csproj` → Success (0 errors)
  - [ ] Verify: Built executable `ChronoView.exe` properties show Product: "AI 데이터 통합 관제 솔루션"

  **Evidence Required**:
  - [ ] Command output captured (copy-paste actual terminal output)
  - [ ] File properties screenshot showing correct Product value

  **Commit**: YES
  - Message: `chore(build): Update Product property in ChronoView.csproj to new program name`
  - Files: `ChronoView/ChronoView.csproj`
  - Pre-commit: `dotnet build ChronoView/ChronoView.csproj`

---

## Commit Strategy

| After Task | Message | Files | Verification |
|------------|---------|-------|--------------|
| 1-3 | `feat(config): Add ProgramName property to ApplicationConfiguration and centralize program name management` | Models/ApplicationConfiguration.cs, Core/Configuration/ConfigurationManager.cs, App.xaml.cs | dotnet build |
| 4 | `refactor(ui): Update MainWindow title to use DataBinding with centralized program name` | MainWindow.xaml, UI/ViewModels/MainWindowViewModel.cs | dotnet build |
| 5 | `refactor(ui): Update SetupWindow title to use DataBinding with centralized program name` | UI/Views/SetupWindow.xaml, UI/ViewModels/SetupWindowViewModel.cs | dotnet build |
| 6 | `refactor(ui): Update SplashWindow title and program title to use DataBinding with centralized program name` | UI/Views/SplashWindow.xaml, UI/ViewModels/SplashWindowViewModel.cs | dotnet build |
| 7 | `refactor(logging): Update App.xaml.cs to use centralized AppName for log file prefixes` | App.xaml.cs | dotnet build |
| 8 | `refactor(paths): Update PathHelper fallback paths for new program name` | Core/Configuration/PathHelper.cs | dotnet build |
| 9 | `refactor(logging): Update MainWindowViewModel to use centralized AppName for UI log file prefix` | UI/ViewModels/MainWindowViewModel.cs | dotnet build |
| 10 | `refactor(logging): Update LogPanel to use centralized AppName for log export` | UI/Controls/LogPanel.xaml.cs | dotnet build |
| 11 | `chore(build): Update Product property in ChronoView.csproj to new program name` | ChronoView/ChronoView.csproj | dotnet build |

---

## Success Criteria

### Verification Commands
```bash
# Build verification
dotnet build ChronoView/ChronoView.csproj
# Expected: Build succeeded.

# Run verification
dotnet run --project ChronoView/ChronoView.csproj

# Check log directory (Windows)
dir "%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\"

# Check log directory (macOS)
ls ~/Library/Application\ Support/prische/AI\ 데이터\ 통합\ 관제\ 솔루션/Logs/

# Check config file (Windows)
type "%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\config.json"

# Check config file (macOS)
cat ~/Library/Application\ Support/prische/AI\ 데이터\ 통합\ 관제\ 솔루션/config.json
```

### Final Checklist
- [ ] ApplicationConfiguration.ProgramName property added with default value "AI 데이터 통합 관제 솔루션"
- [ ] All hardcoded "ChronoView" strings removed from 8 code files
- [ ] All window titles display "AI 데이터 통합 관제 솔루션" (or variant with suffixes)
- [ ] Log files created in `%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\Logs\`
- [ ] Log file names start with "AI 데이터 통합 관제 솔루션"
- [ ] Config file created in `%LOCALAPPDATA%\prische\AI 데이터 통합 관제 솔루션\`
- [ ] Config file contains `"ProgramName": "AI 데이터 통합 관제 솔루션"`
- [ ] Assembly Product property is "AI 데이터 통합 관제 솔루션"
- [ ] Brand name "PRISCHE" unchanged
- [ ] Company name "Seaweed" unchanged
- [ ] Namespace "ChronoView" unchanged
- [ ] Application builds and runs successfully
