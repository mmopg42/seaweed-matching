# Tasks: NIR Visualization Implementation

## Phase 1: Dependencies & Core Logic ✅ COMPLETED
- [x] **Add NuGet Packages**
    - [x] `ScottPlot` (5.0.42)
    - [x] `ScottPlot.WPF` (5.0.42)
- [x] **Implement Core Logic**
    - [x] Create `Core/Nir/NirSpectrumParser.cs`
        - [x] Implement `Parse` method (Whitespace handling, `#` header skip)
        - [x] Add validation (Minimum 10 data points)
        - [x] Return `NirSpectrum` model
    - [x] Create `Core/Nir/NirGraphGenerator.cs`
        - [x] Implement `GenerateGraph(NirSpectrum spectrum, int width, int height)`
        - [x] Apply styling (Frameless, Blue line `#0078d4`, Gray bg `#f5f5f5`)
        - [x] Ensure `BitmapSource` is Frozen

## Phase 2: Configuration & Settings ✅ COMPLETED
- [x] **Update Configuration Models**
    - [x] `ApplicationConfiguration.cs`: Add `bool EnableNirGraph` (Default: true)
    - [x] `UISettings` class (in `ApplicationConfiguration.cs`): Add `int NirThumbnailWidth` (Default: 250), `int NirThumbnailHeight` (Default: 100)
- [x] **Update Settings UI**
    - [x] `SettingsDialogViewModel.cs`: Expose `EnableNirGraph`, `NirThumbnailWidth`, `NirThumbnailHeight`
    - [x] `SettingsDialog.xaml`: Add controls
        - [x] CheckBox for "Enable NIR Graph"
        - [x] TextBox for Width & Height (under UI Options)

## Phase 3: UI Integration ✅ COMPLETED
- [x] **Update ViewModels**
    - [x] `FileGroupViewModel.cs`: Add `BitmapSource NirGraphThumbnail` property
    - [x] `FileGroupViewModel.cs`: Update `LoadThumbnailsAsync`
        - [x] Implement file resolution (Try `.spc`->`A.txt` suffix logic, then fallback to `.txt`)
        - [x] Check `EnableNirGraph` setting
        - [x] Call Generator with configured Width/Height
        - [x] Error handling (catch exception -> null)
    - [x] `MainWindowViewModel.cs`: Pass configuration to FileGroupViewModel constructor
- [x] **Update Views**
    - [x] `MainWindow.xaml`: Update NIR column DataTemplate to bind to `NirGraphThumbnail`

## Phase 4: Verification
- [ ] **Manual Verification**
    - [ ] Verify functionality with sample file (`run_120251204T111028A.txt`)
    - [ ] Verify custom size configuration (Change settings -> Restart/Reload -> Check size)
    - [ ] Performance check (Scroll with multiple items)
    - [ ] Verify "Disable NIR Graph" setting
