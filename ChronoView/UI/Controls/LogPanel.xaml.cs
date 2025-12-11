using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    private ObservableCollection<LogMessage> _allMessages = new();
    private ICollectionView? _filteredView;
    private string _searchText = string.Empty;
    private string _selectedLevel = "All";

    public LogPanel()
    {
        InitializeComponent();
        InitializeLogView();
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

    private static void OnLogMessagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LogPanel panel)
        {
            panel._allMessages = e.NewValue as ObservableCollection<LogMessage> ?? new();
            panel.InitializeLogView();
            
            // Subscribe to collection changes for auto-scroll
            if (panel._allMessages != null)
            {
                panel._allMessages.CollectionChanged += (s, args) =>
                {
                    if (panel.AutoScrollCheckBox.IsChecked == true && panel.LogDataGrid.Items.Count > 0)
                    {
                        panel.LogDataGrid.ScrollIntoView(panel.LogDataGrid.Items[^1]);
                    }
                };
            }
        }
    }

    private void InitializeLogView()
    {
        _filteredView = CollectionViewSource.GetDefaultView(_allMessages);
        _filteredView.Filter = FilterLogMessage;
        LogDataGrid.ItemsSource = _filteredView;
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
        if (_selectedLevel != "All")
        {
            if (message.Severity.ToString() != _selectedLevel)
            {
                return false;
            }
        }

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
            _selectedLevel = item.Content.ToString() ?? "All";
            _filteredView?.Refresh();
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        var result = WpfMessageBox.Show(
            "Are you sure you want to clear all log messages?",
            "Clear Log",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _allMessages.Clear();
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new WpfSaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"ChronoView_Log_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                ExportToFile(saveDialog.FileName);
                WpfMessageBox.Show(
                    $"Log exported successfully to:\n{saveDialog.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"Failed to export log:\n{ex.Message}",
                    "Export Error",
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
            
            // Keep only last 1000 messages to prevent memory issues
            while (_allMessages.Count > 1000)
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
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChronoView",
                "Logs");

            Directory.CreateDirectory(logDir);

            var logFile = Path.Combine(logDir, $"ChronoView_{DateTime.Now:yyyyMMdd}.log");

            var logEntry = $"[{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{message.Severity}] [{message.Source}] {message.Message}";

            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }
        catch
        {
            // Silently fail if we can't write to log file
        }
    }
}
