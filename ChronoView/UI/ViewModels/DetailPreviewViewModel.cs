using System.Collections.ObjectModel;
using System.Windows.Input;
using ChronoView.Models;

namespace ChronoView.UI.ViewModels
{
    public class DetailPreviewViewModel : ViewModelBase
    {
        private FileGroupViewModel? _selectedGroup;
        private bool _isVisible;
        private string _statusMessage = string.Empty;
        private string _statusColor = "#e0e0e0"; // Default gray
        
        // Large preview images
        private string? _mainImagePath;
        private string? _nirImagePath;
        
        // Composite cameras collection
        public ObservableCollection<CameraImageViewModel> CompositeCameras { get; } = new();

        public DetailPreviewViewModel()
        {
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        public ICommand CloseCommand { get; }

        public FileGroupViewModel? SelectedGroup
        {
            get => _selectedGroup;
            private set => SetProperty(ref _selectedGroup, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public string? MainImagePath
        {
            get => _mainImagePath;
            set => SetProperty(ref _mainImagePath, value);
        }

        public string? NirImagePath
        {
            get => _nirImagePath;
            set => SetProperty(ref _nirImagePath, value);
        }

        public void UpdateGroup(FileGroupViewModel? group)
        {
            SelectedGroup = group;
            
            if (group == null)
            {
                IsVisible = false;
                StatusMessage = "No Selection";
                StatusColor = "#e0e0e0";
                MainImagePath = null;
                NirImagePath = null;
                CompositeCameras.Clear();
                return;
            }

            // Update Status
            UpdateStatus(group);

            // Update Images
            MainImagePath = group.MainImagePath; 
            NirImagePath = group.NirImagePath;

            // Update Composite Cameras
            CompositeCameras.Clear();
            if (group.LineNumber == 1)
            {
                AddCamera("Cam 1", group.Camera1ImagePath, 1);
                AddCamera("Cam 2", group.Camera2ImagePath, 2);
                AddCamera("Cam 3", group.Camera3ImagePath, 3);
            }
            else
            {
                AddCamera("Cam 4", group.Camera4ImagePath, 4);
                AddCamera("Cam 5", group.Camera5ImagePath, 5);
                AddCamera("Cam 6", group.Camera6ImagePath, 6);
            }

            IsVisible = true;
        }

        private void UpdateStatus(FileGroupViewModel group)
        {
            StatusMessage = group.StatusText;
            
            // Simple color mapping based on status text or properties
            if (group.IsAbnormal)
            {
                StatusColor = "#ffc107"; // Warning Yellow/Orange
            }
            else if (group.StatusText == "Incomplete" || group.StatusText.Contains("Missing"))
            {
                StatusColor = "#dc3545"; // Error Red
            }
            else
            {
                 StatusColor = "#28a745"; // Success Green
            }
        }

        private void AddCamera(string name, string? path, int index)
        {
            CompositeCameras.Add(new CameraImageViewModel 
            { 
                Name = name, 
                ImagePath = path,
                Index = index
            });
        }

        private void ExecuteClose()
        {
            IsVisible = false;
        }
    }

    public class CameraImageViewModel : ViewModelBase
    {
        private string _name = string.Empty;
        private string? _imagePath;
        private int _index;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string? ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }
    }
}
