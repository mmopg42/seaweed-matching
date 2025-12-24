using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using ChronoView.UI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;

// Using aliases to avoid ambiguous references with System.Windows.Forms
using WpfBorder = System.Windows.Controls.Border;
using WpfImage = System.Windows.Controls.Image;
using WpfGrid = System.Windows.Controls.Grid;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfBinding = System.Windows.Data.Binding;

namespace ChronoView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel, ILogger<MainWindow> logger)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        InitializeComponent();
        
        // Set DataContext to injected ViewModel
        DataContext = _viewModel;
        
        _logger.LogInformation("MainWindow initialized with dependency injection");
    }



    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _logger?.LogInformation("MainWindow loaded");

        // Generate dynamic columns based on DataSequenceSettings
        GenerateDataGridColumns();

        // Restore window state from configuration
        // This will be implemented when window state manager is available
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _logger?.LogInformation("MainWindow closing");
        // Save window state to configuration
        // This will be implemented when window state manager is available
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Setup_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsDialog();
    }

    /// <summary>
    /// Opens the settings dialog when Setup button is clicked.
    /// This is called from XAML instead of using Command binding.
    /// </summary>
    public void OpenSettingsDialog()
    {
        _logger.LogInformation("Opening settings dialog");

        // Get SettingsDialog from DI container
        var app = (App)WpfApplication.Current;
        var settingsDialog = app.Services.GetRequiredService<SettingsDialog>();
        settingsDialog.Owner = this;

        // Subscribe to SettingsApplied event
        if (settingsDialog.DataContext is SettingsDialogViewModel settingsViewModel)
        {
            settingsViewModel.SettingsApplied += OnSettingsApplied;
        }

        var result = settingsDialog.ShowDialog();

        // Unsubscribe from event
        if (settingsDialog.DataContext is SettingsDialogViewModel vm)
        {
            vm.SettingsApplied -= OnSettingsApplied;
        }

        if (result == true)
        {
            _logger.LogInformation("Settings saved");
            _viewModel.AddLogMessage(LogSeverity.Info, "System", "Settings saved successfully");
        }
        else
        {
            _logger.LogInformation("Settings cancelled");
        }
    }

    /// <summary>
    /// Handles the SettingsApplied event to reload all settings and restart monitoring.
    /// </summary>
    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        _logger.LogInformation("Settings applied, reloading all settings and restarting monitoring");
        
        // Reload UI display settings from configuration
        var app = (App)WpfApplication.Current;
        var configManager = app.Services.GetRequiredService<Core.Configuration.IConfigurationManager>();
        var config = configManager.LoadConfiguration<ApplicationConfiguration>();
        
        _viewModel.DisplayImageWidth = config.UISettings.DisplayImageWidth;
        _viewModel.DisplayImageHeight = config.UISettings.DisplayImageHeight;
        _viewModel.DataGridRowHeight = config.UISettings.DataGridRowHeight;
        _viewModel.NirDisplayWidth = config.UISettings.NirDisplayWidth;
        _viewModel.NirDisplayHeight = config.UISettings.NirDisplayHeight;
        
        // Regenerate DataGrid columns based on updated DataSequenceSettings
        _logger.LogInformation("Regenerating DataGrid columns after settings change");
        GenerateDataGridColumns();
        
        // Restart monitoring if currently running to apply new settings
        if (_viewModel.IsMonitoring)
        {
            _viewModel.StopCommand.Execute(null);
            System.Threading.Thread.Sleep(500); // Brief pause
            _viewModel.StartCommand.Execute(null);
            _viewModel.AddLogMessage(LogSeverity.Info, "System", "Settings applied and monitoring restarted");
        }
        else
        {
            // If not monitoring, just reload NIR graphs
            _viewModel.ReloadNirGraphThumbnails();
            _viewModel.AddLogMessage(LogSeverity.Info, "System", "Display settings applied");
        }
    }

    private void FileGroupRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.DataGridRow row)
        {
            _logger.LogInformation("Row DoubleClick detected. DataContext type: {Type}", row.DataContext?.GetType().Name ?? "null");
            
            if (row.DataContext is FileGroupViewModel group && _viewModel != null)
            {
                _logger.LogInformation("Executing OpenDetailViewCommand for group: {GroupId}", group.GroupId);
                _viewModel.OpenDetailViewCommand.Execute(group);
                e.Handled = true;
            }
            else
            {
                 _logger.LogWarning("Row DataContext is not FileGroupViewModel or ViewModel is null");
            }
        }
    }
    
    private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Force command re-evaluation for CanExecute
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Handles SelectAll checkbox Checked event
    /// </summary>
    private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string lineTag)
        {
            _logger.LogDebug("SelectAll checkbox checked for {Line}", lineTag);
            
            if (lineTag == "Line1")
            {
                foreach (var group in _viewModel.Line1Groups)
                {
                    group.IsSelected = true;
                }
            }
            else if (lineTag == "Line2")
            {
                foreach (var group in _viewModel.Line2Groups)
                {
                    group.IsSelected = true;
                }
            }
        }
    }

    /// <summary>
    /// Handles SelectAll checkbox Unchecked event
    /// </summary>
    private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string lineTag)
        {
            _logger.LogDebug("SelectAll checkbox unchecked for {Line}", lineTag);
            
            if (lineTag == "Line1")
            {
                foreach (var group in _viewModel.Line1Groups)
                {
                    group.IsSelected = false;
                }
            }
            else if (lineTag == "Line2")
            {
                foreach (var group in _viewModel.Line2Groups)
                {
                    group.IsSelected = false;
                }
            }
        }
    }

    #region Dynamic Column Generation

    /// <summary>
    /// Generate DataGrid columns dynamically based on DataSequenceSettings
    /// </summary>
    private void GenerateDataGridColumns()
    {
        try
        {
            var app = (App)WpfApplication.Current;
            var configManager = app.Services.GetRequiredService<Core.Configuration.IConfigurationManager>();
            var config = configManager.LoadConfiguration<ApplicationConfiguration>();
            var sequenceSettings = config.DataSequenceSettings;

            // Get ordered types from configuration
            var orderedTypes = sequenceSettings.GetOrderedTypes();

            _logger.LogInformation("Generating dynamic columns. Ordered types: {Types}",
                string.Join(", ", orderedTypes));

            // Generate for Line 1 (Normal, NIR, Cam1-3)
            GenerateColumnsForDataGrid(Line1DataGrid, orderedTypes, 1);

            // Generate for Line 2 (Normal2, NIR2, Cam4-6)
            GenerateColumnsForDataGrid(Line2DataGrid, orderedTypes, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate dynamic columns");
        }
    }

    /// <summary>
    /// Generate columns for a specific DataGrid
    /// </summary>
    private void GenerateColumnsForDataGrid(System.Windows.Controls.DataGrid dataGrid, List<DataType> orderedTypes, int lineNumber)
    {
        if (dataGrid == null)
        {
            _logger.LogWarning("DataGrid is null, cannot generate columns for Line {Line}", lineNumber);
            return;
        }

        // BeginInit to prevent UI flicker
        dataGrid.BeginInit();

        try
        {
            // Keep first 3 static columns (Checkbox, Index, Status)
            // Remove all columns after index 2
            while (dataGrid.Columns.Count > 3)
            {
                dataGrid.Columns.RemoveAt(3);
            }

            // Add dynamic data columns based on sequence
            foreach (var dataType in orderedTypes)
            {
                // Skip data types not relevant to this line
                if (!IsDataTypeForLine(dataType, lineNumber))
                {
                    _logger.LogDebug("Skipping {DataType} for Line {Line}", dataType, lineNumber);
                    continue;
                }

                var column = CreateColumnForDataType(dataType, lineNumber);
                if (column != null)
                {
                    dataGrid.Columns.Add(column);
                    _logger.LogDebug("Add column: {Header} for Line {Line}", column.Header, lineNumber);
                }
            }
        }
        finally
        {
            // EndInit to commit changes and update UI
            dataGrid.EndInit();
        }
    }

    /// <summary>
    /// Check if a data type is relevant for a specific line
    /// Line 1: Normal, NIR, Cam1-3
    /// Line 2: Normal, NIR, Cam1-3 (automatically mapped to Cam4-6)
    /// </summary>
    private bool IsDataTypeForLine(DataType dataType, int lineNumber)
    {
        return dataType switch
        {
            DataType.Normal => true,  // Both lines
            DataType.NIR => true,     // Both lines
            DataType.Cam1 => true,    // Both lines (Cam1 for Line 1, Cam4 for Line 2)
            DataType.Cam2 => true,    // Both lines (Cam2 for Line 1, Cam5 for Line 2)
            DataType.Cam3 => true,    // Both lines (Cam3 for Line 1, Cam6 for Line 2)
            _ => false
        };
    }

    /// <summary>
    /// Create a DataGrid column for a specific data type
    /// </summary>
    private DataGridTemplateColumn? CreateColumnForDataType(DataType dataType, int lineNumber)
    {
        var column = new DataGridTemplateColumn
        {
            Header = GetColumnHeader(dataType, lineNumber),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        };

        // Create data template
        var factory = new FrameworkElementFactory(typeof(WpfBorder));

        // Bind to appropriate width/height based on data type
        if (dataType == DataType.NIR)
        {
            factory.SetBinding(WpfBorder.WidthProperty,
                new WpfBinding("DataContext.NirDisplayWidth")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1),
                    FallbackValue = 120
                });
            factory.SetBinding(WpfBorder.HeightProperty,
                new WpfBinding("DataContext.NirDisplayHeight")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1),
                    FallbackValue = 90
                });
        }
        else
        {
            factory.SetBinding(WpfBorder.WidthProperty,
                new WpfBinding("DataContext.DisplayImageWidth")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1),
                    FallbackValue = 120
                });
            factory.SetBinding(WpfBorder.HeightProperty,
                new WpfBinding("DataContext.DisplayImageHeight")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1),
                    FallbackValue = 90
                });
        }

        factory.SetValue(WpfBorder.BackgroundProperty, new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3a3a3a")));
        factory.SetValue(WpfBorder.BorderBrushProperty, new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#505050")));
        factory.SetValue(WpfBorder.BorderThicknessProperty, new Thickness(1));

        //Inner Grid
        var gridFactory = new FrameworkElementFactory(typeof(WpfGrid));

        // Image
        var imageFactory = new FrameworkElementFactory(typeof(WpfImage));
       imageFactory.SetBinding(WpfImage.SourceProperty, new WpfBinding(GetBindingPath(dataType, lineNumber)));
        imageFactory.SetValue(WpfImage.StretchProperty, System.Windows.Media.Stretch.Uniform);
        gridFactory.AppendChild(imageFactory);

        // ProgressBar (shown when image is loading)
        var progressFactory = new FrameworkElementFactory(typeof(WpfProgressBar));
        progressFactory.SetValue(WpfProgressBar.IsIndeterminateProperty, true);
        progressFactory.SetValue(WpfProgressBar.HeightProperty, 4.0);
        progressFactory.SetValue(WpfProgressBar.VerticalAlignmentProperty, VerticalAlignment.Bottom);

        // ProgressBar visibility: show when image is null (loading)
        // Create converter instance directly instead of resource lookup to avoid runtime errors
        var nullToVisConverter = new NullToVisibilityConverter();
        var visibilityBinding = new WpfBinding(GetBindingPath(dataType, lineNumber))
        {
            Converter = nullToVisConverter
        };
        progressFactory.SetBinding(WpfProgressBar.VisibilityProperty, visibilityBinding);
        gridFactory.AppendChild(progressFactory);

        factory.AppendChild(gridFactory);

        var template = new DataTemplate { VisualTree = factory };
        column.CellTemplate = template;

        return column;
    }

    /// <summary>
    /// Get column header text for a data type
    /// Cam1-3 show as "Cam 1", "Cam 2", "Cam 3" regardless of line
    /// (Actual camera used is determined by lineNumber: Line 1 = Cam1-3, Line 2 = Cam4-6)
    /// </summary>
    private string GetColumnHeader(DataType dataType, int lineNumber)
    {
        return Core.Localization.LocalizationManager.GetColumnHeader(dataType);
    }

    /// <summary>
    /// Get binding path for a data type to access the thumbnail property
    /// Automatically maps Cam1-3 to Cam4-6 for Line 2
    /// </summary>
    private string GetBindingPath(DataType dataType, int lineNumber)
    {
        return dataType switch
        {
            DataType.Normal => "MainImageThumbnail",
            DataType.NIR => "NirGraphThumbnail",
            DataType.Cam1 => lineNumber == 1 ? "Camera1Thumbnail" : "Camera4Thumbnail",
            DataType.Cam2 => lineNumber == 1 ? "Camera2Thumbnail" : "Camera5Thumbnail",
            DataType.Cam3 => lineNumber == 1 ? "Camera3Thumbnail" : "Camera6Thumbnail",
            _ => string.Empty
        };
    }

    #endregion
}
