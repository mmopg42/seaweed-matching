---
Task: image_display_and_line_path_bugs
Created: 2026-01-09
Status: Draft
---

# Plan: Image Display and Line Path Bugs Fix

## Overview

Fix three bugs:
1. Image preview window layout broken
2. Missing image display logs
3. Line2 displaying Line1 path data

## Phase 1: Fix Image Preview Window (Priority: High)

### Changes

#### [MODIFY] ImagePreviewWindow.xaml
- Add `MinWidth="400"` and `MinHeight="300"` combined with `SizeToContent`
- Add fallback text/placeholder UI when `DisplayImage` is null

#### [MODIFY] ImagePreviewWindow.xaml.cs  
- Null check `image` param in constructor
- If null, set a default placeholder image or show a friendly message in DataContext

---

## Phase 2: Add Image Display Logging (Priority: Medium)

### Changes

#### [MODIFY] FileGroupViewModel.cs
- **Location**: `InitializeImagePaths()` (init or update time)
- **Logic**: 
  - If `NormalFolder` exists but `stitched_original.png` missing -> Log **Warning** (via `_uiLog`)
  - Do NOT add logs to `GetNormalImageSize()` to avoid spam

#### [MODIFY] FileGroupMediaLoader.cs
- **Location**: `LoadWithRetryAsync` and `ProcessRetryQueue`
- **Logic**:
  - When thumbnail generation succeeds -> Log **Debug** message (via `_uiLog`): `"Thumbnail loaded: {path}"`

---

## Phase 3: Diagnose & Fix Line Path Bug (Priority: High)

### Strategy: Diagnosis at Decision Point
Instead of post-facto logging, log the line determination logic as it happens.

### Changes

#### [MODIFY] GroupManager.cs
- **Location**: `DetermineLineNumber()`
- **Logic**:
  - Log the result of the line determination with the reason (e.g., "Matched Normal1 settings", "Defaulting to 1")
  - **Warning Log**: If falling back to "Line 1" because all Nir2/Cam4-6 checks failed (potentially indicating config error)
  - Log key config values used for checking (`MatchingSettings.Nir2Path`, etc.)

---

## Verification Plan

### Automated Tests
- Build verification

### Manual Verification

#### Bug #1 - Image Preview
1. Double-click image -> Check layout
2. Double-click group with MISSING image -> Check fallback UI (don't crash, don't collapse)

#### Bug #2 - Image Logs
1. Load normal folder -> Check "Thumbnail loaded" Debug log
2. Load folder without image -> Check "stitched_original.png not found" Warning log

#### Bug #3 - Line Path
1. Check "DetermineLineNumber" logs in real-time
2. Verify Line 2 files are correctly identified as Line 2
3. If warning appears ("Defaulting to 1"), check settings configuration

---

## Approval

- [x] Requirements defined
- [x] Research complete
- [ ] Plan approved by user

**Next Step**: Implementation
