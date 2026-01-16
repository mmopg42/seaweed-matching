---
Task: launch_and_activate_external_programs
Created: 2026-01-11
Status: Approved
Depends On: 01_requirements.md
---

# 외부 프로그램 실행 및 활성화 - Research Findings

## 1. Investigation Summary

| Question | Answer | Confidence |
|----------|--------|------------|
| Q1: 어떤 프로세스를 활성화할 것인가? | `Process.Start`로 반환된 객체의 `MainWindowHandle`을 사용하거나 이름으로 찾음 | High |
| Q2: Foreground Lock 회피 방법 | `ShowWindow` (Restore) + `SetForegroundWindow` 호출 | High |
| Q3: 창 제목 변동 대응 | 프로세스 핸들을 직접 관리하므로 창 제목에 의존하지 않음 | High |

## 2. Detailed Findings

### 2.1 Q1: 동일한 이름의 프로세스가 여러 개 실행 중인 경우 어떤 프로세스를 활성화할 것인가?

**Method**: Windows 프로세스 관리 방식 분석 및 기존 코드 검토.

**Findings**:
- 기존 Launcher 클래스들은 `Process? _process` 멤버 변수를 통해 자신이 실행한 프로세스를 관리하고 있음.
- 하지만 프로그램이 이미 실행 중인 상태에서 ChronoView가 재시작된 경우에는 해당 변수가 null임.
- 따라서 활성화 시나리오에서는 1) `_process` 변수가 유효한지 확인, 2) 유효하지 않다면 시스템 전체 프로세스 중 실행 경로가 일치하거나 이름이 일치하는 프로세스를 검색해야 함.

**Evidence**:
- `GeneralCameraLauncher.cs`의 `_process` 필드.
- `System.Diagnostics.Process.GetProcessesByName(name)` API.

**Conclusion**: 자신이 실행한 프로세스를 우선으로 하되, 없을 경우 이름 기반으로 검색하여 활성화함.

---

### 2.2 Q2: Win32 API 호출 시 Foreground Lock 회피 방법

**Method**: MSDN 및 StackOverflow의 Win32 API 제약 사항 검토.

**Findings**:
- `SetForegroundWindow`는 호출하는 프로세스가 현재 포커스를 가지고 있거나 특정 조건을 만족해야함.
- `AllowSetForegroundWindow`를 호출하거나, 단순히 창이 최소화된 경우 `ShowWindow(handle, SW_RESTORE)`를 먼저 호출한 뒤 `SetForegroundWindow`를 호출하면 대부분의 경우 사용자에게 창이 노출됨.
- 더 강력한 방법으로는 `AttachThreadInput`을 사용할 수 있으나, 단순 활성화에는 과도할 수 있음.

**Evidence**:
- [SetForegroundWindow documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)

**Conclusion**: `ShowWindow`와 `SetForegroundWindow` 조합을 사용하며, 유틸리티 클래스(`WindowActivationHelper`)를 만들어 관리함.

---

### 2.3 Q3: 외부 프로그램의 창 제목이 변동되는 경우 대응

**Method**: 프로세스 정보 기반 핸들 획득 방식 분석.

**Findings**:
- `Process.MainWindowHandle` 속성은 창 제목과 무관하게 해당 프로세스의 메인 창 핸들을 반환함.
- 단, 프로세스가 실행된 직후에는 창이 생성되지 않아 핸들이 0일 수 있으므로 `WaitForInputIdle` 호출이 필요할 수 있음.

**Conclusion**: 창 제목 대신 `MainWindowHandle`을 사용하므로 제목 변동 이슈에서 자유로움.

## 3. Code Analysis

### 3.1 Relevant Existing Code

| File | Component | Relevance | Notes |
|------|-----------|-----------|-------|
| `GeneralCameraLauncher.cs` | `LaunchAsync` | 실행 로직 수정 대상 | 활성화 로직 추가 필요 |
| `SetupWindowViewModel.cs` | `Launch*Command` | 버튼 로직 수정 대상 | 이미 Launcher를 호출 중임 |
| `SystemControlViewModel.cs` | - | 도달 범위 확장 대상 | 실행 버튼 추가 필요 |
| `WorkflowPanel.xaml` | - | UI 수정 대상 | 버튼 추가 필요 |

### 3.2 Glossary Check

**Existing Terms to Reuse**:
- `GeneralCameraLauncher`: Launcher for general camera capture programs
- `NirCameraLauncher`: Launcher for NIR camera capture programs
- `SystemControlViewModel`: ViewModel for system monitoring and control

**New Terms Needed**:
- `WindowActivationHelper`: Win32 API를 래핑하여 창을 활성화하는 유틸리티 클래스

## 4. Recommendations

### Primary Recommendation

1.  **WindowActivationHelper 구현**: Win32 API(`SetForegroundWindow`, `ShowWindow`)를 사용하는 정적 헬퍼 생성.
2.  **Launcher 확장**: `LaunchAsync` 내부에서 프로세스가 이미 실행 중인지 확인하는 로직 추가. 실행 중이면 `WindowActivationHelper.Activate(process)` 호출.
3.  **ViewModel 및 UI 업데이트**: `SystemControlViewModel`에 Launcher의 `LaunchAsync`를 호출하는 명령(Command)을 추가하고 `WorkflowPanel.xaml`에 버튼 배치.

---

## Approval

- [x] All questions from requirements addressed
- [x] Evidence provided for conclusions
- [x] Recommendations are actionable
- [x] Risks identified

**Next Step**: 03_plan.md
