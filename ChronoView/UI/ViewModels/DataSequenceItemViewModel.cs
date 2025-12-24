using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChronoView.UI.ViewModels
{
    /// <summary>
    /// ViewModel for a single data sequence item (for drag-drop reordering)
    /// </summary>
    public class DataSequenceItemViewModel : INotifyPropertyChanged
    {
        private Models.DataType _type;
        private int _order;
        private double _minDelay;
        private double _maxDelay;
        private bool _enabled;
        private bool _isDropTarget;

        /// <summary>
        /// Type of data (NIR, Normal, Camera)
        /// </summary>
        public Models.DataType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// Order in sequence (1-based, lower = earlier)
        /// </summary>
        public int Order
        {
            get => _order;
            set
            {
                if (_order != value)
                {
                    _order = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Minimum delay in seconds from previous data type
        /// Example: For Camera1 after Normal, MinDelay=4 means "Normal + 4 seconds minimum"
        /// </summary>
        public double MinDelay
        {
            get => _minDelay;
            set
            {
                if (_minDelay != value)
                {
                    _minDelay = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Maximum delay in seconds from previous data type
        /// Example: For Camera1 after Normal, MaxDelay=6 means "Normal + 6 seconds maximum"
        /// Combined with MinDelay: Camera1 must arrive between Normal+4s and Normal+6s
        /// </summary>
        public double MaxDelay
        {
            get => _maxDelay;
            set
            {
                if (_maxDelay != value)
                {
                    _maxDelay = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether this data type is enabled in the sequence
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether this item is currently a drop target (for visual feedback)
        /// </summary>
        public bool IsDropTarget
        {
            get => _isDropTarget;
            set
            {
                if (_isDropTarget != value)
                {
                    _isDropTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Display name for UI
        /// Note: Cam4-6 exist internally for Line 2, but are hidden from settings UI
        /// </summary>
        public string DisplayName
        {
            get
            {
                return Core.Localization.LocalizationManager.GetDataTypeDisplayName(Type);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
