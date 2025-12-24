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
    private string? _selectedLevel = null; // null = "All", otherwise LogSeverity enum string

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

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new WpfSaveFileDialog
        {
            Filter = Core.Localization.LocalizationManager.GetString("Filter_TextFiles"),
            DefaultExt = ".txt",
            FileName = $"ChronoView_Log_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                ExportToFile(saveDialog.FileName);
                WpfMessageBox.Show(
                    Core.Localization.LocalizationManager.GetString("Message_ExportSuccess", saveDialog.FileName),
                    Core.Localization.LocalizationManager.GetString("Dialog_ExportComplete"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    Core.Localization.LocalizationManager.GetString("Message_ExportError", ex.Message),
                    Core.Localization.LocalizationManager.GetString("Dialog_ExportError"),
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
