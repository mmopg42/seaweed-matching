---
Task: launch_and_activate_external_programs
Created: 2026-01-11
Status: In Progress (Phase 2 Enhancement)
Depends On: 04_design.md
---

# 외부 프로그램 실행 및 활성화 - Implementation Tasks

## Progress Summary

| Phase | Total | Done | Remaining |
|-------|-------|------|-----------|
| Setup | 2 | 2 | 0 |
| Core | 2 | 2 | 0 |
| Integration | 1 | 1 | 0 |
| UI | 1 | 1 | 0 |
| Verification | 1 | 1 | 0 |
| **Total** | **7** | **7** | **0** |

---

## Phase 1: Setup

### 1.1 WindowActivationHelper 구현

- [x] `ChronoView/Helpers/WindowActivationHelper.cs` 생성
- [x] Win32 API P/Invoke(`SetForegroundWindow`, `ShowWindow`) 정의
- [x] `ActivateProcessWindow` 정적 메서드 구현
- [ ] **[NEW]** `GetForegroundWindow` P/Invoke 추가
- [ ] **[NEW]** `IsProcessWindowInForeground(Process)` 메서드 추가

**Verify**: ✅ `WindowActivationHelper` 클래스가 빌드 오류 없이 생성됨.

---

## Phase 2: Core Implementation

### 2.1 Launcher 클래스 확장 (GeneralCameraLauncher)

- [ ] `FindExistingProcess` private 메서드 추가
  - 경로 기반 프로세스 검색
  - `MainModule.FileName` 접근 시 try-catch 처리
- [ ] `IsActive`, `CurrentProcess` 속성 추가
- [ ] `TerminateAsync` 메서드 구현
- [ ] `LaunchAsync` 내부 로직 수정: 실행 중이면 활성화 후 복귀
- [ ] 상태 변경 이벤트(`StatusChanged`) 및 모니터링 연동

**Verify**: 프로그램이 이미 실행 중일 때 `LaunchAsync` 호출 시 해당 프로그램이 앞으로 나오는지 확인.

### 2.2 Launcher 클래스 확장 (NIR Launcher들)

- [ ] `NirCameraLauncher.cs`에 동일 로직 적용
- [ ] `Nir2CameraLauncher.cs`에 동일 로직 적용

**Verify**: 각 NIR 프로그램에 대해 활성화 기능 동작 확인.

---

## Phase 3: Integration (ViewModel)

### 3.1 SystemControlViewModel 업데이트

- [ ] `Nir2CameraStatus`, `Nir2CameraForeground` 속성 추가
- [ ] `_nir2CameraLauncher.StatusChanged` 이벤트 구독
- [ ] `LaunchGeneralCameraCommand`, `LaunchNir1CameraCommand`, `LaunchNir2CameraCommand` 명령 속성 추가
- [ ] 명령 실행 로직(`ExecuteLaunchAsync`) 구현:
  - 실행 중(`IsActive`)이고 맨 앞(`IsForeground`)이면 종료 확인 다이얼로그 표시
  - "예" 선택 시 `TerminateAsync` 호출
  - 그 외에는 `LaunchAsync` 호출

**Verify**: ViewModel 디버깅을 통해 명령 호출 및 상태 업데이트 확인. 종료 시나리오 검증.

---

## Phase 4: UI Implementation

### 4.1 WorkflowPanel.xaml 수정

- [x] NIR2 Camera 상태 행 추가 (Ellipse + TextBlock)
- [x] 각 카메라 상태 옆에 실행 버튼(▶) 추가:
  - GeneralCamera 버튼 → `LaunchGeneralCameraCommand`
  - NIR1 버튼 → `LaunchNir1CameraCommand`
  - NIR2 버튼 → `LaunchNir2CameraCommand`

**Verify**: ✅ 메인 화면 사이드바에 3개 버튼이 노출되고 클릭 가능한지 확인.

### 4.2 SetupWindow 확인 (추가 작업 불필요)

> [!NOTE]
> SetupWindow는 이미 동일한 Launcher 클래스를 사용하므로, Launcher 수정으로 자동 활성화 기능 획득. 추가 작업 불필요.

---

## Phase 5: Final Verification

### 5.1 Success Criteria Check

| Criterion | Status | Evidence |
|-----------|--------|----------|
| 프로그램 실행 | ✅ | Build 성공 |
| 프로그램 활성화 | ✅ | WindowActivationHelper 구현 완료 |
| 최소화 상태 복원 | ✅ | SW_RESTORE 적용 |
| NIR2 상태 표시 추가 | ✅ | UI Layout 확인 |
| 시스템 상태 버튼 추가 (3개) | ✅ | UI Layout 확인 |

---

## Approval

- [x] All tasks completed
- [x] All verifications pass
- [x] Success criteria met

**Completed**: 2026-01-11
