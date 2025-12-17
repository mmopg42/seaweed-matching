# MinDelay/MaxDelay Migration - 구현 노트

## Phase 1 완료 (2025-12-15)

### DataSequenceItem 변경
```csharp
// Before:
public int ExpectedDelaySeconds { get; set; }
public int TimeToleranceSeconds { get; set; }

// After:
public double MinDelaySeconds { get; set; }
public double MaxDelaySeconds { get; set; }
```

### DataSequenceSettings 변경
```csharp
// Before:
public int GetExpectedDelay(DataType type)
public int GetTolerance(DataType type)

// After:
public double GetMinDelay(DataType type)
public double GetMaxDelay(DataType type)
```

### Validation 변경
```csharp
// Before:
- Delay >= 0
- Tolerance >= 1

// After:
- MinDelay >= 0
- MaxDelay >= 0
- MinDelay <= MaxDelay
```

---

## 다음: Phase 2 - ViewModels

### DataSequenceItemViewModel
위치: `ChronoView/UI/ViewModels/DataSequenceItemViewModel.cs`

필요한 변경:
1. 속성 이름 변경
2. 타입 변경 (int → double)
3. DisplayName 업데이트 (필요시)

### SettingsDialogViewModel  
위치: `ChronoView/UI/ViewModels/SettingsDialogViewModel.cs`

변경 위치:
- Line 633-634: LoadSequenceSettings
- Line 662-663: SaveSequenceSettings
- Line 720-721: ExecuteApplyPreset

---

## 중요 사항
- **Delay 기준**: "앞 순서 데이터"와의 차이
  - Normal: MinDelay/MaxDelay는 NIR 이후 시간
  - Cam1: MinDelay/MaxDelay는 Normal 이후 시간
  - Cam2: MinDelay/MaxDelay는 Cam1 이후 시간

- **첫 번째 타입**: MinDelay/MaxDelay 무시 (매칭 대상 없음)

- **Tolerance=0 허용**: 정확한 시간 매칭 가능
