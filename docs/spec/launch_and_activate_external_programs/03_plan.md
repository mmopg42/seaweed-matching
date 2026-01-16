---
Task: launch_and_activate_external_programs
Created: 2026-01-11
Status: Approved
Depends On: 01_requirements.md, 02_research.md
---

# 외부 프로그램 실행 및 활성화 - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| 비실행 시 프로그램 실행 | Launcher 클래스의 `LaunchAsync` 확장 | 프로그램이 꺼진 상태에서 실행 확인 |
| 실행 시 프로그램 활성화 | `WindowActivationHelper` + `LaunchAsync` 확장 | 프로그램이 켜진 상태에서 활성화 확인 |
| 활성화 상태에서 버튼 클릭 시 종료 | `Launcher.TerminateAsync` + Dialog | 이미 활성화된 상태에서 클릭 시 다이얼로그 확인 및 종료 동작 검증 |
| 최소화 상태 복원 | `WindowActivationHelper` 내 `ShowWindow(SW_RESTORE)` | 최소화된 상태에서 버튼 클릭 시 복원 확인 |
| 코드 중복 제거 | `ProcessHelper` (공통 검색 로직) 및 제네릭 Command 핸들러 | 3개 Launcher 및 ViewModel의 중복 코드 감소 확인 |
| 권한/비트 호환성 이슈 해결 | `ProcessHelper`의 안전한 검색 로직 (Try-Catch) | 관리자 권한 프로세스 대상 테스트 |
| 앱 시작 시 상태 동기화 | `SystemControlViewModel` 초기화 로직 | 앱 시작 시 이미 켜져있는 프로그램 상태 반영 확인 |
| 셋업 화면 유지/개선 | `SetupWindowViewModel` (자동 반영) | Launcher 변경으로 인해 별도 수정 없이 동작 확인 |
| 시스템 상태 섹션에 버튼 추가 | `WorkflowPanel.xaml` 및 `SystemControlViewModel` 업데이트 | 메인 대시보드 사이드바에서 실행 버튼 노출 및 동작 확인 |
| 에러 메시지 표시 | Launcher의 예외 처리 및 서비스 결과 반환 | 잘못된 경로 설정 후 클릭 시 에러 메시지 노출 확인 |

---

## 1. Architecture Overview

### 1.1 System Context

이 기능은 ChronoView가 의존하는 외부 데이터 생성 프로그램(카메라 캡처 등)의 생명주기를 관리하고 사용자 편의를 위해 포커스를 제어하는 유틸리티 성격의 기능입니다. 유지보수성을 위해 공통 검색 로직을 헬퍼로 분리하여 중복을 최소화합니다.

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                      ChronoView                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌────────────────┐      ┌───────────────────────────┐  │
│  │ SetupWindow /  │      │ SystemControlViewModel    │  │
│  │ WorkflowPanel  │      │ (Dashboard Sidebar)       │  │
│  └───────┬────────┘      └─────────────┬─────────────┘  │
│          │                             │                │
│          ▼                             ▼                │
│  ┌───────────────────────────────────────────────────┐  │
│  │                [ Launcher Services ]               │  │
│  │ (GeneralCameraLauncher, NirCameraLauncher, etc.)  │  │
│  │   (Delegates Find/Activate to Helper)             │  │
│  └───────────────────────┬───────────────────────────┘  │
│                          │                              │
│                          ▼                              │
│  ┌───────────────────────────────────────────────────┐  │
│  │                  ProcessHelper                    │  │
│  │             (Safe Find, Activate Logic)           │  │
│  └───────────────────────┬───────────────────────────┘  │
│                          │                              │
└──────────────────────────┼──────────────────────────────┘
                           │
                           ▼
                ┌───────────────────┐
                │ External Programs │
                └───────────────────┘
```

### 1.3 Data Flow

```
App Start / User Click
    │
    ▼
ViewModel (Initialize / Execute)
    │
    ▼
Launcher.LaunchAsync()
    │
    ▼
ProcessHelper.FindProcessByPath() (Safe MainModule Access)
    │
    ├─► Found? ─► WindowActivationHelper.Activate()
    │
    └─► Not Found? ─► Process.Start()
    │
    ▼
Update Status (UI)
```

---

## 2. Components

### 2.1 New Components

| Component | Type | Location | Purpose |
|-----------|------|----------|---------|
| `WindowActivationHelper` | Static Class | `ChronoView.Helpers` | Win32 API를 사용한 창 활성화 로직 제공 |
| `ProcessHelper` | Static Class | `ChronoView.Helpers` | 프로세스 검색(권한 문제 호환) 및 관리 유틸리티 |

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `GeneralCameraLauncher` | `Core/ProgramLaunching/` | `ProcessHelper`를 사용하여 활성화 로직 위임 | No |
| `NirCameraLauncher` | `Core/ProgramLaunching/` | `ProcessHelper`를 사용하여 활성화 로직 위임 | No |
| `Nir2CameraLauncher` | `Core/ProgramLaunching/` | `ProcessHelper`를 사용하여 활성화 로직 위임 | No |
| `SystemControlViewModel` | `UI/ViewModels/` | Initialization 로직, Launch Command 추가 | No |
| `WorkflowPanel.xaml` | `UI/Controls/` | 실행/활성화 버튼(▶) 추가 | No |

---

## 3. Interface Definitions

### 3.1 Helpers

```csharp
namespace ChronoView.Helpers;

public static class WindowActivationHelper {
    public static void ActivateProcessWindow(Process process);
    public static bool IsProcessWindowInForeground(Process process); // NEW
}

public static class ProcessHelper {
    // Try-Catch로 MainModule 접근 제어
    public static Process? FindProcessByPath(string path);
}
```

### 3.2 SystemControlViewModel

```csharp
// 기존 ViewModel에 Command 및 초기화 로직 추가
// Generic Helper Method 활용 가능성 고려: ExecuteLaunchAsync<T>(...)
// [NEW] 종료 확인 로직 추가:
// if (launcher.IsActive && launcher.IsForeground) -> Show Dialog -> Terminate
```

**Responsibilities**:
- `ProcessHelper`: 안전한 프로세스 검색 (권한 예외 처리 포함).
- `Launcher Classes`: `ProcessHelper`를 호출하여 중복 코드 없이 '찾아서 활성화' 로직 수행.

---

## 4. Key Design Decisions

### 4.1 활성화 대상 프로세스 식별 방식

**Context**: `FindExistingProcess` 구현 시 `MainModule` 접근 권한 문제 발생 가능.

**Decision**: `ProcessHelper`에서 `MainModule` 접근을 `try-catch`로 감싸고, 실패 시 프로세스 이름과 `MainWindowHandle` 유무로 식별하는 Fallback 로직 적용.

### 4.2 중복 제거 전략

**Context**: 3개 Launcher의 코드가 90% 일치함.

**Decision**: User Feedback에 따라 Base Class 도입 대신 **Helper Class (`ProcessHelper`)**에 공통 로직(`FindProcessByPath`)을 위임하여 복잡성을 줄이고 Breaking Change를 방지함. ViewModel에서도 Generic Helper Method 패턴을 고려하여 중복을 줄임.

---

## 5. Configuration

기존 `ExternalProgramSettings`의 경로 정보를 그대로 사용함.

---

## 6. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `GeneralCameraLauncher` | 일반 카메라 실행 담당 |
| [x] | `NirCameraLauncher` | NIR 1 실행 담당 |
| [x] | `SystemControlViewModel` | 대시보드 사이드바의 시스템 상태 관리 |

---

## 7. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| SetForegroundWindow 실패 (OS 제약) | Medium | Low | `ShowWindow`를 먼저 호출하여 최소화 해제 후 재시도 |
| 권한 부족으로 Process.MainModule 접근 불가 | High | Medium | `ProcessHelper`에서 Try-Catch 예외 처리 및 Name 기반 Fallback |
| Launcher 상태 불일치 | Medium | Low | `FindProcessByPath`를 통해 외부 실행 상태 동기화 |

---

## 8. Open Questions

- [x] 활성화 성공 여부를 사용자에게 알려야 하는가? -> 성공 시 별도 메시지보다는 창이 앞으로 뜨는 것으로 충분, 실패 시에만 에러 메시지 표시.

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented with rationale
- [x] Glossary terms identified
- [x] All open questions resolved

**Next Step**: 04_design.md
