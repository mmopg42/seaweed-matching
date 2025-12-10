# Image Column Mapping Troubleshooting

## Problem Description
Users report that images are appearing in the incorrect columns within the DataGrid. For example:
- Standard Camera (Main Image) appearing in a Camera column.
- Cam 1 images appearing in Cam 2 column.
- Images being generally mixed up.

## System Logic Analysis
The application uses the following logic to map images to columns:

1.  **Configuration**: Paths for each camera (Cam 1 - Cam 6) and Normal folders (Line 1/2) are defined in `Settings`.
2.  **Scanning**: `MonitoringOrchestrator` scans these specific user-defined paths.
    - Files found in `Camera1Path` are tagged as "cam1".
    - Files found in `Camera2Path` are tagged as "cam2".
    - Files found in `Normal1Path` are tagged as "normal1".
3.  **Matching**: `FileGroupMatcherService` matches these files into groups.
    - `cam1` files trigger the assignment to the Group's `CameraFiles["cam1"]`.
    - `cam2` files trigger the assignment to the Group's `CameraFiles["cam2"]`.
    - `Main Image` is *always* derived from the "Normal" folder (specifically `stitched_original.png`), or explicitly set if logic allows.
4.  **Display**: The UI (`MainWindow.xaml`) binds columns individually:
    - "Main Img" column binds to `MainImageThumbnail` (derived from `MainImagePath`).
    - "Cam 1" column binds to `Camera1Thumbnail` (derived from `cam1` path).
    - "Cam 2" column binds to `Camera2Thumbnail` (derived from `cam2` path).

## Troubleshooting Steps

### 1. Verify Path Configuration
The most likely cause of mixed-up images is incorrect path configuration in the Settings dialog.

*   **Action**: Open **Tools > Settings**.
*   **Check**: Verify that the path entered for "Camera 1" actually points to the folder containing Camera 1 images.
    *   *Common Error*: Pointing "Camera 1" path to the Camera 2 folder.
    *   *Common Error*: Swapping Normal and Camera paths.
*   **Verification**: Browse to the path in Windows Explorer and check a few image files.

### 2. Verify File Content
Ensure that the files inside the folders are what they claim to be.
*   **Action**: Open the "Normal" folder configured in Settings.
*   **Check**: Does it contain `stitched_original.png`?
    *   If `stitched_original.png` is missing, the "Main Img" column will be empty or show a placeholder.
    *   The "Main Img" column *never* displays raw camera images (e.g., .jpg) unless they are renamed to `stitched_original.png` or the code logic is fundamentally altered.

### 3. Check Logs for Scan Results
The application logs the number of files scanned for each category.
*   **Action**: Check the "Message Log" panel at the bottom of the main window.
*   **Look for**:
    *   `Scanned X files from Camera 1`
    *   `Scanned Y files from Camera 2`
*   **Analysis**: If Camera 1 has 0 files but you expect 100, the path is likely wrong.

### 4. Timestamp Correlation (Advanced)
If configuration is correct but images are still "wrong" (e.g., Cam 1 image for Time A is shown next to Cam 2 image for Time B):
*   **Cause**: The matching logic relies on timestamps. If Cam 1 and Cam 2 clocks are not synchronized, they might not match into the same group.
*   **Check**: Look at the "Index" or "Created At" time of the group. Open the specific image files and check their "Date Modified" or file name timestamps.
*   **Fix**: Ensure camera clocks are synchronized or adjust `CamMatchMinDiff`/`CamMatchMaxDiff` in configuration (if exposed) or code constant.

## Conclusion
The application code enforces strict separation of columns based on the internal keys ("cam1", "cam2", etc.). These keys are assigned strictly based on which *folder* the file was found in. Therefore, if an image appears in the "Cam 1" column, it **must** have come from the folder configured as "Camera 1 Path".
