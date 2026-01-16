---
Task: Change Abnormal Detection to Ratio Based
Created: 2026-01-14
Status: Pending
Depends On: 04_design.md
---

# Change Abnormal Detection to Ratio Based - Implementation Tasks

## 1. Core Models & Configuration
- [ ] **Update ApplicationConfiguration** <!-- id: 1 -->
    - Location: `Models/ApplicationConfiguration.cs`
    - Action: Remove `AbnormalPercentThreshold`.
    - Action: Add `AbnormalRatioThreshold` (double, default 0.3).
    - Note: This is a breaking change for `config.json`.
- [ ] **Update ImageMetadata** <!-- id: 2 -->
    - Location: `Models/ImageMetadata.cs`
    - Action: Remove `DevPctWidth`, `DevPctHeight` properties.

## 2. Interface Definitions
- [ ] **Update IImageProcessor** <!-- id: 3 -->
    - Location: `Core/ImageProcessing/IImageProcessor.cs`
    - Action: Add `(int Width, int Height) GetImageDimensions(string imagePath);`
- [ ] **Update IAbnormalDetector** <!-- id: 4 -->
    - Location: `Core/Analytics/IAbnormalDetector.cs`
    - Action: Update `AddAndCheckImage` return signature to `(bool IsAbnormal, double? RatioDiff)`.
    - Action: Remove overload without context if exists.
    - Action: Ensure `Threshold` documentation reflects Ratio difference.

## 3. Core Service Implementation
- [ ] **Update ImageProcessingService** <!-- id: 5 -->
    - Location: `Core/ImageProcessing/ImageProcessingService.cs`
    - Action: Implement `GetImageDimensions` using `Image.Identify` (ImageSharp).
    - Action: Remove initialization of `DevPctWidth/Height` in `GetImageMetadataAsync`.
- [ ] **Refactor AbnormalHistoryManager** <!-- id: 6 -->
    - Location: `Core/Analytics/AbnormalHistoryManager.cs`
    - Action: Change internal storage from `(int W, int H)` to `double Ratio`.
    - Action: Update `AddAndGet` to `Add(context, ratio)`.
    - Action: Handle loading of old history files (e.g., delete/reset if deserialization fails).
- [ ] **Update AbnormalDetectorService** <!-- id: 7 -->
    - Location: `Core/Analytics/AbnormalDetectorService.cs`
    - Action: Implement new Ratio-based detection logic.
    - Action: Remove `AdaptiveReset` logic.
    - Action: Ensure Thread Safety (`lock`, `ConcurrentDictionary`).
    - Action: Use `AbnormalRatioThreshold` from config.

## 4. UI & ViewModels
- [ ] **Update FileGroupViewModel** <!-- id: 8 -->
    - Location: `UI/ViewModels/FileGroupViewModel.cs`
    - Action: Use `_imageProcessor.GetImageDimensions` instead of `BitmapFrame`.
    - Action: Call `_abnormalDetector.AddAndCheckImage` with context.
    - Action: Rename `_cachedHeightDeviation` to `_cachedRatioDiff`.
    - Action: Update `AbnormalReason` message.
- [ ] **Update SettingsDialogViewModel** <!-- id: 9 -->
    - Location: `UI/ViewModels/SettingsDialogViewModel.cs`
    - Action: Update property to bind to `AbnormalRatioThreshold` (0.0 - 1.0 range).
- [ ] **Update SettingsDialog XAML** <!-- id: 10 -->
    - Location: `UI/Views/SettingsDialog.xaml`
    - Action: Connect text box to new VM property.
    - Action: Update label description (Explain "Difference of Ratio" vs "Percent").

## 5. Verification
- [ ] **Verify Build** <!-- id: 11 -->
    - Action: Build `ChronoView` project.
- [ ] **Manual Verification** <!-- id: 12 -->
    - Action: Run application.
    - Action: Check Settings (Default 0.3).
    - Action: Test with Normal images (ensure no false positives).
    - Action: Test with Cropped image (ensure Abnormal flag).
    - Action: Test with Zoomed image (ensure Normal flag).
