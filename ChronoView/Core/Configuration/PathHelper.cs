using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using ChronoView;

namespace ChronoView.Core.Configuration;

/// <summary>
/// Static helper for accessing application paths in contexts where DI is not available (e.g., UserControls).
/// </summary>
public static class PathHelper
{
    private static IConfigurationManager? _configManager;

    private static IConfigurationManager? ConfigManager
    {
        get
        {
            if (_configManager == null)
            {
                if (System.Windows.Application.Current is App app)
                {
                    _configManager = app.Services?.GetService<IConfigurationManager>();
                }
            }
            return _configManager;
        }
    }

    /// <summary>
    /// Logs directory path.
    /// Tries to get from IConfigurationManager, otherwise falls back to calculating it directly.
    /// </summary>
    public static string LogsDirectory
    {
        get
        {
            try
            {
                if (ConfigManager != null)
                {
                    return ConfigManager.LogsDirectory;
                }
            }
            catch
            {
                // Fallback handled below
            }
            return FallbackLogsDirectory;
        }
    }

    /// <summary>
    /// Global session start time for log grouping.
    /// </summary>
    public static DateTime SessionStartTime => App.SessionStartTime;

    /// <summary>
    /// Session log directory (LogsDirectory/yyyyMMdd). Ensures the directory exists.
    /// </summary>
    public static string GetSessionLogDirectory()
    {
        var sessionStart = App.SessionStartTime;
        var dateFolder = Path.Combine(LogsDirectory, sessionStart.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dateFolder);
        return dateFolder;
    }

    /// <summary>
    /// Builds a session log file path under the session directory.
    /// </summary>
    public static string GetSessionLogFilePath(string prefix, string extension = "log")
    {
        var sessionStart = App.SessionStartTime;
        var dateFolder = GetSessionLogDirectory();
        var fileName = $"{prefix}_{sessionStart:yyyyMMdd_HHmmss}.{extension}";
        return Path.Combine(dateFolder, fileName);
    }

    /// <summary>
    /// Builds a session export log file path under the session directory.
    /// Uses session date with current time for uniqueness.
    /// </summary>
    public static string GetSessionLogExportFilePath(string prefix, string extension)
    {
        var sessionStart = App.SessionStartTime;
        var now = DateTime.Now;
        var timestamp = new DateTime(
            sessionStart.Year,
            sessionStart.Month,
            sessionStart.Day,
            now.Hour,
            now.Minute,
            now.Second,
            now.Millisecond);
        var dateFolder = GetSessionLogDirectory();
        var fileName = $"{prefix}_{timestamp:yyyyMMdd_HHmmss_fff}.{extension}";
        return Path.Combine(dateFolder, fileName);
    }

    /// <summary>
    /// Abnormal history file path.
    /// Tries to get from IConfigurationManager, otherwise falls back to calculating it directly.
    /// </summary>
    public static string HistoryFilePath
    {
        get
        {
            try
            {
                if (ConfigManager != null)
                {
                    return ConfigManager.HistoryFilePath;
                }
            }
            catch
            {
                // Fallback handled below
            }
            return Path.Combine(FallbackAppDataDirectory, "abnormal_history.json");
        }
    }

    // Fallback logic matches expected behavior: %LOCALAPPDATA%\prische\ChronoView\Logs
    private static string FallbackLogsDirectory => Path.Combine(FallbackAppDataDirectory, "Logs");

    private static string FallbackAppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "prische",
        "ChronoView");
}
