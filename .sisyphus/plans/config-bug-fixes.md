# Plan: Configuration Bug Fixes

## Context

### Original Request
사용자가 "설정값들이 잘못 저장되는 것 같아요. 문법이 잘못되다 보니, 설정 값이 자꾸 초기화되요. 전수조사해주세요"라고 요청

### Investigation Summary

전수 조사를 통해 다음 3개의 치명적 버그 발견됨:

1. **DataSequencePresets.CamerasFirst()의 중복된 DataType**
2. **ApplicationConfiguration.cs의 잘못된 XML 주석** (JSON 파싱 오류 유발)
3. **SettingsDialogViewModel의 유효성 검사 타이밍 문제**

이 버그들로 인해 설정 저장이 실패하고, 앱이 시작될 때마다 초기화되는 현상이 발생함.

---

## Work Objectives

### Core Objective
설정 파일 저장 실패와 초기화 문제를 해결하여 사용자의 설정값이 안정적으로 유지되도록 함

### Concrete Deliverables
- 수정된 DataSequencePresets.cs (중복 DataType 제거)
- 수정된 ApplicationConfiguration.cs (잘못된 주석 수정)
- 개선된 SettingsDialogViewModel.cs (유효성 검사 타이밍 수정)
- 모든 수정 사항이 포괄된 유닛 테스트

### Definition of Done
- [ ] DataSequencePresets.CamerasFirst() 메서드가 중복 없는 유한한 DataType들로 수정됨
- [ ] ApplicationConfiguration.cs의 잘못된 XML 주석이 올바른 주석 형식으로 수정됨
- [ ] SettingsDialogViewModel에서 유효성 검사가 config 수정 전에 수행되도록 개선됨
- [ ] 모든 수정이 통합 테스트를 통과함 (test: ConfigurationPersistencePropertyTests)
- [ ] 설정 파일 저장/로드가 정상 동작하는지 수동으로 검증됨

### Must Have
- 중복된 DataType이 제거된 CamerasFirst() 프리셋
- JSON 직렬화에 영향을 주지 않는 올바른 주석 형식
- 유효성 검사 실패 시 config 객체가 수정되지 않도록 보호
- System.Text.Json 호환성 유지

### Must NOT Have (Guardrails)
- 다른 프리셋들(NormalFirst, NirFirst) 수정하지 않음
- DataSequenceSettings.Validate() 로직 변경하지 않음
- ApplicationConfiguration의 다른 속성 수정하지 않음
- 사용자 인터페이스(UI 텍스트 등) 수정하지 않음

---

## Verification Strategy

### Test Decision
- **Infrastructure exists**: YES
- **User wants tests**: YES (Tests-after)

### Manual QA Procedures

수정된 각 파일에 대한 수동 검증 절차:

#### For DataSequencePresets.CamerasFirst() 수정:
1. 앱을 시작하고 Settings 대화상자 엶
2. "Sequence" 탭으로 이동
3. "Cameras First" 프리셋을 선택
4. 변경 사항을 적용 (OK 클릭)
5. 앱을 재시작하고 config.json 열어서 확인:
   ```bash
   type "%LocalAppData%\prische\ChronoView\config.json"
   ```
6. 예상 출력: "Cam1, Cam2, Normal, NIR, Cam3" 순서로 5개의 항목 있어야 함
7. DataSequenceSettings.Validate()가 성공해야 함 (예외 없음)

#### For ApplicationConfiguration.cs 주석 수정:
1. UseCameraSubfolderNormal2 설정값을 변경 (체크박스 토글)
2. Settings를 저장 (OK 클릭)
3. config.json 열어서 확인:
   ```bash
   type "%LocalAppData%\prische\ChronoView\config.json"
   ```
4. 예상 출력: 주석이 JSON에 포함되지 않아야 함

#### For SettingsDialogViewModel 타이밍 수정:
1. 유효하지 않은 순서로 Cam1/Cam2 값을 입력 (UI에서 가능하도록)
2. "Apply" 클릭
3. 예상 출력: "Validation failed" 메시지가 나타나야 함

---

## Task Flow

```
1. DataSequencePresets.cs 수정
   ↓
2. ApplicationConfiguration.cs 수정
   ↓
3. SettingsDialogViewModel.cs 수정
   ↓
4. 통합 테스트 실행
```

## Parallelization

| Group | Tasks | Reason |
|-------|-------|---------|
| A | 1, 2 | 독립적 파일 수정 (서로 의존성 없음) |
| B | 3 | SettingsDialogViewModel 수정 (다른 수정들에 의존적) |

| Task | Depends On | Reason |
|------|------------|--------|
| 3 | 1, 2 | 다른 프리셋 로직 수정 후 적용 |

---

## TODOs

### Task 1: Fix DataSequencePresets.CamerasFirst() Duplicate DataType Bug

**What to do**:
DataSequencePresets.CamerasFirst() 메서드(119-167줄)에서 중복된 DataType.Cam1과 DataType.Cam2를 정상적인 데이터 타입(DataType.Normal, DataType.NIR)으로 수정

**Current Buggy Code**:
```csharp
// Lines 143, 149 - DUPLICATE!
new DataSequenceItem { Type = DataType.Cam1, Order = 3, ... },  // ❌ Duplicate
new DataSequenceItem { Type = DataType.Cam2, Order = 4, ... }  // ❌ Duplicate
```

**Fixed Code**:
```csharp
// Lines 143, 149 - REPLACED!
new DataSequenceItem { Type = DataType.Normal, Order = 3, ... },  // ✅ Normal
new DataSequenceItem { Type = DataType.NIR, Order = 4, ... }      // ✅ NIR
```

**Must NOT do**:
- 다른 프리셋들(NormalFirst, NirFirst) 수정하지 않음
- Order 값을 변경하지 않음 (Order 1-5 유지)
- MinDelaySeconds/MaxDelaySeconds 값을 변경하지 않음 (기존 값 유지)
- 프리셋 이름(CamerasFirst)을 변경하지 않음

**Acceptance Criteria**:

**If TDD (tests enabled):**
- [ ] DataSequencePresetsTests.cs 파일 생성: `ChronoView.Tests/Presets/DataSequencePresetsTests.cs`
- [ ] 테스트 메서드: `TestCamerasFirst_NoDuplicates()`
- [ ] 테스트 코드:
  ```csharp
  [Fact]
  public void CamerasFirst_NoDuplicateTypes()
  {
      var settings = DataSequencePresets.CamerasFirst();
      var settings.Validate(out var errors);
      Assert.Empty(errors);  // No duplicate type errors
      Assert.Equal(5, settings.Sequence.Count);  // 5 items (not 7)

      // Verify correct types
      var types = settings.Sequence.Select(x => x.Type).ToList();
      Assert.Contains(DataType.Cam1, types);
      Assert.Contains(DataType.Cam2, types);
      Assert.Contains(DataType.Cam3, types);
      Assert.Contains(DataType.Normal, types);
      Assert.Contains(DataType.NIR, types);
      Assert.False(types.Any(x => x == DataType.Cam4 || x == DataType.Cam5));
  }
  ```
- [ ] `dotnet test` 실행: `dotnet test --filter "FullyQualifiedName*DataSequencePresetsTests"`
- [ ] Expected: PASS (1 test, 0 failures)

**Manual Execution Verification (ALWAYS include, even with tests):**

*For UI verification:*
- [ ] 앱 시작 및 Settings 대화상자 열기
- [ ] "Sequence" 탭 선택 → "Cameras First" 프리셋 선택
- [ ] UI에서 현재 시퀀스 확인: "Cam1(0s) → Cam2(1-2s) → Normal(0-1s) → NIR(0-1s) → Cam3(0-1s)" 로 변경되어야 함
- [ ] 스크린샷: `.sisyphus/evidence/task1-preset-fix.png` (변경 전후 비교)

*For config.json verification:*
- [ ] config.json 파일 열기:
  ```bash
  type "%LocalAppData%\prische\ChronoView\config.json"
  ```
- [ ] dataSequenceSettings 섹션 확인:
  ```json
  "dataSequenceSettings": {
    "sequence": [
      { "type": "Cam1", "order": 1, ... },
      { "type": "Cam2", "order": 2, ... },
      { "type": "Normal", "order": 3, ... },  // ✅ Should exist
      { "type": "NIR", "order": 4, ... },    // ✅ Should exist
      { "type": "Cam3", "order": 5, ... }
    ]
  }
  ```
- [ ] 예상: 5개의 항목 (Cam1, Cam2, Normal, NIR, Cam3), 중복 없음

*For validation check:*
- [ ] 프리셋 적용 후 "Validation failed" 메시지가 나타나지 않아야 함
- [ ] Settings 창이 정상적으로 닫혀야 함

**Evidence Required:**
- [ ] 스크린샷 저장됨 (CamerasFirst 프리셋 UI)
- [ ] config.json 내용 복사 (dataSequenceSettings 섹션)
- [ ] 테스트 결과 출력 복사 (dotnet test 실행 결과)

**Commit**: YES
- Message: `fix(config): resolve duplicate DataType in CamerasFirst preset`
- Files: `ChronoView/Models/DataSequencePresets.cs`
- Pre-commit: `dotnet test --filter "*DataSequencePresetsTests"`

---

### Task 2: Fix ApplicationConfiguration.cs Malformed XML Comment

**What to do**:
ApplicationConfiguration.cs의 188-191줄에 있는 잘못된 XML 주석 `\u003csummary\u003e`를 올바른 C# XML 문서 주석 형식(`/// <summary>` 및 `/// </summary>`)으로 수정

**Current Buggy Code**:
```csharp
// Lines 188, 190 - MALFORMED!
// \u003csummary\u003e
// Use camera subfolder for Normal2 path.
// \u003c/summary\u003e
public bool UseCameraSubfolderNormal2 { get; set; } = false;
```

**Fixed Code**:
```csharp
// Lines 188, 192 - FIXED!
/// <summary>
/// Use camera subfolder for Normal2 path.
/// </summary>
public bool UseCameraSubfolderNormal2 { get; set; } = false;
```

**Must NOT do**:
- 속성 이름 변경하지 않음
- 기본값 변경하지 않음 (false 유지)
- 다른 주석 수정하지 않음

**Acceptance Criteria**:

**If TDD (tests enabled):**
- [ ] ApplicationConfigurationTests.cs 파일 수정 (이미 존재함)
- [ ] 새 테스트 메서드 추가: `Test_InvalidCommentFormat_DoesNotCauseJsonError()`
- [ ] 테스트 코드:
  ```csharp
  [Fact]
  public void InvalidCommentFormat_DoesNotCauseJsonError()
  {
      var config = new ApplicationConfiguration
      {
          UseCameraSubfolderNormal2 = true  // Set a non-default value
      };

      // Serialize to JSON
      var options = new JsonSerializerOptions
      {
          WriteIndented = true,
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };
      var json = JsonSerializer.Serialize(config, options);

      // Deserialize back
      var deserialized = JsonSerializer.Deserialize<ApplicationConfiguration>(json, options);

      // Verify value is preserved (comments should not affect serialization)
      Assert.True(deserialized.UseCameraSubfolderNormal2);
  }
  ```
- [ ] `dotnet test` 실행: `dotnet test --filter "FullyQualifiedName*ApplicationConfigurationTests"`
- [ ] Expected: PASS (1 test, 0 failures)

**Manual Execution Verification (ALWAYS include, even with tests):**

*For config.json verification:*
- [ ] Settings 대화상자에서 UseCameraSubfolderNormal2 토글 (true ↔ false)
- [ ] "Apply" 또는 "Save" 클릭하여 설정 저장
- [ ] config.json 파일 열기:
  ```bash
  type "%LocalAppData%\prische\ChronoView\config.json"
  ```
- [ ] useCameraSubfolderNormal2 속성 확인:
  ```json
  {
    "matchingSettings": {
      "useCameraSubfolderNormal2": true  // 또는 false
    }
  }
  ```
- [ ] 예상: 속성값이 올바르게 저장됨 (true/false)
- [ ] 예상: 잘못된 주석(`// \u003csummary\u003e`)이 JSON에 포함되지 않아야 함

*For serialization verification:*
- [ ] 앱 재시작 후 설정이 정상적으로 로드되어야 함 (예외 없음)

**Evidence Required:**
- [ ] config.json 내용 복사 (matchingSettings 섹션, useCameraSubfolderNormal2 속성)
- [ ] 테스트 결과 출력 복사 (dotnet test 실행 결과)

**Commit**: YES
- Message: `fix(config): correct malformed XML comment format`
- Files: `ChronoView/Models/ApplicationConfiguration.cs`
- Pre-commit: `dotnet test --filter "*ApplicationConfigurationTests"`

---

### Task 3: Improve SettingsDialogViewModel Validation Timing

**What to do**:
SettingsDialogViewModel.SaveSequenceSettings() 메서드(875-900줄)에서 유효성 검사를 config 객체 수정 전으로 이동하여, 검사 실패 시 원래 설정을 보호하도록 개선

**Current Code**:
```csharp
// Lines 878-887 - BUGGY TIMING!
var newSequence = SequenceItems.Select(vm => new DataSequenceItem { ... }).ToList();

// Modify config BEFORE validation
_configuration.DataSequenceSettings.Sequence = newSequence;  // ❌ Already modified!

// Validate AFTER modification
if (!_configuration.DataSequenceSettings.Validate(out var errors))
{
    _logger.LogWarning("Validation failed...");
    WpfMessageBox.Show($"Validation failed:\n{errors}", ...);
    return;  // Return but config is already corrupted!
}
```

**Fixed Code**:
```csharp
// Lines 878-900 - IMPROVED TIMING!
var newSequence = SequenceItems.Select(vm => new DataSequenceItem { ... }).ToList();

// Validate BEFORE modification
if (!ValidateNewSequence(newSequence, out var errors))
{
    _logger.LogWarning("Data sequence validation failed: {Errors}", string.Join("\n", errors));
    WpfMessageBox.Show($"Validation failed:\n{string.Join("\n", errors)}", "Invalid Sequence", MessageBoxButton.OK, MessageBoxImage.Warning);
    return;  // Return without modifying config
}

// Only modify config if validation passes
_configuration.DataSequenceSettings.Sequence = newSequence;
_logger.LogInformation("Data sequence settings saved");
```

**Helper method to add**:
```csharp
/// <summary>
/// Validate new sequence without modifying config object.
/// </summary>
private bool ValidateNewSequence(List<DataSequenceItem> newSequence, out List<string> errors)
{
    var tempSettings = new DataSequenceSettings { Sequence = newSequence };
    return tempSettings.Validate(out errors);
}
```

**Must NOT do**:
- 다른 SaveToConfiguration 메서드 수정하지 않음
- 유효성 검사 로직 변경하지 않음
- 사용자 인터페이스 텍스트 변경하지 않음

**Acceptance Criteria**:

**If TDD (tests enabled):**
- [ ] SettingsDialogViewModelTests.cs 수정 (이미 존재함)
- [ ] 새 테스트 메서드 추가: `Test_ValidationFailure_DoesNotCorruptConfig()`
- [ ] 테스트 코드:
  ```csharp
  [Fact]
  public void ValidationFailure_DoesNotCorruptConfig()
  {
      var configManager = Substitute.For<IConfigurationManager>()
          .Setup(cm => cm.LoadConfiguration<ApplicationConfiguration>())
          .Returns(new ApplicationConfiguration
          {
              DataSequenceSettings = new DataSequenceSettings
              {
                  Sequence = new List<DataSequenceItem>
                  {
                      new DataSequenceItem { Type = DataType.Cam1, Order = 1, Enabled = true },
                      new DataSequenceItem { Type = DataType.Cam2, Order = 2, Enabled = true }
                  }
              }
          });

      var viewModel = new SettingsDialogViewModel(configManager.Object, _logger);
      viewModel.SequenceItems.Add(new DataSequenceItemViewModel
      {
          Type = DataType.Normal,
          Order = 3,
          MinDelay = 0,
          MaxDelay = 1,
          Enabled = true
      });

      // Simulate validation failure (e.g., duplicate type)
      var originalSequence = viewModel.GetConfiguration().DataSequenceSettings.Sequence;
      var originalCam1Count = originalSequence.Count(x => x.Type == DataType.Cam1);  // Should be 1

      viewModel.SaveSequenceSettings();

      // Verify original sequence is NOT corrupted (should still have Cam1)
      var savedSequence = configManager.Object.LoadConfiguration<ApplicationConfiguration>().DataSequenceSettings.Sequence;
      var savedCam1Count = savedSequence.Count(x => x.Type == DataType.Cam1);  // Should still be 1

      Assert.Equal(originalCam1Count, savedCam1Count);  // Config should NOT be corrupted
  }
  ```
- [ ] `dotnet test` 실행: `dotnet test --filter "FullyQualifiedName*SettingsDialogViewModelTests"`
- [ ] Expected: PASS (1 test, 0 failures)

**Manual Execution Verification (ALWAYS include, even with tests):**

*For UI verification:*
- [ ] 앱 시작 및 Settings 대화상자 열기
- [ ] "Sequence" 탭으로 이동
- [ ] 유효하지 않은 순서로 시퀀스 입력 (UI에서 가능하도록)
  - 예: Cam1(Cam1), Order=1
  - 예: Cam2(Cam2), Order=1 (Cam1과 중복) → "Validation failed" 메시지 예상
  - 예: Cam2(Cam2), Order=2 → 성공
- [ ] "Apply" 클릭
- [ ] 검증:
  - 유효하지 않은 순서 입력 시: "Validation failed" 메시지가 나타남
  - 유효한 순서 입력 시: 저장 성공

*For config.json verification:*
- [ ] config.json 파일 열기:
  ```bash
  type "%LocalAppData%\prische\ChronoView\config.json"
  ```
- [ ] dataSequenceSettings 섹션 확인:
  ```json
  // Case 1: Invalid input rejected
  {
    "dataSequenceSettings": {
      "sequence": [
        // Original valid sequence preserved
        { "type": "Cam1", "order": 1, ... },
        { "type": "Cam2", "order": 2, ... }
      ]
    }
  }
  ```
- [ ] 예상: 유효하지 않은 입력이 거부되어 기존 설정이 보존됨

**Evidence Required:**
- [ ] 스크린샷 저장됨 (Settings 대화상자에서 Validation failed 메시지)
- [ ] config.json 내용 복사 (validation failure 시 원래 설정 보존 확인)
- [ ] 테스트 결과 출력 복사 (dotnet test 실행 결과)

**Commit**: YES
- Message: `fix(viewmodel): protect config from corruption on validation failure`
- Files: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`
- Pre-commit: `dotnet test --filter "*SettingsDialogViewModelTests"`

---

## Commit Strategy

| After Task | Message | Files | Verification |
|------------|---------|-------|--------------|
| 1 | `fix(config): resolve duplicate DataType in CamerasFirst preset` | `ChronoView/Models/DataSequencePresets.cs` | `dotnet test --filter "*DataSequencePresetsTests"` |
| 2 | `fix(config): correct malformed XML comment format` | `ChronoView/Models/ApplicationConfiguration.cs` | `dotnet test --filter "*ApplicationConfigurationTests"` |
| 3 | `fix(viewmodel): protect config from corruption on validation failure` | `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs` | `dotnet test --filter "*SettingsDialogViewModelTests"` |

---

## Success Criteria

### Verification Commands
```bash
# Run all tests
dotnet test ChronoView.Tests/ChronoView.Tests.csproj

# Verify config.json structure
type "%LocalAppData%\prische\ChronoView\config.json"
```

### Final Checklist
- [ ] DataSequencePresets.CamerasFirst()가 중복 없는 5개의 데이터 타입(Cam1, Cam2, Normal, NIR, Cam3)을 생성함
- [ ] ApplicationConfiguration.cs의 잘못된 XML 주석이 올바른 C# 문서 주석 형식으로 수정됨
- [ ] SettingsDialogViewModel에서 유효성 검사 실패 시 원래 설정을 보호하도록 개선됨
- [ ] 모든 수정이 통합 테스트를 통과함 (0 failures)
- [ ] config.json 파일이 정상적인 JSON 구조를 가지고 있음
- [ ] 설정 파일 저장 및 로드가 정상적으로 동작하는지 수동으로 검증됨

---

## References

### Pattern References (existing code to follow):

**For CamerasFirst() fix**:
- `DataSequencePresets.cs:13-61` - NormalFirst() 메서드 (올바른 구조 참조)
  - 5개의 항목: Normal(1), NIR(2), Cam1(3), Cam2(4), Cam3(5)
  - 모든 Type이 유니하게 유지됨
  - Order가 1-5까지 순차적으로 할당됨

**For XML comment fix**:
- `ChronoView/Models/ApplicationConfiguration.cs:5-53` - 다른 속성의 주석 형식 참조
  - 모든 속성이 `/// <summary>` 형식을 사용함

**For validation timing fix**:
- `ChronoView/Core/Configuration/ConfigurationManager.cs:225-254` - ValidateApplicationConfiguration() 메서드 참조
  - config 수정 전에 유효성 검사를 먼저 수행하는 패턴
  - ConfigurationException을 던지는 패턴

### API/Type References (contracts to implement against):

**For DataTypes**:
- `ChronoView/Models/DataSequenceSettings.cs:12-23` - DataType 열거형 정의
  - NIR, Normal, Cam1, Cam2, Cam3, Cam4, Cam5, Cam6, Camera

**For Validation**:
- `ChronoView/Models/DataSequenceSettings.cs:147-198` - Validate() 메서드 구현
  - 중복된 Order/Type 감지 로직 (lines 161-178)
  - 지연 범위 유효성 검사 로직 (lines 181-195)

### Test References (testing patterns to follow):

**For config persistence tests**:
- `ChronoView.Tests/Core/Configuration/ConfigurationPersistencePropertyTests.cs` - 속성 기반 테스트 예시
  - Round-trip 직렬화 테스트 패턴
  - 100번 반복 실행
  - Write-Read-Verify 접근 방식

**For config validation tests**:
- `ChronoView.Tests/Core/Configuration/ConfigurationCompletenessPropertyTests.cs` - 유효성 테스트 예시
  - ConfigurationValidationException이 던져지는지 확인
  - Assert.ThrowsAsync 패턴 사용

### Documentation References (specs and requirements):

- `ChronoView/docs/architecture/module_configuration.md` - 설정 모듈 문서
  - 설정 저장/로드 메커니즘 설명

### External References (libraries and frameworks):

**System.Text.Json**: 직렬화 옵션
- JsonNamingPolicy.CamelCase - 속성 이름 정책
- WriteIndented - 가독성 옵션
- DefaultIgnoreCondition.WhenWritingNull - null 값 무시

**Why Each Reference Matters** (explain the relevance):
- DataSequencePresets.NormalFirst()와 동일한 패턴으로 5개의 유니한 DataType으로 수정해야 함
- XML 주석(`/// <summary>`)이 System.Text.Json 직렬화에 영향을 주지 않으므로 올바른 형식 사용해야 함
- ValidateApplicationConfiguration()에서 config 수정 전에 유효성을 검사하는 패턴을 따라야 함
