---
Task: image_display_and_line_path_bugs
Created: 2026-01-09
Status: Draft
---

# Tasks: Image Display and Line Path Bugs

## 1. Line Path Diagnosis (Priority: High)
> Implement first to enable debugging while working on other tasks.

- [ ] **[GroupManager]** Update `DetermineLineNumber` signature (or internal usage) if needed (no signature change expected, just logic).
- [ ] **[GroupManager]** Add `RaiseLog` calls to trace config checking logic (Debug level).
- [ ] **[GroupManager]** Add logic to detect "Suspicious Default" (e.g., path contains 'nir2' but matched Line 1).
- [ ] **[GroupManager]** Add Warning `RaiseLog` when suspicious default is detected.
- [ ] **[Verification]** Run app, drop file in Nir2 folder, verify logs in UI panel.

## 2. Image Display Logging (Priority: Medium)

- [ ] **[FileGroupViewModel]** Locate `InitializeImagePaths`.
- [ ] **[FileGroupViewModel]** Add `File.Exists` check for `stitched_original.png`.
- [ ] **[FileGroupViewModel]** Invoke `_uiLog` (Warning) if file missing.
- [ ] **[FileGroupMediaLoader]** Locate `LoadWithRetryAsync`.
- [ ] **[FileGroupMediaLoader]** Add `_uiLog` (Debug) invocation on success callback.
- [ ] **[Verification]** Load bad/good folders, verify Warning/Debug logs in UI panel.

## 3. Image Preview Fix (Priority: High)

- [ ] **[ImagePreviewWindow.xaml]** Add `MinWidth="400" MinHeight="300"` to Window.
- [ ] **[ImagePreviewWindow.xaml]** Add `TargetNullValue` or Fallback content to Image binding (optional, or handle in code).
- [ ] **[ImagePreviewWindow.xaml.cs]** In Constructor, check `if (image == null)` (Already implemented / handled by binding).
- [ ] **[ImagePreviewWindow.xaml.cs]** If null, set DataContext to placeholder state (Already implemented / handled by binding).
- [ ] **[Verification]** Double-click valid image (check layout). Force null image call (check fallback).

## 4. Final Review

- [ ] Run full build (`dotnet build`).
- [ ] Manual verification of all 3 bugs using scenarios in Design doc.
