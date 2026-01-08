using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChronoView.UI.Views
{
    public partial class ImagePreviewWindow : Window
    {
        public ImagePreviewWindow(ImageSource image, string title)
        {
            InitializeComponent();
            DataContext = new { DisplayImage = image, ImageTitle = title };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Close window on click (if clicking on the image)
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
