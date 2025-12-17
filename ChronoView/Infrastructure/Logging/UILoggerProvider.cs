using Microsoft.Extensions.Logging;
using System;

namespace ChronoView.Infrastructure.Logging
{
    /// <summary>
    /// Logger provider that forwards logs to UI
    /// </summary>
    public class UILoggerProvider : ILoggerProvider
    {
        private readonly Action<LogLevel, string, string> _logAction;

        public UILoggerProvider(Action<LogLevel, string, string> logAction)
        {
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new UILogger(categoryName, _logAction);
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }

    /// <summary>
    /// Logger that forwards to UI log panel
    /// </summary>
    internal class UILogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Action<LogLevel, string, string> _logAction;

        public UILogger(string categoryName, Action<LogLevel, string, string> logAction)
        {
            _categoryName = categoryName;
            _logAction = logAction;
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

            // Extract source from category name (e.g., "ChronoView.Core.FileMatching.FileMatchingEngine" -> "FileMatching")
            var source = ExtractSource(_categoryName);

            _logAction(logLevel, source, message);
        }

        private static string ExtractSource(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return "System";

            // Extract meaningful part from namespace
            // "ChronoView.Core.FileMatching.FileMatchingEngine" -> "FileMatching"
            var parts = categoryName.Split('.');
            if (parts.Length >= 3)
            {
                return parts[^2]; // Second to last part (e.g., "FileMatching")
            }
            else if (parts.Length >= 2)
            {
                return parts[^1]; // Last part
            }
            else
            {
                return categoryName;
            }
        }
    }
}
