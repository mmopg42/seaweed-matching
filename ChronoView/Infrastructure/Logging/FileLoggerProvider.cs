using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ChronoView.Infrastructure.Logging
{
    /// <summary>
    /// Logger provider that writes logs to a file
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;

        public FileLoggerProvider(string logFilePath)
        {
            _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logFilePath);
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }

    /// <summary>
    /// Logger that writes to a file
    /// </summary>
    internal class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private static readonly object _fileLock = new object();

        public FileLogger(string categoryName, string logFilePath)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                lock (_fileLock)
                {
                    // Ensure the directory exists (safety measure)
                    var logDir = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var level = logLevel.ToString().ToUpper().PadRight(5);
                    var category = _categoryName;
                    
                    var logEntry = $"[{timestamp}] [{level}] [{category}] {message}";
                    
                    if (exception != null)
                    {
                        logEntry += $"{Environment.NewLine}{exception}";
                    }
                    
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if we can't write to log file
            }
        }
    }
}

