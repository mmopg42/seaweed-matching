# Requirements: Fix Missing Normal/NIR Data Display

## 1. Problem Description

Despite recent fixes to `DetermineFileType` and strict path checking, Real-time Monitoring continues to create groups that lack Normal and NIR data.

### Symptoms
- **Log Evidence**:
  ```text
  Debug ... Group VM created group_001 HasNir=False Normal= Main= CamCount=1
  Debug ... Cam1 path missing for group_001
  ```
- **Behavior**:
  - Groups are created sequentially (`group_001`, `group_002`...)
  - `Normal` field is empty.
  - `Main` (MainImage) field is empty.
  - `HasNir` is False.
  - Only `CamCount=1` implies Camera files are triggering the creation.
  - "Cam1 path missing" logs suggest even Camera keys might be mapped incorrectly, or these are Cam2/3 files creating the group without Cam1.

### Root Cause Hypotheses
1. **Event Detection Failure**: Normal/NIR file creation events are not triggering `CreateOrUpdateGroupAsync`.
   - *Reason*: `DetermineFileType` might still be failing strict path matching (e.g., slash direction, missing trailing slash).
2. **Merging Failure**: Normal/NIR files *are* detected, but fail to match the existing Camera-created group.
   - *Reason*: The stable identifier matching logic (`FindMatchingExistingGroup`) might not handle the case where "Group has no Normal/Nir yet" correctly.
3. **Configuration Mismatch**: The runtime `MatchingSettings` might not have the paths loaded as expected (e.g., empty strings).

## 2. Goals

1. **Ensure Normal/NIR Data Visibility**: UI must display Main Image and NIR Graph.
2. **Correct Group Merging**: Camera, Normal, and NIR files for the same timestamp must merge into a single `FileGroup`.
3. **Diagnostic Clarity**: Logs must clearly state *why* a file was classified as Normal/NIR/Camera and *why* it matched (or failed to match) a group.

## 3. Scope & Constraints

- **Scope**: `MonitoringOrchestrator.cs`, `DetermineFileType`, `CreateOrUpdateGroupAsync`, `FindMatchingExistingGroup`.
- **Constraint**: Must verify `ApplicationConfiguration` runtime values.

## 4. Investigation Plan

1. **Verify Runtime Configuration**: Add logs to print specific `Normal1Path`, `Nir1Path` values at startup.
2. **Verify File Detection**: Log *every* file event and its Determined Type before filtering.
3. **Verify Merge Logic**: Trace why a Normal file finding an existing Camera group might fail.

## 5. Acceptance Criteria

- [ ] New groups in log show `Normal=...` and `HasNir=True` (when data exists).
- [ ] UI displays Main Image and NIR Graph.
- [ ] "Cam path missing" errors reduced (correct mapping).
