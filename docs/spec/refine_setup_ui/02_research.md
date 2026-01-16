# Research: Default Configuration Values

## 1. Existing Default Values
Analysis of `ApplicationConfiguration.cs` reveals the following initialized defaults:

- **BasePath**: `"D:/Data"` (Potential conflict with "Empty Path" rule)
- **FolderPaths**: Empty Dictionary
- **Paths (Nir1Path, Camera1Path, etc.)**: `""` (Empty String) - **Good**
- **ExternalProgramSettings**: All paths `""` (Empty String) - **Good**
- **ImageSettings**: 
    - Thumbnail: 200x150, Q85
    - Cache: On, 500MB
- **MatchingSettings**:
    - AbnormalDetection: On, Threshold 0.3
    - LineMode: "integrated"
- **DataSequenceSettings**: `DataSequencePresets.NormalFirst()`
- **WorkflowSettings**:
    - `DeleteQuarantinePath`: `""` (Empty String)
    - **Issue**: `SettingsDialogViewModel` logic automatically fills this if empty:
      ```csharp
      if (string.IsNullOrWhiteSpace(DeleteQuarantinePath))
      {
          var basePath = string.IsNullOrWhiteSpace(_configuration.BasePath) ? "D:/Data" : _configuration.BasePath;
          DeleteQuarantinePath = Path.Combine(basePath, "Trash");
      }
      ```

## 2. Strategy for "Empty Defaults"
To strictly follow the user's rule "**Folder paths and Program paths must be blank**":

1.  **`DefaultConfiguration.GetDefault()`**:
    -   Create `new ApplicationConfiguration()`.
    -   Explicitly overwrite `BasePath` to `string.Empty`.
    -   Explicitly overwrite all other path properties to `string.Empty` (redundant but safe).

2.  **ViewModel Handling**:
    -   The `LoadFromConfiguration` method in `SettingsDialogViewModel` contains logic that populates `DeleteQuarantinePath` with a default if it's empty.
    -   **Solution**: In `ExecuteResetToDefaults`, after loading the configuration, we must **explicitly clear** `DeleteQuarantinePath` (and potentially `OutputPath` if it gets auto-filled) to ensure the UI shows blank fields.

## 3. Conclusion
-   We can use `new ApplicationConfiguration()` as the base.
-   We must override `BasePath` to empty.
-   We must handle the ViewModel's auto-fill logic for `DeleteQuarantinePath` to ensure it respects the "Blank" requirement upon reset.
