using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

    public FileGroupDataGrid()
    {
        InitializeComponent();
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
                var orderedTypes = sequenceSettings.GetOrderedTypes();

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
            resourceKey = dataType switch
            {
                DataType.Normal => "NormalFileTemplate",
                DataType.NIR => "NirFileTemplate",
                DataType.Cam1 => "Camera4Template",
                DataType.Cam2 => "Camera5Template",
                DataType.Cam3 => "Camera6Template",
                _ => null
            };
        }

        if (string.IsNullOrEmpty(resourceKey)) return null;

        var column = new DataGridTemplateColumn
        {
            Header = Core.Localization.LocalizationManager.GetColumnHeader(dataType),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = (DataTemplate)FindResource(resourceKey)
        };

        return column;
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
    /// DataGrid를 마지막 항목까지 스크롤합니다. 항목이 없거나 DataGrid가 없으면 무시합니다.
    /// </summary>
    public void ScrollToBottom()
    {
        if (MainDataGrid == null || MainDataGrid.Items.Count == 0) return;
        MainDataGrid.ScrollIntoView(MainDataGrid.Items[^1]);
    }
}
