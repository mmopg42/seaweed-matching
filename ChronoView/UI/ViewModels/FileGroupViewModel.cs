using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using System.Windows.Media.Imaging;
using System.Windows;

namespace ChronoView.UI.ViewModels;

/// <summary>
/// ViewModel for displaying individual file groups in the UI.
/// </summary>
public class FileGroupViewModel : ViewModelBase, IDisposable
{
    private readonly FileGroup _fileGroup;
    private readonly IImageProcessor _imageProcessor;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isSelected;
    private string? _mainImagePath;
    private string? _nirImagePath;
    private Dictionary<string, string?> _cameraImagePaths = new();
    private BitmapSource? _mainImageThumbnail;
    private BitmapSource? _nirImageThumbnail;
    private BitmapSource? _camera1Thumbnail;
    private BitmapSource? _camera2Thumbnail;
    private BitmapSource? _camera3Thumbnail;
    private BitmapSource? _camera4Thumbnail;
    private BitmapSource? _camera5Thumbnail;
    private BitmapSource? _camera6Thumbnail;
    private bool _disposed;

    /// <summary>
    /// Creates a new FileGroupViewModel wrapping a FileGroup model.
    /// </summary>
    public FileGroupViewModel(FileGroup fileGroup, IImageProcessor imageProcessor)
    {
        _fileGroup = fileGroup ?? throw new ArgumentNullException(nameof(fileGroup));
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        InitializeImagePaths();
    }

    /// <summary>
    /// The underlying FileGroup model.
    /// </summary>
    public FileGroup Model => _fileGroup;

    /// <summary>
    /// Unique identifier for this file group.
    /// </summary>
    public string GroupId => _fileGroup.GroupId;

    /// <summary>
    /// NIR file key/identifier.
    /// </summary>
    public string NirKey => _fileGroup.NirKey;

    /// <summary>
    /// Path to the normal folder.
    /// </summary>
    public string NormalFolder => _fileGroup.NormalFolder;

    /// <summary>
    /// Line number for multi-line monitoring.
    /// </summary>
    public int LineNumber => _fileGroup.LineNumber;

    /// <summary>
    /// Indicates whether this group has an associated NIR file.
    /// </summary>
    public bool HasNir => _fileGroup.HasNir;

    /// <summary>
    /// Timestamp when this group was created.
    /// </summary>
    public DateTime CreatedAt => _fileGroup.CreatedAt;

    /// <summary>
    /// Current status of this file group.
    /// </summary>
    public GroupStatus Status => _fileGroup.Status;

    /// <summary>
    /// Status display text for UI.
    /// </summary>
    public string StatusText => Status switch
    {
        GroupStatus.Complete => "✓ Complete",
        GroupStatus.Pending => "⏳ Pending",
        GroupStatus.Processing => "⚙ Processing",
        GroupStatus.Moved => "📦 Moved",
        GroupStatus.Error => "✗ Error",
        GroupStatus.Abnormal => "⚠ Abnormal",
        _ => Status.ToString()
    };

    /// <summary>
    /// Indicates whether this group has abnormal characteristics.
    /// </summary>
    public bool IsAbnormal => Status == GroupStatus.Abnormal;

    /// <summary>
    /// Whether this group is selected in the UI.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Path to the main image for display.
    /// </summary>
    public string? MainImagePath
    {
        get => _mainImagePath;
        set => SetProperty(ref _mainImagePath, value);
    }

    /// <summary>
    /// Path to the NIR image for display.
    /// </summary>
    public string? NirImagePath
    {
        get => _nirImagePath;
        set => SetProperty(ref _nirImagePath, value);
    }

    /// <summary>
    /// Gets the camera image path for a specific camera number (1-6).
    /// </summary>
    public string? GetCameraImagePath(int cameraNumber)
    {
        var key = $"cam{cameraNumber}";
        return _cameraImagePaths.TryGetValue(key, out var path) ? path : null;
    }

    /// <summary>
    /// Sets the camera image path for a specific camera number (1-6).
    /// </summary>
    public void SetCameraImagePath(int cameraNumber, string? path)
    {
        var key = $"cam{cameraNumber}";
        if (_cameraImagePaths.TryGetValue(key, out var currentPath) && currentPath == path)
            return;

        _cameraImagePaths[key] = path;
        OnPropertyChanged($"Camera{cameraNumber}ImagePath");
    }

    /// <summary>
    /// Camera 1 image path (for data binding).
    /// </summary>
    public string? Camera1ImagePath
    {
        get => GetCameraImagePath(1);
        set => SetCameraImagePath(1, value);
    }

    /// <summary>
    /// Camera 2 image path (for data binding).
    /// </summary>
    public string? Camera2ImagePath
    {
        get => GetCameraImagePath(2);
        set => SetCameraImagePath(2, value);
    }

    /// <summary>
    /// Camera 3 image path (for data binding).
    /// </summary>
    public string? Camera3ImagePath
    {
        get => GetCameraImagePath(3);
        set => SetCameraImagePath(3, value);
    }

    /// <summary>
    /// Camera 4 image path (for data binding).
    /// </summary>
    public string? Camera4ImagePath
    {
        get => GetCameraImagePath(4);
        set => SetCameraImagePath(4, value);
    }

    /// <summary>
    /// Camera 5 image path (for data binding).
    /// </summary>
    public string? Camera5ImagePath
    {
        get => GetCameraImagePath(5);
        set => SetCameraImagePath(5, value);
    }

    /// <summary>
    /// Camera 6 image path (for data binding).
    /// </summary>
    public string? Camera6ImagePath
    {
        get => GetCameraImagePath(6);
        set => SetCameraImagePath(6, value);
    }

    /// <summary>
    /// Cam1ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam1ImagePath => Camera1ImagePath;

    /// <summary>
    /// Cam2ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam2ImagePath => Camera2ImagePath;

    /// <summary>
    /// Cam3ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam3ImagePath => Camera3ImagePath;

    /// <summary>
    /// Cam4ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam4ImagePath => Camera4ImagePath;

    /// <summary>
    /// Cam5ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam5ImagePath => Camera5ImagePath;

    /// <summary>
    /// Cam6ImagePath alias for XAML binding compatibility.
    /// </summary>
    public string? Cam6ImagePath => Camera6ImagePath;

    /// <summary>
    /// Main image thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? MainImageThumbnail
    {
        get => _mainImageThumbnail;
        private set => SetProperty(ref _mainImageThumbnail, value);
    }

    /// <summary>
    /// NIR image thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? NirImageThumbnail
    {
        get => _nirImageThumbnail;
        private set => SetProperty(ref _nirImageThumbnail, value);
    }

    /// <summary>
    /// Camera 1 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera1Thumbnail
    {
        get => _camera1Thumbnail;
        private set => SetProperty(ref _camera1Thumbnail, value);
    }

    /// <summary>
    /// Camera 2 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera2Thumbnail
    {
        get => _camera2Thumbnail;
        private set => SetProperty(ref _camera2Thumbnail, value);
    }

    /// <summary>
    /// Camera 3 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera3Thumbnail
    {
        get => _camera3Thumbnail;
        private set => SetProperty(ref _camera3Thumbnail, value);
    }

    /// <summary>
    /// Camera 4 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera4Thumbnail
    {
        get => _camera4Thumbnail;
        private set => SetProperty(ref _camera4Thumbnail, value);
    }

    /// <summary>
    /// Camera 5 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera5Thumbnail
    {
        get => _camera5Thumbnail;
        private set => SetProperty(ref _camera5Thumbnail, value);
    }

    /// <summary>
    /// Camera 6 thumbnail for display in DataGrid.
    /// </summary>
    public BitmapSource? Camera6Thumbnail
    {
        get => _camera6Thumbnail;
        private set => SetProperty(ref _camera6Thumbnail, value);
    }

    /// <summary>
    /// Initializes image paths from the FileGroup model.
    /// </summary>
    private void InitializeImagePaths()
    {
        // Initialize camera image paths from the model
        foreach (var kvp in _fileGroup.CameraFiles)
        {
            if (kvp.Key.StartsWith("cam", StringComparison.OrdinalIgnoreCase))
            {
                _cameraImagePaths[kvp.Key.ToLower()] = kvp.Value;
            }
        }

        // Set NIR image path if available
        if (_fileGroup.HasNir && !string.IsNullOrEmpty(_fileGroup.NirKey))
        {
            _nirImagePath = _fileGroup.NirKey;
        }

        // Set main image path (prioritize explicitly set MainImagePath)
        if (!string.IsNullOrEmpty(_fileGroup.MainImagePath))
        {
            _mainImagePath = _fileGroup.MainImagePath;
        }
        else if (_cameraImagePaths.Count > 0)
        {
            _mainImagePath = _cameraImagePaths.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        }
        else if (!string.IsNullOrEmpty(_fileGroup.NormalFolder))
        {
            _mainImagePath = _fileGroup.NormalFolder;
        }
    }

    /// <summary>
    /// Updates the ViewModel from the underlying model (call after model changes).
    /// </summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(GroupId));
        OnPropertyChanged(nameof(NirKey));
        OnPropertyChanged(nameof(NormalFolder));
        OnPropertyChanged(nameof(LineNumber));
        OnPropertyChanged(nameof(HasNir));
        OnPropertyChanged(nameof(CreatedAt));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusText));
        
        InitializeImagePaths();
        OnPropertyChanged(nameof(MainImagePath));
        OnPropertyChanged(nameof(NirImagePath));
        OnPropertyChanged(nameof(Camera1ImagePath));
        OnPropertyChanged(nameof(Camera2ImagePath));
        OnPropertyChanged(nameof(Camera3ImagePath));
        OnPropertyChanged(nameof(Camera4ImagePath));
        OnPropertyChanged(nameof(Camera5ImagePath));
        OnPropertyChanged(nameof(Camera6ImagePath));
    }

    /// <summary>
    /// Loads thumbnail images asynchronously for all image paths in this file group.
    /// Uses Dispatcher.InvokeAsync to update UI properties from background thread.
    /// </summary>
    public async Task LoadThumbnailsAsync()
    {
        if (_disposed)
            return;

        const int thumbnailWidth = 100;
        const int thumbnailHeight = 100;

        try
        {
            // Load main image thumbnail
            if (!string.IsNullOrEmpty(MainImagePath))
            {
                var mainThumbnail = await LoadSingleThumbnailAsync(MainImagePath, thumbnailWidth, thumbnailHeight);
                await Application.Current.Dispatcher.InvokeAsync(() => MainImageThumbnail = mainThumbnail);
            }

            // Load NIR image thumbnail
            if (!string.IsNullOrEmpty(NirImagePath))
            {
                var nirThumbnail = await LoadSingleThumbnailAsync(NirImagePath, thumbnailWidth, thumbnailHeight);
                await Application.Current.Dispatcher.InvokeAsync(() => NirImageThumbnail = nirThumbnail);
            }

            // Load camera thumbnails
            await LoadCameraThumbnailAsync(1, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(2, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(3, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(4, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(5, thumbnailWidth, thumbnailHeight);
            await LoadCameraThumbnailAsync(6, thumbnailWidth, thumbnailHeight);
        }
        catch (OperationCanceledException)
        {
            // Expected when disposing - no action needed
        }
        catch (Exception ex)
        {
            // Log error but don't throw - thumbnails are non-critical
            System.Diagnostics.Debug.WriteLine($"Error loading thumbnails for group {GroupId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a single thumbnail image and converts it to BitmapSource.
    /// </summary>
    private async Task<BitmapSource?> LoadSingleThumbnailAsync(string imagePath, int width, int height)
    {
        if (_disposed || string.IsNullOrEmpty(imagePath))
            return null;

        try
        {
            var thumbnailBytes = await _imageProcessor.GenerateThumbnailAsync(
                imagePath, width, height, _cancellationTokenSource.Token);

            if (thumbnailBytes == null || thumbnailBytes.Length == 0)
                return null;

            // Convert byte array to BitmapSource
            using var ms = new System.IO.MemoryStream(thumbnailBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze(); // Make it thread-safe for cross-thread access
            return bitmap;
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation
        }
        catch (Exception)
        {
            // Return null for failed loads - placeholder will be shown
            return null;
        }
    }

    /// <summary>
    /// Loads a camera thumbnail by camera number.
    /// </summary>
    private async Task LoadCameraThumbnailAsync(int cameraNumber, int width, int height)
    {
        if (_disposed)
            return;

        var imagePath = GetCameraImagePath(cameraNumber);
        if (string.IsNullOrEmpty(imagePath))
            return;

        var thumbnail = await LoadSingleThumbnailAsync(imagePath, width, height);
        
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            switch (cameraNumber)
            {
                case 1: Camera1Thumbnail = thumbnail; break;
                case 2: Camera2Thumbnail = thumbnail; break;
                case 3: Camera3Thumbnail = thumbnail; break;
                case 4: Camera4Thumbnail = thumbnail; break;
                case 5: Camera5Thumbnail = thumbnail; break;
                case 6: Camera6Thumbnail = thumbnail; break;
            }
        });
    }

    /// <summary>
    /// Disposes resources and cancels any pending thumbnail loading operations.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
