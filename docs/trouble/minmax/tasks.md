# MinDelay/MaxDelay Migration - Tasks

## 목표
ExpectedDelay±Tolerance (대칭) → MinDelay/MaxDelay (비대칭) 범위로 변경

**예시**: Camera1 = Normal + 4~6초
- 기존: Delay=5, Tol=±1 (이론적으로만 4~6초)
- 신규: MinDelay=4, MaxDelay=6 (정확히 4~6초)

---

## Phase 1: Models ✅
- [x] DataSequenceItem.cs - MinDelay/MaxDelay 필드 추가
- [x] DataSequenceSettings.cs - GetMinDelay/GetMaxDelay 메서드 추가
- [x] Validation 규칙 업데이트

## Phase 2: ViewModels
- [ ] **DataSequenceItemViewModel.cs**
  - [ ] ExpectedDelay → MinDelay 변경
  - [ ] Tolerance → MaxDelay 변경
  - [ ] 속성 타입 int → double 변경
  
- [ ] **SettingsDialogViewModel.cs**
  - [ ] LoadSequenceSettings - 바인딩 변경 (line 633-634)
  - [ ] SaveSequenceSettings - 저장 로직 변경 (line 662-663)
  - [ ] ExecuteApplyPreset - 프리셋 로드 변경 (line 720-721)

## Phase 3: Views (UI) ✅
- [x] **SettingsDialog.xaml**
  - [x] "Delay" → "Min (s)" 레이블 변경
  - [x] "Tol" → "Max (s)" 레이블 변경
  - [x] 바인딩: ExpectedDelay → MinDelay
  - [x] 바인딩: Tolerance → MaxDelay
  - [x] Minimum 값: 1 → 0 허용 (기본적으로 0 허용)

## Phase 4: Presets ✅
- [x] **DataSequencePresets.cs**
  - [x] Preset 1: NormalFirst (5개 아이템)
  - [x] Preset 2: NirFirst (5개 아이템)
  - [x] Preset 3: CamerasFirst (5개 아이템)
  - [x] 각 아이템마다:
    - ExpectedDelaySeconds → MinDelaySeconds
    - TimeToleranceSeconds → MaxDelaySeconds
    - 값 재계산 (Delay±Tol → Min/Max)

## Phase 5: Core Logic
- [ ] **MonitoringOrchestrator.cs - FindMatchingExistingGroup**
  - [ ] GetTolerance() 호출 제거
  - [ ] GetMinDelay/GetMaxDelay 사용
  - [ ] Match 3 로직 변경:
    ```csharp
    // Before:
    var tolerance = GetTolerance(type);
    if (timeDelta <= tolerance)
    
    // After:
    var minDelay = GetMinDelay(type);
    var maxDelay = GetMaxDelay(type);
    var minTime = prevTime.AddSeconds(minDelay);
    var maxTime = prevTime.AddSeconds(maxDelay);
    if (newTime >= minTime && newTime <= maxTime)
    ```

## Phase 6: Tests
- [ ] **DataSequenceSettingsValidation.cs**
  - [ ] 10개 테스트 케이스 업데이트
  - [ ] ExpectedDelaySeconds → MinDelaySeconds
  - [ ] TimeToleranceSeconds → MaxDelaySeconds

## Phase 7: Configuration
- [ ] **appsettings.json**
  - [ ] DataSequenceSettings 기본값 업데이트
  - [ ] ExpectedDelaySeconds → MinDelaySeconds
  - [ ] TimeToleranceSeconds → MaxDelaySeconds

## Phase 8: Build & Test
- [ ] 빌드 확인
- [ ] 설정 UI 테스트
- [ ] 프리셋 적용 테스트
- [ ] Match 3 로직 테스트

---

## 현재 상태
- ✅ Phase 1 완료
- ❌ 빌드 오류: 57개
- 🎯 다음: Phase 2 - ViewModels

## 예상 값 변환 예시

### NIR
- Before: Delay=0, Tol=±10
- After: Min=0, Max=10 (NIR은 첫 번째라서 실제로는 무시됨)

### Normal  
- Before: Delay=0, Tol=±5
- After: Min=0, Max=1 (NIR + 0~1초)

### Camera1
- Before: Delay=5, Tol=±1  
- After: Min=4, Max=6 (Normal + 4~6초)

### Camera2
- Before: Delay=6, Tol=±1
- After: Min=1, Max=2 (Cam1 + 1~2초)

### Camera3
- Before: Delay=7, Tol=±1
- After: Min=1, Max=2 (Cam2 + 1~2초)
