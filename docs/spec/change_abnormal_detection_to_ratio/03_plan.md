---
Task: Change Abnormal Detection to Ratio Based
Created: 2026-01-14
Status: Approved
Depends On: 01_requirements.md, 02_research.md
---

# Change Abnormal Detection to Ratio Based - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Detect abnormal aspect ratio changes | `AbnormalDetectorService` (New Logic) | Review log with mismatched ratio images |
| Ignored valid zoom/size changes | `AbnormalDetectorService` (Ratio-based) | Verify zoomed image is NOT flagged |
| Configurable sensitivity | `ApplicationConfiguration` (`AbnormalRatioThreshold`) | Check Settings Dialog & Config file |
| Code cleanup (remove unused logic) | `ImageMetadata`, `IAbnormalDetector` | Verify code compilation & absence of fields |

## 1. Architecture Overview

### 1.1 System Context

The **Abnormal Detection** feature safeguards data quality by flagging images that deviate significantly from the expected shape (Aspect Ratio). This change shifts the logic from checking "Size Deviation %" (which falsely flags zoom/position changes) to "Ratio Deviation" (which only flags distortion/crop errors).

### 1.2 Data Flow

```
[Normal Image Input] (Width, Height)
    │
    ▼
[AbnormalDetectorService]
    │ 1. Calculate Ratio = W / H
    │ 2. Get Median Ratio from History
    │ 3. Calculate Diff = |Ratio - Median|
    │
    ▼
[Comparison]
    │ Diff > AbnormalRatioThreshold (0.3)?
    │
    ├─ YES ──► [Flag as Abnormal] (Ratio mismatch)
    │
    └─ NO ──► [Flag as Normal]
```

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `ApplicationConfiguration` | `Models/ApplicationConfiguration.cs` | Remove `AbnormalPercentThreshold`, Add `AbnormalRatioThreshold` | **Yes** (Config schema change) |
| `IAbnormalDetector` | `Core/Analytics/IAbnormalDetector.cs` | Update `AddAndCheckImage` return type, Logic properties | **Yes** (Interface change) |
| `AbnormalDetectorService` | `Core/Analytics/AbnormalDetectorService.cs` | Implement Ratio Logic, Remove Width/Height Dev logic | No |
| `FileGroupViewModel` | `UI/ViewModels/FileGroupViewModel.cs` | Adapt to new Interface, Update Abnormality Reason text | No |
| `SettingsDialogViewModel` | `UI/ViewModels/SettingsDialogViewModel.cs` | Bind to `AbnormalRatioThreshold` | No |
| `SettingsDialog` | `UI/Views/SettingsDialog.xaml` | Update Label and Binding | No |
| `ImageMetadata` | `Models/ImageMetadata.cs` | Remove `DevPctWidth`, `DevPctHeight`. (Optional: Add `RatioDiff`) | **Yes** (Json schema change) |
| `ImageProcessingService` | `Core/ImageProcessing/ImageProcessingService.cs` | Remove `DevPctWidth/Height`. Add `GetImageDimensions`. | No |
| `IImageProcessor` | `Core/ImageProcessing/IImageProcessor.cs` | Add `GetImageDimensions` method. | **Yes** (Interface change) |
| `AbnormalHistoryManager` | `Core/Analytics/AbnormalHistoryManager.cs` | Change storage from `W,H` to `Ratio`. | No |

## 3. Interface Definitions

### 3.1 IAbnormalDetector

```csharp
public interface IAbnormalDetector
{
    // Changed: Return type simplified to focus on Ratio Difference
    // old: (bool IsAbnormal, double? DevPctWidth, double? DevPctHeight)
    // new: (bool IsAbnormal, double? RatioDiff)
    (bool IsAbnormal, double? RatioDiff) AddAndCheckImage(int width, int height, string context);

    // Changed: Threshold meaning changes from % to Absolute Ratio Diff
    double Threshold { get; set; } 
    
    // Unchanged
    int WindowSize { get; set; }
    int MinSamples { get; set; }
}
```


### 3.3 IImageProcessor

```csharp
public interface IImageProcessor
{
    // NEW Helper to avoid duplication in VMs
    (int Width, int Height) GetImageDimensions(string imagePath);
    
    // ... existing methods ...
}

### 3.2 ImageMetadata

```csharp
public class ImageMetadata
{
    // ... existing fields ...

    // REMOVED
    // public double DevPctWidth { get; set; }
    // public double DevPctHeight { get; set; }
    
    // No new field added for now, as RatioDiff is calculated transiently for detection.
    // If persistence is needed, we can add `RatioDiff` later, but for now strict cleanup is preferred.
}
```

## 4. Key Design Decisions

### 4.1 Comparison Logic: Absolute Diff vs Percentage

**Context**: How to express the difference between Current Ratio (1.0) and Normal Ratio (1.33)?

**Options Considered**:
| Option | Pros | Cons |
|--------|------|------|
| A. Percentage (`25%`) | Familiar unit | Confusing calculation (Ratio of Ratio?) |
| B. Absolute Diff (`0.33`) | Simple, Direct subtraction | "0.3" might feel abstract to users initially |

**Decision**: **Option B (Absolute Diff)**

**Rationale**:
- Users confirmed "Ratio" is the key metric.
- `|1.33 - 1.0| = 0.33` is unambiguous.
- Avoids complexity of explaining "Percentage of a Ratio".

### 4.2 Configuration Migration

**Context**: Users have existing `config.json` with `AbnormalPercentThreshold`.

**Decision**: **Breaking Change (Manual Reset likely)**
- Since the logic changes fundamentally, the old value (12.0) is meaningless for the new logic (range 0.0 ~ 1.0).
- We will introduce a NEW key `AbnormalRatioThreshold`.
- Old key `AbnormalPercentThreshold` will be ignored/removed.
- Default value `0.3` will apply for new/migrated configs.

## 5. Configuration

### 5.1 New Configuration Keys

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `MatchingSettings.AbnormalRatioThreshold` | double | 0.3 | Threshold for Ratio Difference. If `|CurrentRatio - MedianRatio| > 0.3`, it is abnormal. |

### 5.2 Deleted Keys

| Key | Reason |
|-----|--------|
| `MatchingSettings.AbnormalPercentThreshold` | Replaced by Ratio logic |

## 6. Glossary Updates

### New Terms

| Term | Type | Definition |
|------|------|------------|
| `RatioDiff` | Metric | Absolute difference between current Aspect Ratio and Median Aspect Ratio. |
| `AbnormalRatioThreshold` | Setting | The limit for `RatioDiff` to trigger an abnormal alert. |

## 7. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| User confusion on new threshold | Medium | Low | Add tooltip in Settings UI explaining "0.3 means 1.33 vs 1.0 difference". |
| Valid 90-degree rotation flagged | High | Medium | 90-degree rotation changes ratio (4:3 -> 3:4, 1.33 -> 0.75, diff 0.58). This SHOULD be flagged as abnormal usually. |

## 8. Open Questions

- [x] Need to handle Landscape/Portrait flipping? -> Yes, standard ratio check will flag this (Diff ~0.6). This is desired behavior for alignment checks.

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented
- [x] Glossary terms identified

**Next Step**: 04_design.md
