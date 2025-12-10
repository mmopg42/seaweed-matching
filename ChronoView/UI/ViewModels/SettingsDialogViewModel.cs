using System.Windows;
using System.Windows.Input;
using ChronoView.Models;
using ChronoView.Core.Configuration;
using Microsoft.Extensions.Logging;

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

    // Advanced options - Camera subfolder
    private bool _useCameraSubfolderNormal;
    private bool _useCameraSubfolderNormal2;

    // Advanced options - Image processing
    private bool _useDiskCache = true;
    private int _thumbnailWidth = 200;
    private int _thumbnailHeight = 150;
    private int _thumbnailQuality = 85;

    // Advanced options - Matching
    private bool _useCamTimeMatching = true;
    private double _camMatchMinDiff = 4.0;
    private double _camMatchMaxDiff = 6.0;
    private double _nirMatchTimeDiff = 1.0;
    private int _nirTimeWindowSeconds = 300;
    private int _cameraTimeWindowSeconds = 60;

    // Advanced options - UI
    private bool _legacyUiMode;
    private bool _useFolderSuffix;
    private bool _showTooltips = true;
    private int _displayImageWidth = 120;
    private int _displayImageHeight = 90;
    private int _dataGridRowHeight = 100;

    // Line mode
    private bool _isSeparatedMode;

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
        BrowsePathCommand = new RelayCommand<string>(ExecuteBrowsePath);

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

    // Matching options
    public bool UseCamTimeMatching
    {
        get => _useCamTimeMatching;
        set => SetProperty(ref _useCamTimeMatching, value);
    }

    public double CamMatchMinDiff
    {
        get => _camMatchMinDiff;
        set => SetProperty(ref _camMatchMinDiff, value);
    }

    public double CamMatchMaxDiff
    {
        get => _camMatchMaxDiff;
        set => SetProperty(ref _camMatchMaxDiff, value);
    }

    public double NirMatchTimeDiff
    {
        get => _nirMatchTimeDiff;
        set => SetProperty(ref _nirMatchTimeDiff, value);
    }

    public int NirTimeWindowSeconds
    {
        get => _nirTimeWindowSeconds;
        set => SetProperty(ref _nirTimeWindowSeconds, value);
    }

    public int CameraTimeWindowSeconds
    {
        get => _cameraTimeWindowSeconds;
        set => SetProperty(ref _cameraTimeWindowSeconds, value);
    }

    // UI options
    public bool LegacyUiMode
    {
        get => _legacyUiMode;
        set => SetProperty(ref _legacyUiMode, value);
    }

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

    public int DataGridRowHeight
    {
        get => _dataGridRowHeight;
        set => SetProperty(ref _dataGridRowHeight, value);
    }

    // Line mode
    public bool IsSeparatedMode
    {
        get => _isSeparatedMode;
        set => SetProperty(ref _isSeparatedMode, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowsePathCommand { get; }

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

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

    private void ExecuteBrowsePath(string? pathType)
    {
        // TODO: Implement folder browser dialog
        // This will be implemented when the UI is created
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// Loads settings from the configuration object.
    /// </summary>
    private void LoadFromConfiguration()
    {
        // Load paths
        _configuration.FolderPaths.TryGetValue("nir", out var nirPath);
        _configuration.FolderPaths.TryGetValue("nir2", out var nir2Path);
        _configuration.FolderPaths.TryGetValue("normal", out var normalPath);
        _configuration.FolderPaths.TryGetValue("normal2", out var normal2Path);

        NirPath = nirPath ?? string.Empty;
        Nir2Path = nir2Path ?? string.Empty;
        NormalPath = normalPath ?? string.Empty;
        Normal2Path = normal2Path ?? string.Empty;

        Camera1Path = _configuration.MatchingSettings.Camera1Path;
        Camera2Path = _configuration.MatchingSettings.Camera2Path;
        Camera3Path = _configuration.MatchingSettings.Camera3Path;
        Camera4Path = _configuration.MatchingSettings.Camera4Path;
        Camera5Path = _configuration.MatchingSettings.Camera5Path;
        Camera6Path = _configuration.MatchingSettings.Camera6Path;

        // Load advanced options
        UseDiskCache = _configuration.ImageSettings.EnableCaching;
        ThumbnailWidth = _configuration.ImageSettings.ThumbnailWidth;
        ThumbnailHeight = _configuration.ImageSettings.ThumbnailHeight;
        ThumbnailQuality = _configuration.ImageSettings.ThumbnailQuality;

        NirTimeWindowSeconds = _configuration.MatchingSettings.NirTimeWindowSeconds;
        CameraTimeWindowSeconds = _configuration.MatchingSettings.CameraTimeWindowSeconds;

        IsSeparatedMode = _configuration.MatchingSettings.LineMode == "separated";

        // Load UI settings
        DisplayImageWidth = _configuration.UISettings.DisplayImageWidth;
        DisplayImageHeight = _configuration.UISettings.DisplayImageHeight;
        DataGridRowHeight = _configuration.UISettings.DataGridRowHeight;
        LegacyUiMode = _configuration.UISettings.LegacyUiMode;
        ShowTooltips = _configuration.UISettings.ShowTooltips;
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

        // ALSO save to MatchingSettings (this is what MonitoringOrchestrator reads)
        _configuration.MatchingSettings.NirPath = NirPath;
        _configuration.MatchingSettings.NormalPath = NormalPath;

        _configuration.MatchingSettings.Camera1Path = Camera1Path;
        _configuration.MatchingSettings.Camera2Path = Camera2Path;
        _configuration.MatchingSettings.Camera3Path = Camera3Path;
        _configuration.MatchingSettings.Camera4Path = Camera4Path;
        _configuration.MatchingSettings.Camera5Path = Camera5Path;
        _configuration.MatchingSettings.Camera6Path = Camera6Path;

        // Save advanced options
        _configuration.ImageSettings.EnableCaching = UseDiskCache;
        _configuration.ImageSettings.ThumbnailWidth = ThumbnailWidth;
        _configuration.ImageSettings.ThumbnailHeight = ThumbnailHeight;
        _configuration.ImageSettings.ThumbnailQuality = ThumbnailQuality;

        _configuration.MatchingSettings.NirTimeWindowSeconds = NirTimeWindowSeconds;
        _configuration.MatchingSettings.CameraTimeWindowSeconds = CameraTimeWindowSeconds;

        _configuration.MatchingSettings.LineMode = IsSeparatedMode ? "separated" : "integrated";

        // Save UI settings
        _configuration.UISettings.DisplayImageWidth = DisplayImageWidth;
        _configuration.UISettings.DisplayImageHeight = DisplayImageHeight;
        _configuration.UISettings.DataGridRowHeight = DataGridRowHeight;
        _configuration.UISettings.LegacyUiMode = LegacyUiMode;
        _configuration.UISettings.ShowTooltips = ShowTooltips;

        // Persist to disk
        try 
        {
            _configurationManager.SaveConfiguration(_configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to disk");
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
}
