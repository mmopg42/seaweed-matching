---
Task: image_display_and_line_path_bugs
Created: 2026-01-09
Status: Draft
---

# Design: Image Display and Line Path Bugs

## 1. Architecture Changes

No structural architecture changes. Modifications are limited to existing ViewModels and Service logic to improve robustness and logging transparency.

---

## 2. Detailed Component Design

### 2.1 [UI] Image Preview Window
**File**: `ChronoView/UI/Views/ImagePreviewWindow.xaml` / `.xaml.cs`

**Layout Strategy**:
- Maintain `SizeToContent="WidthAndHeight"` for optimal sizing with valid images.
- Add Safety Constraints:
  - `MinWidth="400"`
  - `MinHeight="300"`
  - `SizeToContent` will respect these minimums if content is smaller.

**Null Handling (Code-behind)**:
- **Input**: `ImageSource image`
- **Logic**:
  ```csharp
  if (image == null) {
      // 1. Log warning (internal/debug)
      // 2. Set DataContext with placeholder info
      DataContext = new { DisplayImage = (ImageSource)null, ImageTitle = title + " (No Image)" };
      // 3. UI triggers Fallback/TargetNullValue or visibility toggle
  }
  ```
- **XAML Fallback**:
  - Use `TargetNullValue` in binding or a Trigger to show "Image Not Available" text when Source is null.

### 2.2 [Logic] Image Display Logging
**Goal**: Visibility into image loading process via UI Log Panel.

#### Component: FileGroupViewModel
**Method**: `InitializeImagePaths()`
**Logic**:
```csharp
var stitchedPath = Path.Combine(NormalFolder, "stitched_original.png");
if (File.Exists(stitchedPath)) {
    // Current logic
} else {
    // NEW: Warning Log to UI
    _uiLog?.Invoke(LogSeverity.Warning, "ImageLoader", 
        $"[이미지누락] 폴더 존재하나 이미지 없음: {NormalFolder}");
}
```

#### Component: FileGroupMediaLoader
**Method**: `LoadWithRetryAsync` & `ProcessRetryQueue`
**Logic**:
```csharp
if (thumb != null) {
    ctx.Callback?.Invoke(thumb);
    // NEW: Debug Log to UI
    _uiLog?.Invoke(LogSeverity.Debug, "Thumbnail",
        $"[썸네일로드] 성공: {Path.GetFileName(ctx.Path)}");
}
```

### 2.3 [Logic] Line Path Diagnosis
**Goal**: Diagnose why Line 2 files might default to Line 1 by tracing the decision path.

#### Component: GroupManager
**Method**: `DetermineLineNumber(string filePath, FileType fileType, ApplicationConfiguration config)`

**Enhanced Logic Flow**:
1. **Normal Files**:
   - Log: `[LineCheck] Normal File: {filePath}`
   - Result: Log determined line from `NormalFolderHelper`.

2. **NIR/Camera Files**:
   - **Step 1 (Nir2 Check)**:
     - Log: `[LineCheck] Checking Nir2Path '{config.MatchingSettings.Nir2Path}' against '{filePath}'`
     - Match: Log `[LineMatches] Matched Nir2 -> Line 2`

   - **Step 2 (Cam4-6 Check)**:
     - Loop i=4..6:
       - Log: `[LineCheck] Checking Cam{i}Path '{camPath}'`
       - Match: Log `[LineMatches] Matched Cam{i} -> Line 2`

   - **Step 3 (Default Fallback)**:
     - If reached (Line 1):
       - **Condition**: If file name implies Line 2 (e.g., contains "cam4", "nir2") but failed checks?
       - **Action**: Log **WARNING** via `RaiseLog`:
         `[LineMismatch] WARNING: File '{filePath}' falling back to Line 1. Check configuration paths!`
       - Else: Log `[LineDecision] Defaulting to Line 1`

## 3. Data Integrity & Safety

- Logs are informational and must not throw exceptions.
- Diagnosis logs should be `Debug` level to avoid flooding, except for the explicit `Warning` on suspicious fallbacks.
- UI Log Panel integration uses existing `RaiseLog` / `_uiLog` delegate pattern.

---

## 4. Verification Scenarios

### Scenario A: Line Path Configuration Error
- **Setup**: Misconfigure "Nir2Path" in settings (e.g., add extra space).
- **Action**: Drop a file into Nir2 folder.
- **Expected**: UI Log shows "[LineMismatch] WARNING" and detailed "[LineCheck]" failure logs showing the mismatch.

### Scenario B: Missing Image
- **Setup**: Create Normal folder without `stitched_original.png`.
- **Action**: Wait for detection.
- **Expected**: UI Log (Warning tab) shows "[이미지누락]" message.

### Scenario C: Successful Image
- **Setup**: Normal folder with valid image.
- **Action**: Wait for detection.
- **Expected**: UI Log (Debug tab) shows "[썸네일로드] 성공".
