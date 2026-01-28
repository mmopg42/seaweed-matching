using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ChronoView.Core.Configuration;
using ChronoView.UI.ViewModels;
using Microsoft.Win32;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMessageBox = System.Windows.MessageBox;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ChronoView.UI.Controls;

/// <summary>
/// Custom control for displaying and managing log messages with search, filtering, and export capabilities.
/// </summary>
public partial class LogPanel : WpfUserControl
{
    /// <summary>
    /// Maximum number of log messages to retain in memory to prevent excessive memory usage.
    /// </summary>
    public const int MaxLogMessages = 5000;

    private ObservableCollection<LogMessage> _allMessages = new();
    private ICollectionView? _filteredView;
    private string _searchText = string.Empty;
    private string? _selectedLevel = null; // null = "All", otherwise LogSeverity enum string
    private readonly IConfigurationManager _configurationManager;

    public LogPanel()
    {
        InitializeComponent();
        InitializeLogView();
        
        // Initialize selected level after XAML is loaded
        if (LevelFilter.SelectedItem is ComboBoxItem selectedItem)
        {
            _selectedLevel = selectedItem.Tag?.ToString();
        }
    }

    /// <summary>
    /// Dependency property for the log messages collection.
    /// </summary>
    public static readonly DependencyProperty LogMessagesProperty =
        DependencyProperty.Register(
            nameof(LogMessages),
            typeof(ObservableCollection<LogMessage>),
            typeof(LogPanel),
            new PropertyMetadata(null, OnLogMessagesChanged));

    public ObservableCollection<LogMessage> LogMessages
    {
        get => (ObservableCollection<LogMessage>)GetValue(LogMessagesProperty);
        set => SetValue(LogMessagesProperty, value);
    }

    /// <summary>
    /// Dependency property for the close command (hides the log panel).
    /// </summary>
    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(
            nameof(CloseCommand),
            typeof(ICommand),
            typeof(LogPanel),
            new PropertyMetadata(null));

    public ICommand CloseCommand
    {
        get => (ICommand)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>
    /// Dependency property for the active tab index (0=Line1, 1=Line2, 2=Combined).
    /// Used for line-based log filtering.
    /// </summary>
    public static readonly DependencyProperty ActiveTabIndexProperty =
        DependencyProperty.Register(
            nameof(ActiveTabIndex),
            typeof(int),
            typeof(LogPanel),
            new PropertyMetadata(2, OnActiveTabIndexChanged)); // Default: Combined (show all)

    public int ActiveTabIndex
    {
        get => (int)GetValue(ActiveTabIndexProperty);
        set => SetValue(ActiveTabIndexProperty, value);
    }

    private static void OnActiveTabIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LogPanel panel)
        {
            panel._filteredView?.Refresh();
        }
    }

    private static void OnLogMessagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LogPanel panel)
        {
            // Unsubscribe from old collection
            if (e.OldValue is ObservableCollection<LogMessage> oldCollection)
            {
                oldCollection.CollectionChanged -= panel.OnLogMessagesCollectionChanged;
            }
            
            // Set new collection - use the same reference from DependencyProperty
            // This ensures that when LogMessages.Add() is called, it updates _allMessages too
            if (e.NewValue is ObservableCollection<LogMessage> newCollection)
            {
                panel._allMessages = newCollection;
            }
            else
            {
                // If null, create empty collection but this should not happen in normal usage
                panel._allMessages = new ObservableCollection<LogMessage>();
            }
            
            panel.InitializeLogView();
            
            // Subscribe to new collection changes for auto-scroll
            panel._allMessages.CollectionChanged += panel.OnLogMessagesCollectionChanged;
        }
    }

    private void OnLogMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (AutoScrollCheckBox.IsChecked == true && LogDataGrid.Items.Count > 0)
        {
            LogDataGrid.ScrollIntoView(LogDataGrid.Items[^1]);
        }
    }

    private void InitializeLogView()
    {
        // Ensure _allMessages is not null
        if (_allMessages == null)
        {
            _allMessages = new ObservableCollection<LogMessage>();
        }
        
        // Create or recreate the filtered view
        _filteredView = CollectionViewSource.GetDefaultView(_allMessages);
        if (_filteredView != null)
        {
            _filteredView.Filter = FilterLogMessage;
            LogDataGrid.ItemsSource = _filteredView;
        }
    }

    private bool FilterLogMessage(object obj)
    {
        if (obj is not LogMessage message)
            return false;

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var searchLower = _searchText.ToLower();
            if (!message.Message.ToLower().Contains(searchLower) &&
                !message.Source.ToLower().Contains(searchLower))
            {
                return false;
            }
        }

        // Filter by severity level
        if (_selectedLevel != null)
        {
            if (message.Severity.ToString() != _selectedLevel)
            {
                return false;
            }
        }

        // Filter by production line (ActiveTabIndex: 0=Line1, 1=Line2, 2=Combined)
        if (ActiveTabIndex == 0) // Line 1 tab: show Line 1 + System (null)
        {
            if (message.LineNumber == 2)
                return false;
        }
        else if (ActiveTabIndex == 1) // Line 2 tab: show Line 2 + System (null)
        {
            if (message.LineNumber == 1)
                return false;
        }
        // ActiveTabIndex == 2 (Combined): show all

        return true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        _filteredView?.Refresh();
    }

    private void LevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LevelFilter.SelectedItem is ComboBoxItem item)
        {
            // Use Tag property to store enum value (set in XAML)
            _selectedLevel = item.Tag?.ToString(); // null = "All", otherwise enum string like "Debug", "Info", etc.
            _filteredView?.Refresh();
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        var result = WpfMessageBox.Show(
            Core.Localization.LocalizationManager.GetString("Message_ClearLogConfirm"),
            Core.Localization.LocalizationManager.GetString("Dialog_ClearLog"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _allMessages.Clear();
        }
    }

    private void QuickSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var filePath = Core.Configuration.PathHelper.GetSessionLogExportFilePath(
                _configurationManager.AppName + "_UI_Export",
                "txt");
            
            ExportToFile(filePath);
            
            WpfMessageBox.Show(
                $"로그가 저장되었습니다:\n{filePath}",
                "저장 완료",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(
                $"저장 중 오류가 발생했습니다:\n{ex.Message}",
                "저장 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = Core.Configuration.PathHelper.LogsDirectory;
            
            Directory.CreateDirectory(logDir);
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = logDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(
                $"Log Export Complete: {_configurationManager.AppName}",
                "완료",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show(
                $"저장 중 오류가 발생했습니다:\n{ex.Message}",
                "저장 오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    }

    private void ExportToFile(string filePath)
    {
        if (_filteredView == null) return;

        var extension = Path.GetExtension(filePath).ToLower();
        
        using var writer = new StreamWriter(filePath);
        
        if (extension == ".csv")
        {
            // CSV format
            writer.WriteLine("Severity,Timestamp,Source,Message");
            foreach (LogMessage message in _filteredView)
            {
                writer.WriteLine($"\"{message.Severity}\",\"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",\"{message.Source}\",\"{message.Message.Replace("\"", "\"\"")}\"");
            }
        }
        else
        {
            // Plain text format
            writer.WriteLine("ChronoView Log Export");
            writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine(new string('=', 80));
            writer.WriteLine();

            foreach (LogMessage message in _filteredView)
            {
                writer.WriteLine($"[{message.Severity}] {message.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                writer.WriteLine($"Source: {message.Source}");
                writer.WriteLine($"Message: {message.Message}");
                writer.WriteLine(new string('-', 80));
            }
        }
    }

    /// <summary>
    /// Adds a log message to the panel and optionally writes it to a file.
    /// </summary>
    public void AddLogMessage(LogSeverity severity, string source, string message, bool writeToFile = true)
    {
        var logMessage = new LogMessage
        {
            Severity = severity,
            Timestamp = DateTime.Now,
            Source = source,
            Message = message
        };

        Dispatcher.Invoke(() =>
        {
            _allMessages.Add(logMessage);
            
            // Keep only last MaxLogMessages to prevent memory issues
            while (_allMessages.Count > MaxLogMessages)
            {
                _allMessages.RemoveAt(0);
            }
        });

        if (writeToFile)
        {
            WriteToLogFile(logMessage);
        }
    }

    private void WriteToLogFile(LogMessage message)
    {
        try
        {
            var logDir = Core.Configuration.PathHelper.LogsDirectory;

            var logFile = Core.Configuration.PathHelper.GetSessionLogFilePath(_configurationManager.AppName);

            var logEntry = $"[{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{message.Severity}] [{message.Source}] {message.Message}";

            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }
        catch
        {
            // Silently fail if we can't write to log file
        }
    }
}
