---
Task: launch_and_activate_external_programs
Created: 2026-01-11
Status: Approved
Summary: 외부 프로그램(General Camera, NIR 1, NIR 2) 실행 및 활성화(최상위 노출) 기능 구현
Research Required: Yes
---

# 외부 프로그램 실행 및 활성화 - Requirements

## 1. Goal

### 1.1 Primary Goal

사용자가 셋업 화면 및 메인 대시보드(시스템 상태)에서 버튼을 클릭하여 외부 프로그램(일반 카메라, NIR 1, NIR 2)을 실행하거나, 이미 실행 중인 경우 해당 프로그램을 최상위 화면으로 활성화(Activate)할 수 있도록 한다.

### 1.2 Success Criteria

- [ ] 프로그램이 실행 중이지 않을 때 버튼 클릭 시 지정된 경로의 프로그램이 실행됨.
- [ ] 프로그램이 이미 실행 중일 때 버튼 클릭 시 해당 프로그램 창이 시스템의 최상단(Foreground)으로 이동함.
- [ ] 프로그램이 최소화(Minimized) 상태인 경우 활성화 시 창 크기가 복원(Restore)됨.
- [ ] 셋업 화면(SetupWindow)에 관련 버튼 및 상태 표시가 유지되거나 개선됨.
- [ ] 메인 대시보드의 시스템 상태(WorkflowPanel) 섹션에 각 프로그램별 실행/활성화 버튼이 추가됨.
- [ ] 프로그램이 이미 최상위(Foreground)에 있을 때 버튼 클릭 시 종료 확인 다이얼로그가 표시됨.
- [ ] 종료 확인 다이얼로그에서 '예' 선택 시 프로그램이 정상 종료됨.
- [ ] 프로그램 실행 실패 시(경로 오류 등) 적절한 에러 메시지가 사용자에게 표시됨.

## 2. Constraints

### 2.1 Technical Constraints

- Windows 환경의 `Process` 객체 및 Win32 API(`SetForegroundWindow`, `ShowWindow` 등)를 사용하여 창 활성화를 구현해야 함.
- 기존 `GeneralCameraLauncher`, `NirCameraLauncher`, `Nir2CameraLauncher` 클래스의 구조를 유지하며 기능을 확장해야 함.
- 다중 모니터 환경에서도 활성화가 정상적으로 동작해야 함.

### 2.2 Business Constraints

- 사용자가 별도의 설정 없이 버튼 하나로 프로그램을 관리할 수 있어야 함.
- 프로그램 실행 경로는 기존 `ApplicationConfiguration`에 설정된 값을 사용함.

### 2.3 Non-Goals (Out of Scope)

- 외부 프로그램의 종료 기능(Kill Process)은 이번 작업 범위에 포함되지 않으나, **이미 프로그램이 최상위(Foreground)에 있는 상태에서 버튼을 다시 누른 경우에는 종료 여부를 묻고 종료하는 기능**은 포함된다.
- 외부 프로그램 내부의 특정 메뉴 조작 기능은 포함되지 않음.
- 외부 프로그램의 위치나 크기를 강제로 조정(Resize/Move)하는 기능은 활성화 목적 외에는 포함되지 않음.

## 3. Questions to Investigate

- [ ] Q1: 동일한 이름의 프로세스가 여러 개 실행 중인 경우 어떤 프로세스를 활성화할 것인가? (일반적으로 가장 최근 실행된 것 또는 PID를 저장하여 관리)
- [ ] Q2: Win32 API 호출 시 `SetForegroundWindow`가 포커스를 가져오지 못하는 제약 사항(Foreground Lock)을 어떻게 회피할 것인가?
- [ ] Q3: 외부 프로그램의 창 제목(Window Title)이 동적으로 변하는 경우에도 안정적으로 찾을 수 있는가?

## 4. Assumptions

- 외부 프로그램은 표준 Windows 창을 가지고 있는 애플리케이션임.
- 사용자는 `ApplicationConfiguration`에서 외부 프로그램의 경로를 올바르게 설정함.
- `Process.Start`로 실행된 프로세스 핸들을 통해 기본 창 핸들(MainWindowHandle)을 획득할 수 있음.

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| 기존 Launcher 클래스 구조 분석 | Done | AI |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| 현장 셋업 편의성 향상 | 작업 효율 저하 |

---

## Approval

- [x] Requirements reviewed and approved
- [x] Success criteria are measurable
- [x] Scope boundaries are clear
- [x] All blocking dependencies identified

**Next Step**: 03_plan.md
