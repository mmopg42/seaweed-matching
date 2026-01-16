using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using WinForms = System.Windows.Forms;
using ChronoView.Models;
using ChronoView.Core.Configuration;
using Microsoft.Extensions.Logging;
using WpfMessageBox = System.Windows.MessageBox;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for the settings dialog with basic and advanced options.
/// </summary>
public class SettingsDialogViewModel : ViewModelBase
{
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<SettingsDialogViewModel> _logger;
    private ApplicationConfiguration _configuration;
    
    // Basic path settings
    private string _nirPath = string.Empty;
    private string _nir2Path = string.Empty;
    private string _normalPath = string.Empty;
    private string _normal2Path = string.Empty;
    private string _camera1Path = string.Empty;
    private string _camera2Path = string.Empty;
    private string _camera3Path = string.Empty;
    private string _camera4Path = string.Empty;
    private string _camera5Path = string.Empty;
    private string _camera6Path = string.Empty;
    private string _outputPath = string.Empty;
    private string _deleteQuarantinePath = string.Empty;

    // Advanced options - Camera subfolder
    private bool _useCameraSubfolderNormal;
    private bool _useCameraSubfolderNormal2;

    // Advanced options - Image processing
    private bool _useDiskCache = true;
    private int _thumbnailWidth = 200;
    private int _thumbnailHeight = 150;
    private int _thumbnailQuality = 85;

    // Advanced options - Matching (DEPRECATED - Use DataSequenceSettings)
    // All time-based matching is now controlled by DataSequenceSettings in the Sequence tab

    // Advanced options - UI

    private bool _useFolderSuffix;
    private bool _showTooltips = true;
    private int _displayImageWidth = 120;
    private int _displayImageHeight = 90;

    // NIR graph options
    private bool _enableNirGraph = true;
    private int _nirThumbnailWidth = 10;
    private int _nirThumbnailHeight = 150;
    private int _nirDisplayWidth = 120;
    private int _nirDisplayHeight = 90;
    private double _displayFontSize = 10.0;

    // Line mode
    private bool _isSeparatedMode;

    // Log retention
    private int _logRetentionDays = 30;

    // Group management
    private bool _useLineSpecificGroupId = false;

    // Abnormal Detection settings
    private double _abnormalRatioThreshold = 0.3;
    private int _abnormalDetectionWindowSize = 10;

    // Data Sequence settings
    // Data Sequence settings
    private ObservableCollection<DataSequenceItemViewModel> _sequenceItems = new();
    private List<DataSequenceItemViewModel> _originalSequence = new(); // For cancel

    // Tab Selection
    private int _selectedTabIndex = 0;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public SettingsDialogViewModel(
        IConfigurationManager configurationManager,
        ILogger<SettingsDialogViewModel> logger)
    {
        _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Load configuration from manager
        _configuration = _configurationManager.LoadConfiguration<ApplicationConfiguration>();
        
        // Initialize commands
        SaveCommand = new RelayCommand(ExecuteSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
        ApplyCommand = new RelayCommand(ExecuteApply);
        BrowsePathCommand = new RelayCommand<string>(ExecuteBrowsePath);
        OpenFolderCommand = new RelayCommand<string>(ExecuteOpenFolder);
        BrowseExeCommand = new RelayCommand<string>(ExecuteBrowseExe);
        ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);

        // Load configuration
        LoadFromConfiguration();
        
        _logger.LogInformation("SettingsDialogViewModel initialized");
    }

    #region Properties - Basic Paths

    public string NirPath
    {
        get => _nirPath;
        set => SetProperty(ref _nirPath, value);
    }

    public string Nir2Path
    {
        get => _nir2Path;
        set => SetProperty(ref _nir2Path, value);
    }

    public string NormalPath
    {
        get => _normalPath;
        set => SetProperty(ref _normalPath, value);
    }

    public string Normal2Path
    {
        get => _normal2Path;
        set => SetProperty(ref _normal2Path, value);
    }

    public string Camera1Path
    {
        get => _camera1Path;
        set => SetProperty(ref _camera1Path, value);
    }

    public string Camera2Path
    {
        get => _camera2Path;
        set => SetProperty(ref _camera2Path, value);
    }

    public string Camera3Path
    {
        get => _camera3Path;
        set => SetProperty(ref _camera3Path, value);
    }

    public string Camera4Path
    {
        get => _camera4Path;
        set => SetProperty(ref _camera4Path, value);
    }

    public string Camera5Path
    {
        get => _camera5Path;
        set => SetProperty(ref _camera5Path, value);
    }

    public string Camera6Path
    {
        get => _camera6Path;
        set => SetProperty(ref _camera6Path, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string DeleteQuarantinePath
    {
        get => _deleteQuarantinePath;
        set => SetProperty(ref _deleteQuarantinePath, value);
    }

    #endregion

    #region Properties - Advanced Options

    // Camera subfolder options
    public bool UseCameraSubfolderNormal
    {
        get => _useCameraSubfolderNormal;
        set => SetProperty(ref _useCameraSubfolderNormal, value);
    }

    public bool UseCameraSubfolderNormal2
    {
        get => _useCameraSubfolderNormal2;
        set => SetProperty(ref _useCameraSubfolderNormal2, value);
    }

    // Image processing options
    public bool UseDiskCache
    {
        get => _useDiskCache;
        set => SetProperty(ref _useDiskCache, value);
    }

    public int ThumbnailWidth
    {
        get => _thumbnailWidth;
        set => SetProperty(ref _thumbnailWidth, value);
    }

    public int ThumbnailHeight
    {
        get => _thumbnailHeight;
        set => SetProperty(ref _thumbnailHeight, value);
    }

    public int ThumbnailQuality
    {
        get => _thumbnailQuality;
        set => SetProperty(ref _thumbnailQuality, value);
    }

    // Matching options (DEPRECATED - Use DataSequenceSettings)
    // All time-based matching properties removed - use Sequence tab for configuration

    // UI options


    public bool UseFolderSuffix
    {
        get => _useFolderSuffix;
        set => SetProperty(ref _useFolderSuffix, value);
    }

    public bool ShowTooltips
    {
        get => _showTooltips;
        set => SetProperty(ref _showTooltips, value);
    }

    public int DisplayImageWidth
    {
        get => _displayImageWidth;
        set => SetProperty(ref _displayImageWidth, value);
    }

    public int DisplayImageHeight
    {
        get => _displayImageHeight;
        set => SetProperty(ref _displayImageHeight, value);
    }

    // NIR graph options
    public bool EnableNirGraph
    {
        get => _enableNirGraph;
        set => SetProperty(ref _enableNirGraph, value);
    }

    public int NirThumbnailWidth
    {
        get => _nirThumbnailWidth;
        set => SetProperty(ref _nirThumbnailWidth, value);
    }

    public int NirThumbnailHeight
    {
        get => _nirThumbnailHeight;
        set => SetProperty(ref _nirThumbnailHeight, value);
    }

    public int NirDisplayWidth
    {
        get => _nirDisplayWidth;
        set => SetProperty(ref _nirDisplayWidth, value);
    }

    public int NirDisplayHeight
    {
        get => _nirDisplayHeight;
        set => SetProperty(ref _nirDisplayHeight, value);
    }

    public double DisplayFontSize
    {
        get => _displayFontSize;
        set => SetProperty(ref _displayFontSize, value);
    }

    public int LogRetentionDays
    {
        get => _logRetentionDays;
        set => SetProperty(ref _logRetentionDays, value);
    }

    // Line mode
    public bool IsSeparatedMode
    {
        get => _isSeparatedMode;
        set => SetProperty(ref _isSeparatedMode, value);
    }

    // Group management
    public bool UseLineSpecificGroupId
    {
        get => _useLineSpecificGroupId;
        set => SetProperty(ref _useLineSpecificGroupId, value);
    }

    // Abnormal Detection settings
    public double AbnormalRatioThreshold
    {
        get => _abnormalRatioThreshold;
        set => SetProperty(ref _abnormalRatioThreshold, value);
    }

    public int AbnormalDetectionWindowSize
    {
        get => _abnormalDetectionWindowSize;
        set => SetProperty(ref _abnormalDetectionWindowSize, value);
    }

    // Data Sequence settings
    public ObservableCollection<DataSequenceItemViewModel> SequenceItems
    {
        get => _sequenceItems;
        set => SetProperty(ref _sequenceItems, value);
    }

    public bool CompareToReferenceCamera
    {
        get => _configuration.DataSequenceSettings.CompareToReferenceCamera;
        set
        {
            if (_configuration.DataSequenceSettings.CompareToReferenceCamera != value)
            {
                _configuration.DataSequenceSettings.CompareToReferenceCamera = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Properties - External Programs

    private string _generalCameraProgramPath = string.Empty;
    private string _nir1ProgramPath = string.Empty;
    private string _nir2ProgramPath = string.Empty;
    private string _nir2FilterMonitorPath = string.Empty;
    private string _nir2FilterDestinationPath = string.Empty;

    public string GeneralCameraProgramPath
    {
        get => _generalCameraProgramPath;
        set => SetProperty(ref _generalCameraProgramPath, value);
    }

    public string Nir1ProgramPath
    {
        get => _nir1ProgramPath;
        set => SetProperty(ref _nir1ProgramPath, value);
    }

    public string Nir2ProgramPath
    {
        get => _nir2ProgramPath;
        set => SetProperty(ref _nir2ProgramPath, value);
    }

    public string Nir2FilterMonitorPath
    {
        get => _nir2FilterMonitorPath;
        set => SetProperty(ref _nir2FilterMonitorPath, value);
    }

    public string Nir2FilterDestinationPath
    {
        get => _nir2FilterDestinationPath;
        set => SetProperty(ref _nir2FilterDestinationPath, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand BrowsePathCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand BrowseExeCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// Event raised when settings are applied without closing.
    /// </summary>
    public event EventHandler? SettingsApplied;

    #endregion

    #region Command Implementations

    private void ExecuteSave()
    {
        SaveToConfiguration();
        CloseRequested?.Invoke(this, true);
    }

    private void ExecuteCancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    private void ExecuteApply()
    {
        SaveToConfiguration();
        SettingsApplied?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteBrowsePath(string? pathType)
    {
        if (string.IsNullOrWhiteSpace(pathType))
            return;

        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "Select folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK)
            return;

        var selected = dialog.SelectedPath;

        switch (pathType.ToLowerInvariant())
        {
            case "nir":
            case "nir1":
                NirPath = selected;
                break;
            case "nir2":
                Nir2Path = selected;
                break;
            case "normal":
            case "normal1":
                NormalPath = selected;
                break;
            case "normal2":
                Normal2Path = selected;
                break;
            case "cam1":
                Camera1Path = selected;
                break;
            case "cam2":
                Camera2Path = selected;
                break;
            case "cam3":
                Camera3Path = selected;
                break;
            case "cam4":
                Camera4Path = selected;
                break;
            case "cam5":
                Camera5Path = selected;
                break;
            case "cam6":
                Camera6Path = selected;
                break;
            case "output":
                OutputPath = selected;
                break;
            case "quarantine":
            case "deletequarantine":
            case "deletequarantinepath":
                DeleteQuarantinePath = selected;
                break;
            case "nir2filtermonitor":
                Nir2FilterMonitorPath = selected;
                break;
            case "nir2filterdest":
            case "nir2filterdestination":
                Nir2FilterDestinationPath = selected;
                break;
            default:
                // Unknown path type; ignore
                break;
        }
    }

    private void ExecuteOpenFolder(string? pathType)
    {
        if (string.IsNullOrWhiteSpace(pathType))
            return;

        string? folderPath = pathType.ToLowerInvariant() switch
        {
            "nir" or "nir1" => NirPath,
            "nir2" => Nir2Path,
            "normal" or "normal1" => NormalPath,
            "normal2" => Normal2Path,
            "cam1" => Camera1Path,
            "cam2" => Camera2Path,
            "cam3" => Camera3Path,
            "cam4" => Camera4Path,
            "cam5" => Camera5Path,
            "cam6" => Camera6Path,
            "output" => OutputPath,
            "quarantine" or "deletequarantine" or "deletequarantinepath" => DeleteQuarantinePath,
            "nir2filtermonitor" => Nir2FilterMonitorPath,
            "nir2filterdest" or "nir2filterdestination" => Nir2FilterDestinationPath,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            WpfMessageBox.Show("Path is not set.", "Cannot Open Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            WpfMessageBox.Show($"Folder does not exist:\n{folderPath}", "Cannot Open Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder: {Path}", folderPath);
            WpfMessageBox.Show($"Failed to open folder:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteBrowseExe(string? programType)
    {
        if (string.IsNullOrWhiteSpace(programType))
            return;

        using var dialog = new WinForms.OpenFileDialog
        {
            Title = "Select Program",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK)
            return;

        var selected = dialog.FileName;

        switch (programType.ToLowerInvariant())
        {
            case "generalcamera":
                GeneralCameraProgramPath = selected;
                break;
            case "nir1program":
                Nir1ProgramPath = selected;
                break;
            case "nir2program":
                Nir2ProgramPath = selected;
                break;
            default:
                // Unknown program type; ignore
                break;
        }
    }

    private void ExecuteResetToDefaults()
    {
        // 1. Save current path values before reset
        var savedPaths = new
        {
            NirPath = this.NirPath,
            Nir2Path = this.Nir2Path,
            NormalPath = this.NormalPath,
            Normal2Path = this.Normal2Path,
            Camera1Path = this.Camera1Path,
            Camera2Path = this.Camera2Path,
            Camera3Path = this.Camera3Path,
            Camera4Path = this.Camera4Path,
            Camera5Path = this.Camera5Path,
            Camera6Path = this.Camera6Path,
            OutputPath = this.OutputPath,
            DeleteQuarantinePath = this.DeleteQuarantinePath,
            GeneralCameraProgramPath = this.GeneralCameraProgramPath,
            Nir1ProgramPath = this.Nir1ProgramPath,
            Nir2ProgramPath = this.Nir2ProgramPath,
            Nir2FilterMonitorPath = this.Nir2FilterMonitorPath,
            Nir2FilterDestinationPath = this.Nir2FilterDestinationPath
        };
        
        // 2. Get default configuration and replace local instance
        var defaultConfig = DefaultConfiguration.GetDefault();
        _configuration = defaultConfig;
        
        // 3. Reload ViewModel properties from new configuration (resets all values)
        LoadFromConfiguration();
        
        // 4. Restore saved paths
        NirPath = savedPaths.NirPath;
        Nir2Path = savedPaths.Nir2Path;
        NormalPath = savedPaths.NormalPath;
        Normal2Path = savedPaths.Normal2Path;
        Camera1Path = savedPaths.Camera1Path;
        Camera2Path = savedPaths.Camera2Path;
        Camera3Path = savedPaths.Camera3Path;
        Camera4Path = savedPaths.Camera4Path;
        Camera5Path = savedPaths.Camera5Path;
        Camera6Path = savedPaths.Camera6Path;
        OutputPath = savedPaths.OutputPath;
        DeleteQuarantinePath = savedPaths.DeleteQuarantinePath;
        GeneralCameraProgramPath = savedPaths.GeneralCameraProgramPath;
        Nir1ProgramPath = savedPaths.Nir1ProgramPath;
        Nir2ProgramPath = savedPaths.Nir2ProgramPath;
        Nir2FilterMonitorPath = savedPaths.Nir2FilterMonitorPath;
        Nir2FilterDestinationPath = savedPaths.Nir2FilterDestinationPath;
        
        _logger.LogInformation("Configuration reset to defaults (paths preserved).");
        WpfMessageBox.Show("Settings have been reset to default values.\nPath settings have been preserved.", 
                           "Defaults Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// Loads settings from the configuration object.
    /// </summary>
    private void LoadFromConfiguration()
    {
        // Load paths from MatchingSettings (primary source)
        NirPath = _configuration.MatchingSettings.Nir1Path;
        Nir2Path = _configuration.MatchingSettings.Nir2Path;
        NormalPath = _configuration.MatchingSettings.Normal1Path;
        Normal2Path = _configuration.MatchingSettings.Normal2Path;

        Camera1Path = _configuration.MatchingSettings.Camera1Path;
        Camera2Path = _configuration.MatchingSettings.Camera2Path;
        Camera3Path = _configuration.MatchingSettings.Camera3Path;
        Camera4Path = _configuration.MatchingSettings.Camera4Path;
        Camera5Path = _configuration.MatchingSettings.Camera5Path;
        Camera6Path = _configuration.MatchingSettings.Camera6Path;

        // Load output path
        OutputPath = _configuration.MatchingSettings.OutputPath ?? string.Empty;

        // Quarantine path (soft delete)
        DeleteQuarantinePath = _configuration.WorkflowSettings.DeleteQuarantinePath;

        // Load advanced options
        UseDiskCache = _configuration.ImageSettings.EnableCaching;
        ThumbnailWidth = _configuration.ImageSettings.ThumbnailWidth;
        ThumbnailHeight = _configuration.ImageSettings.ThumbnailHeight;
        ThumbnailQuality = _configuration.ImageSettings.ThumbnailQuality;

        // Matching options (DEPRECATED - removed, use DataSequenceSettings)

        // Load camera subfolder options
        UseCameraSubfolderNormal = _configuration.MatchingSettings.UseCameraSubfolderNormal;
        UseCameraSubfolderNormal2 = _configuration.MatchingSettings.UseCameraSubfolderNormal2;
        UseFolderSuffix = _configuration.MatchingSettings.UseFolderSuffix;

        IsSeparatedMode = _configuration.MatchingSettings.LineMode == "separated";

        // Load UI settings
        DisplayImageWidth = _configuration.UISettings.DisplayImageWidth;
        DisplayImageWidth = _configuration.UISettings.DisplayImageWidth;
        DisplayImageHeight = _configuration.UISettings.DisplayImageHeight;

        ShowTooltips = _configuration.UISettings.ShowTooltips;

        // Load NIR graph settings
        EnableNirGraph = _configuration.MatchingSettings.EnableNirGraph;
        NirThumbnailWidth = _configuration.UISettings.NirThumbnailWidth;
        NirThumbnailHeight = _configuration.UISettings.NirThumbnailHeight;
        NirDisplayWidth = _configuration.UISettings.NirDisplayWidth;
        NirDisplayHeight = _configuration.UISettings.NirDisplayHeight;
        DisplayFontSize = _configuration.UISettings.DisplayFontSize;

        // Load Data Sequence settings
        LoadSequenceSettings();

        // Load External Programs settings
        GeneralCameraProgramPath = _configuration.ExternalProgramSettings.GeneralCameraProgramPath;
        Nir1ProgramPath = _configuration.ExternalProgramSettings.Nir1ProgramPath;
        Nir2ProgramPath = _configuration.ExternalProgramSettings.Nir2ProgramPath;
        Nir2FilterMonitorPath = _configuration.ExternalProgramSettings.Nir2FilterMonitorPath;
        Nir2FilterDestinationPath = _configuration.ExternalProgramSettings.Nir2FilterDestinationPath;

        // Auto-update date in monitor path if pattern exists
        Nir2FilterMonitorPath = UpdatePathWithCurrentDate(Nir2FilterMonitorPath);

        // Load Log retention settings
        LogRetentionDays = _configuration.WorkflowSettings.LogRetentionDays;

        // Load Group management settings
        UseLineSpecificGroupId = _configuration.WorkflowSettings.UseLineSpecificGroupId;

        // Load Abnormal Detection settings
        AbnormalRatioThreshold = _configuration.MatchingSettings.AbnormalRatioThreshold;
        AbnormalDetectionWindowSize = _configuration.MatchingSettings.AbnormalDetectionWindowSize;
    }

    /// <summary>
    /// Saves settings to the configuration object.
    /// </summary>
    private void SaveToConfiguration()
    {
        // Save paths to FolderPaths dictionary (for backward compatibility)
        _configuration.FolderPaths["nir"] = NirPath;
        _configuration.FolderPaths["nir2"] = Nir2Path;
        _configuration.FolderPaths["normal"] = NormalPath;
        _configuration.FolderPaths["normal2"] = Normal2Path;

        // Save Line 1 paths to MatchingSettings
        _configuration.MatchingSettings.Nir1Path = NirPath;
        _configuration.MatchingSettings.Normal1Path = NormalPath;

        // Save Line 2 paths to MatchingSettings
        _configuration.MatchingSettings.Nir2Path = Nir2Path;
        _configuration.MatchingSettings.Normal2Path = Normal2Path;

        // Save output path
        _configuration.MatchingSettings.OutputPath = OutputPath;

        _configuration.MatchingSettings.Camera1Path = Camera1Path;
        _configuration.MatchingSettings.Camera2Path = Camera2Path;
        _configuration.MatchingSettings.Camera3Path = Camera3Path;
        _configuration.MatchingSettings.Camera4Path = Camera4Path;
        _configuration.MatchingSettings.Camera5Path = Camera5Path;
        _configuration.MatchingSettings.Camera6Path = Camera6Path;

        // Save quarantine path (soft delete)
        _configuration.WorkflowSettings.DeleteQuarantinePath = DeleteQuarantinePath;

        // Save advanced options
        _configuration.ImageSettings.EnableCaching = UseDiskCache;
        _configuration.ImageSettings.ThumbnailWidth = ThumbnailWidth;
        _configuration.ImageSettings.ThumbnailHeight = ThumbnailHeight;
        _configuration.ImageSettings.ThumbnailQuality = ThumbnailQuality;

        // Matching options (DEPRECATED - removed, use DataSequenceSettings)

        // Save camera subfolder options
        _configuration.MatchingSettings.UseCameraSubfolderNormal = UseCameraSubfolderNormal;
        _configuration.MatchingSettings.UseCameraSubfolderNormal2 = UseCameraSubfolderNormal2;
        _configuration.MatchingSettings.UseFolderSuffix = UseFolderSuffix;

        _configuration.MatchingSettings.LineMode = IsSeparatedMode ? "separated" : "integrated";

        // Save UI settings
        _configuration.UISettings.DisplayImageWidth = DisplayImageWidth;
        _configuration.UISettings.DisplayImageHeight = DisplayImageHeight;

        _configuration.UISettings.ShowTooltips = ShowTooltips;

        // Save NIR graph settings
        _configuration.MatchingSettings.EnableNirGraph = EnableNirGraph;
        _configuration.UISettings.NirThumbnailWidth = NirThumbnailWidth;
        _configuration.UISettings.NirThumbnailHeight = NirThumbnailHeight;
        _configuration.UISettings.NirDisplayWidth = NirDisplayWidth;
        _configuration.UISettings.NirDisplayHeight = NirDisplayHeight;
        _configuration.UISettings.DisplayFontSize = DisplayFontSize;

        // Save Data Sequence settings
        SaveSequenceSettings();

        // Save External Programs settings
        _configuration.ExternalProgramSettings.GeneralCameraProgramPath = GeneralCameraProgramPath;
        _configuration.ExternalProgramSettings.Nir1ProgramPath = Nir1ProgramPath;
        _configuration.ExternalProgramSettings.Nir2ProgramPath = Nir2ProgramPath;
        _configuration.ExternalProgramSettings.Nir2FilterMonitorPath = Nir2FilterMonitorPath;
        _configuration.ExternalProgramSettings.Nir2FilterDestinationPath = Nir2FilterDestinationPath;

        // Save Log retention settings
        _configuration.WorkflowSettings.LogRetentionDays = LogRetentionDays;

        // Save Group management settings
        _configuration.WorkflowSettings.UseLineSpecificGroupId = UseLineSpecificGroupId;

        // Save Abnormal Detection settings
        _configuration.MatchingSettings.AbnormalRatioThreshold = AbnormalRatioThreshold;
        _configuration.MatchingSettings.AbnormalDetectionWindowSize = AbnormalDetectionWindowSize;

        // Persist to disk
        try 
        {
            _configurationManager.SaveConfiguration(_configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to disk");
            WpfMessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Gets the current configuration with all changes applied.
    /// </summary>
    public ApplicationConfiguration GetConfiguration()
    {
        return _configuration;
    }

    #endregion

    #region Data Sequence Management

    /// <summary>
    /// Load data sequence settings from configuration
    /// Filters out Cam4-6 (Line 2) as they are auto-mapped from Cam1-3
    /// </summary>
    private void LoadSequenceSettings()
    {
        SequenceItems.Clear();

        var settings = _configuration.DataSequenceSettings;
        foreach (var item in settings.Sequence.OrderBy(x => x.Order))
        {
            // FILTER: Hide Cam4-6 from UI (they are Line 2 internal types)
            if (item.Type == DataType.Cam4 || item.Type == DataType.Cam5 || item.Type == DataType.Cam6)
            {
                _logger.LogTrace("Skipping {Type} from UI (Line 2 internal type)", item.Type);
                continue;
            }

            var viewModel = new DataSequenceItemViewModel
            {
                Type = item.Type,
                Order = item.Order,
                MinDelay = item.MinDelaySeconds,
                MaxDelay = item.MaxDelaySeconds,
                Enabled = item.Enabled
            };
            SequenceItems.Add(viewModel);
        }

        // Store original for cancel
        _originalSequence = SequenceItems.Select(item => new DataSequenceItemViewModel
        {
            Type = item.Type,
            Order = item.Order,
            MinDelay = item.MinDelay,
            MaxDelay = item.MaxDelay,
            Enabled = item.Enabled
        }).ToList();
    }

    /// <summary>
    /// Save data sequence settings to configuration
    /// </summary>
    private void SaveSequenceSettings()
    {
        // Build new sequence list from ViewModel
        var newSequence = SequenceItems.Select(vm => new DataSequenceItem
        {
            Type = vm.Type,
            Order = vm.Order,
            MinDelaySeconds = vm.MinDelay,
            MaxDelaySeconds = vm.MaxDelay,
            Enabled = vm.Enabled
        }).ToList();

        // Update existing object (preserves other properties like CompareToReferenceCamera)
        _configuration.DataSequenceSettings.Sequence = newSequence;

        // Validate
        if (!_configuration.DataSequenceSettings.Validate(out var errors))
        {
            var errorMessage = string.Join("\n", errors);
            _logger.LogWarning("Data sequence validation failed: {Errors}", errorMessage);
            WpfMessageBox.Show($"Validation failed:\n{errorMessage}", "Invalid Sequence", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.LogInformation("Data sequence settings saved");
    }

    /// <summary>
    /// Updates the date portion of the path to current date if it matches the pattern YYYY/MM/DD using Regex.
    /// </summary>
    private string UpdatePathWithCurrentDate(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        try
        {
            // Pattern: YYYY/MM/DD or YYYY\MM\DD
            var regex = new System.Text.RegularExpressions.Regex(@"\d{4}[\\/]\d{2}[\\/]\d{2}");
            var match = regex.Match(path);

            if (match.Success)
            {
                // Use double backslash in verbatim string - it becomes a single literal backslash in format output
                var currentDate = DateTime.Now.ToString(@"yyyy\\MM\\dd"); 
                return regex.Replace(path, currentDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-updating date in path: {Path}", path);
        }

        return path;
    }


    /// <summary>
    /// Apply a preset configuration
    /// </summary>


    /// <summary>
    /// Check if there are unsaved changes in sequence
    /// </summary>
    private bool HasUnsavedSequenceChanges()
    {
        if (SequenceItems.Count != _originalSequence.Count)
            return true;

        for (int i = 0; i < SequenceItems.Count; i++)
        {
            var current = SequenceItems[i];
            var original = _originalSequence[i];

            if (current.Type != original.Type ||
                current.Order != original.Order ||
                current.MinDelay != original.MinDelay ||
                current.MaxDelay != original.MaxDelay ||
                current.Enabled != original.Enabled)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Move item in sequence after drag-drop (called from code-behind)
    /// </summary>
    public void ReorderSequenceItem(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= SequenceItems.Count || 
            newIndex < 0 || newIndex >= SequenceItems.Count ||
            oldIndex == newIndex)
            return;

        // Move item in collection
        var item = SequenceItems[oldIndex];
        SequenceItems.RemoveAt(oldIndex);
        SequenceItems.Insert(newIndex, item);

        // Reassign Order values to match new positions
        for (int i = 0; i < SequenceItems.Count; i++)
        {
            SequenceItems[i].Order = i + 1;  // 1-based ordering
        }

        _logger.LogDebug("Reordered: {Type} from index {OldIndex} to {NewIndex}", 
            item.Type, oldIndex, newIndex);
    }

    #endregion
}
