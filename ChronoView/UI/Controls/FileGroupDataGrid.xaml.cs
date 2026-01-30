using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;

namespace ChronoView.UI.Controls;

public partial class FileGroupDataGrid : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(FileGroupDataGrid), new PropertyMetadata(null));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty LineNumberProperty =
        DependencyProperty.Register(nameof(LineNumber), typeof(int), typeof(FileGroupDataGrid), new PropertyMetadata(1, OnLineNumberChanged));

    public int LineNumber
    {
        get => (int)GetValue(LineNumberProperty);
        set => SetValue(LineNumberProperty, value);
    }

    // Sticky scroll state tracking
    private ScrollViewer? _scrollViewer;
    private bool _isUserAtBottom = true;
    private bool _hasPerformedInitialAutoScroll;
    private const double ScrollTolerance = 2.0;

    public FileGroupDataGrid()
    {
        InitializeComponent();
        Loaded += OnControlLoaded;
        Unloaded += OnControlUnloaded;
    }

    private static void OnLineNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileGroupDataGrid grid)
        {
            grid.RefreshColumns();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshColumns();
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        // Find ScrollViewer in visual tree and subscribe to ScrollChanged
        _scrollViewer = GetVisualChild<ScrollViewer>(MainDataGrid);
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void OnControlUnloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe to prevent memory leaks
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (MainDataGrid == null || MainDataGrid.Items.Count == 0)
        {
            _hasPerformedInitialAutoScroll = false;
            _isUserAtBottom = true;
            return;
        }

        // Check if viewport is at the bottom
        bool atBottom = (e.VerticalOffset + e.ViewportHeight) >= (e.ExtentHeight - ScrollTolerance);

        // Only update user intent when content size hasn't changed (user is scrolling)
        if (e.ExtentHeightChange == 0)
        {
            _isUserAtBottom = atBottom;
        }
        // When content grows (new items added), preserve previous state

        if (!_hasPerformedInitialAutoScroll && _scrollViewer != null && _scrollViewer.ScrollableHeight > 0 && _isUserAtBottom)
        {
            _hasPerformedInitialAutoScroll = true;
            MainDataGrid.ScrollIntoView(MainDataGrid.Items[^1]);
        }
    }

    /// <summary>
    /// Finds a child element of the specified type in the visual tree.
    /// </summary>
    private static T? GetVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = GetVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public void RefreshColumns()
    {
        if (MainDataGrid == null) return;

        try
        {
            // Use Application.Current to get config service
            if (WpfApplication.Current is App app)
            {
                var configManager = app.Services.GetRequiredService<IConfigurationManager>();
                var config = configManager.LoadConfiguration<ApplicationConfiguration>();
                var sequenceSettings = config.DataSequenceSettings;
                // Show ALL data types as columns (Enabled flag only affects matching strategy, not UI visibility)
                var orderedTypes = sequenceSettings.GetAllOrderedTypes();

                MainDataGrid.BeginInit();

                // Keep first 3 static columns (Checkbox, Index, Status)
                while (MainDataGrid.Columns.Count > 3)
                {
                    MainDataGrid.Columns.RemoveAt(3);
                }

                foreach (var dataType in orderedTypes)
                {
                    if (!IsDataTypeForLine(dataType, LineNumber)) continue;

                    var column = CreateColumnForDataType(dataType, LineNumber);
                    if (column != null)
                    {
                        MainDataGrid.Columns.Add(column);
                    }
                }

                MainDataGrid.EndInit();
            }
        }
        catch (Exception)
        {
            // Fallback or log if needed
        }
    }

    private bool IsDataTypeForLine(DataType dataType, int lineNumber)
    {
        return dataType switch
        {
            DataType.Normal => true,
            DataType.NIR => true,
            DataType.Cam1 => true,
            DataType.Cam2 => true,
            DataType.Cam3 => true,
            DataType.Cam4 => true,
            DataType.Cam5 => true,
            DataType.Cam6 => true,
            _ => false
        };
    }

    private DataGridTemplateColumn? CreateColumnForDataType(DataType dataType, int lineNumber)
    {
        string? resourceKey = null;

        DataType headerDataType = dataType;

        if (lineNumber == 1)
        {
            resourceKey = dataType switch
            {
                DataType.Normal => "NormalFileTemplate",
                DataType.NIR => "NirFileTemplate",
                DataType.Cam1 => "Camera1Template",
                DataType.Cam2 => "Camera2Template",
                DataType.Cam3 => "Camera3Template",
                _ => null
            };
        }
        else if (lineNumber == 2)
        {
            // Line 2 maps Cam1/2/3 types to Cam4/5/6 headers and templates
            // Line 2 uses NIR2 template for NIR data
            if (dataType == DataType.Cam1) headerDataType = DataType.Cam4;
            else if (dataType == DataType.Cam2) headerDataType = DataType.Cam5;
            else if (dataType == DataType.Cam3) headerDataType = DataType.Cam6;

            resourceKey = dataType switch
            {
                DataType.Normal => "NormalFileTemplate",
                DataType.NIR => "Nir2FileTemplate",  // Line 2 uses NIR2 template
                DataType.Cam1 => "Camera4Template",
                DataType.Cam2 => "Camera5Template",
                DataType.Cam3 => "Camera6Template",
                _ => null
            };
        }

        if (string.IsNullOrEmpty(resourceKey)) return null;

        // 헤더를 가운데 정렬하기 위해 스타일을 생성한다.
        var headerStyle = new Style(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));

        var column = new DataGridTemplateColumn
        {
            Header = Core.Localization.LocalizationManager.GetColumnHeader(headerDataType),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = (DataTemplate)FindResource(resourceKey),
            HeaderStyle = headerStyle
        };

        return column;
    }

    private void OnRowCheckboxPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // CheckBox 클릭 시 바인딩된 항목 선택 상태를 토글하고 이벤트 중복 처리를 방지한다.
        if (sender is not System.Windows.Controls.CheckBox checkBox) return;
        if (checkBox.DataContext is not FileGroupViewModel item) return;

        item.IsSelected = !item.IsSelected;
        checkBox.IsChecked = item.IsSelected;
        e.Handled = true;
    }

    private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (ItemsSource is IEnumerable<FileGroupViewModel> items)
        {
            foreach (var item in items) item.IsSelected = true;
        }
    }

    private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (ItemsSource is IEnumerable<FileGroupViewModel> items)
        {
            foreach (var item in items) item.IsSelected = false;
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// DataGrid를 마지막 항목까지 스크롤합니다. 
    /// 사용자가 스크롤을 올려놓은 상태라면 자동 스크롤하지 않습니다.
    /// </summary>
    public void ScrollToBottom()
    {
        if (MainDataGrid == null || MainDataGrid.Items.Count == 0) return;

        // Only scroll if user was at the bottom before this update
        if (_isUserAtBottom)
        {
            MainDataGrid.ScrollIntoView(MainDataGrid.Items[^1]);
        }
    }
}
