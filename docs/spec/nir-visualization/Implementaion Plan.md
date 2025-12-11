Implementation Plan: NIR Visualization
Goal Description
Enable visualization of NIR spectrum data in the ChronoView application. The system will parse 

.txt
 files containing waveform data (paired with .spc files) and generate a line graph to be displayed as a thumbnail in the DataGrid. A cached, asynchronous approach will be used to ensure high performance and responsiveness.

User Review Required
IMPORTANT

Dependency Addition: This plan adds the ScottPlot.WPF (or ScottPlot) NuGet package to the project. This is a standard scientific plotting library but increases build size slightly.

Fast Mode: The graph will be generated as a static image (Bitmap) for the DataGrid to maintain performance, rather than a live interactive control for every row.

Proposed Changes
Dependencies
[MODIFY]

ChronoView.csproj
Add both ScottPlot packages (verified necessary):
```xml
<PackageReference Include="ScottPlot" Version="5.0.42" />
<PackageReference Include="ScottPlot.WPF" Version="5.0.42" />
```
**Note**: Both packages are required. Core provides plotting, WPF provides `BitmapSource` integration.
Core Logic
[NEW]

ChronoView/Core/Nir/NirSpectrumParser.cs
Implement `Parse(string filePath)`:
*   **Verified Format**: Space-separated values, skip lines starting with `#`
*   **Parsing Logic**:
    ```csharp
    var parts = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length >= 2 && double.TryParse(parts[0], out wavelength) && double.TryParse(parts[1], out intensity))
    {
        wavelengths.Add(wavelength);
        intensities.Add(intensity);
    }
    ```
*   **Validation**: Return empty if < 10 data points
*   Returns `NirSpectrum` model with `Wavelengths` (List<double>) and `Intensities` (List<double>)

[NEW]

ChronoView/Core/Nir/NirGraphGenerator.cs
Implement `GenerateGraph(NirSpectrum spectrum, int width, int height)`:
*   Use `width` and `height` parameters for generation.
*   **ScottPlot 5.0 API**:
    ```csharp
    var plot = new Plot();
    var signal = plot.Add.Signal(spectrum.Intensities.ToArray());
    signal.Color = ScottPlot.Color.FromHex("#0078d4");  // NOT FromARGB!
    signal.LineWidth = 1;

    // Map wavelength range to X-axis
    signal.Data.XOffset = spectrum.Wavelengths.Min();
    signal.Data.Period = (spectrum.Wavelengths.Max() - spectrum.Wavelengths.Min()) / spectrum.Wavelengths.Count;

    // Minimalist styling
    plot.Axes.Frameless();
    plot.HideGrid();
    plot.Layout.Frameless();
    plot.FigureBackground.Color = ScottPlot.Color.FromHex("#f5f5f5");
    plot.DataBackground.Color = ScottPlot.Color.FromHex("#f5f5f5");

    // Generate BitmapSource
    var image = plot.GetImage(width, height);
    image.Freeze();  // Required for cross-thread usage!
    return image;
    ```
*   **Configurable Size**: Use width/height passed from argument (sourced from Settings)
*   Returns `BitmapSource` (frozen)
UI & ViewModel
[MODIFY]

ChronoView/UI/ViewModels/FileGroupViewModel.cs
Add `NirGraphThumbnail` property (BitmapSource).
Update `LoadThumbnailsAsync()`:
*   **File Resolution Priority**:
    1. Try `.spc` filename with `A` suffix: `run_120251204T111028.spc` → `run_120251204T111028A.txt`
    2. Fallback: exact match without suffix: `run_120251204T111028.txt`
*   Check `Configuration.EnableNirGraph` before processing
*   Use `Configuration.UISettings.NirThumbnailWidth` and `NirThumbnailHeight` for generation size.
*   Execute parsing/generation on `Task.Run()` (background thread)
*   **CRITICAL**: Call `.Freeze()` on BitmapSource before assigning to property
*   Handle exceptions gracefully (log warning, set null/placeholder image)
[MODIFY] 

ChronoView/Models/ApplicationConfiguration.cs
Add bool EnableNirGraph { get; set; } = true;
In `UISettings` class:
*   Add `int NirThumbnailWidth { get; set; } = 250;`
*   Add `int NirThumbnailHeight { get; set; } = 100;`
[MODIFY] 

ChronoView/UI/ViewModels/SettingsDialogViewModel.cs
Expose the new setting.
[MODIFY] 

ChronoView/UI/Views/SettingsDialog.xaml
Add ToggleSwitch/CheckBox for "Enable NIR Graph".
Add NumberBox/TextBox for "NIR Width" and "NIR Height" in UI Options.
[MODIFY] 

ChronoView/MainWindow.xaml
Bind the NIR column Image Source to NirGraphThumbnail.
Verification Plan
Test Data Location
**Sample File**: `C:\workspace\seaweed\data\20251204\2021_A014\with NIR\Nir\run_120251204T111028A.txt`
*   1951 data points
*   Wavelength range: 4000-9994 cm⁻¹
*   Headers start with `#` (Chinese characters)

**Test Program**: `C:\workspace\seaweed\gui_kiro\task_helper\nir_graph_test\`
*   Standalone C# console app for ScottPlot verification
*   Generates test graphs before integration
*   Output: `nir_thumbnail_200x150.png`, `nir_detailed_800x600.png`, `nir_wpf_bitmap_200x150.png`

Automated Tests
Parser Test:
*   Verify header skip (lines starting with `#`)
*   Verify space-separated parsing
*   Verify minimum 10 data points validation
*   Test with negative intensity values

Generator Test:
*   Verify ScottPlot 5.0 Color API (`FromHex` instead of `FromARGB`)
*   Verify BitmapSource generation and `.Freeze()` call
*   Verify 200x150 output size

Manual Verification
Visual Check: Run the app with sample NIR data from `2021_A014`. Verify graphs appear in NIR column.
Performance: Scroll DataGrid with 100+ file groups. Verify no UI stutter (maintain 60 FPS).
Toggle Check: Settings → UI Options → Disable NIR Graph → Save. Verify graphs stop loading.
Error Handling:
*   Corrupt a `.txt` file (invalid numbers) → verify graceful error (no crash)
*   Missing `.txt` file → verify placeholder/null handling