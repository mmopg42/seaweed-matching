using System.IO;
using System.Windows;
using System.Windows.Input;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Media.Imaging;
using WpfMessageBox = System.Windows.MessageBox;

namespace ChronoView.UI.Views;

/// <summary>
/// Dialog for displaying full-size image previews with EXIF rotation handling.
/// </summary>
public partial class ImagePreviewDialog : Window
{
    private double _currentZoom = 1.0;
    private const double ZoomIncrement = 0.2;

    public ImagePreviewDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string ImageTitle { get; set; } = Core.Localization.LocalizationManager.GetString("Dialog_ImagePreview");
    public bool IsLoading { get; set; }

    /// <summary>
    /// Loads and displays an image from the specified file path.
    /// Handles EXIF rotation automatically.
    /// </summary>
    public async Task LoadImageAsync(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            WpfMessageBox.Show(
                Core.Localization.LocalizationManager.GetString("Message_ImageNotFound"),
                Core.Localization.LocalizationManager.GetString("Dialog_Error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        IsLoading = true;
        ImageTitle = Path.GetFileName(imagePath);

        try
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    
                    // Handle EXIF rotation
                    bitmap.Rotation = GetExifRotation(imagePath);
                    
                    bitmap.EndInit();
                    bitmap.Freeze();

                    PreviewImage.Source = bitmap;
                });
            });
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Reads EXIF orientation data and returns the appropriate rotation.
    /// </summary>
    private Rotation GetExifRotation(string imagePath)
    {
        try
        {
            using var stream = File.OpenRead(imagePath);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
            
            if (decoder.Frames.Count > 0)
            {
                var frame = decoder.Frames[0];
                var metadata = frame.Metadata as BitmapMetadata;
                
                if (metadata != null && metadata.ContainsQuery("System.Photo.Orientation"))
                {
                    var orientation = metadata.GetQuery("System.Photo.Orientation");
                    if (orientation != null)
                    {
                        return (ushort)orientation switch
                        {
                            6 => Rotation.Rotate90,
                            3 => Rotation.Rotate180,
                            8 => Rotation.Rotate270,
                            _ => Rotation.Rotate0
                        };
                    }
                }
            }
        }
        catch
        {
            // If we can't read EXIF data, just use no rotation
        }

        return Rotation.Rotate0;
    }

    private void RotateLeft_Click(object sender, RoutedEventArgs e)
    {
        ImageRotation.Angle -= 90;
    }

    private void RotateRight_Click(object sender, RoutedEventArgs e)
    {
        ImageRotation.Angle += 90;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        _currentZoom += ZoomIncrement;
        PreviewImage.LayoutTransform = new System.Windows.Media.ScaleTransform(_currentZoom, _currentZoom);
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        _currentZoom = Math.Max(0.2, _currentZoom - ZoomIncrement);
        PreviewImage.LayoutTransform = new System.Windows.Media.ScaleTransform(_currentZoom, _currentZoom);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _currentZoom = 1.0;
        ImageRotation.Angle = 0;
        PreviewImage.LayoutTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
