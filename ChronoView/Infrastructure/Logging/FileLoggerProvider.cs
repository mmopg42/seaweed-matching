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
                    // 전달받은 경로에서 기본 로그 디렉토리 추출
                    // 예: "C:\Users\...\AppData\ChronoView\Logs\20250115\ChronoView_Debug_20250115.log"
                    //     -> "C:\Users\...\AppData\ChronoView\Logs"
                    var baseLogDir = Path.GetDirectoryName(Path.GetDirectoryName(_logFilePath));
                    var fileName = Path.GetFileName(_logFilePath);
                    
                    // 파일명에서 기본 이름 추출 (ChronoView_Debug)
                    var baseName = fileName.Substring(0, fileName.LastIndexOf('_'));
                    
                    // 오늘 날짜 폴더 경로 생성
                    var today = DateTime.Now.ToString("yyyyMMdd");
                    var todayDateFolder = Path.Combine(baseLogDir ?? "", today);
                    
                    // 오늘 날짜 폴더가 없으면 생성 (날짜 변경 시 자동 대응)
                    Directory.CreateDirectory(todayDateFolder);
                    
                    // 오늘 날짜의 로그 파일 경로 생성
                    var todayLogFile = Path.Combine(todayDateFolder, $"{baseName}_{today}.log");
                    
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var level = logLevel.ToString().ToUpper().PadRight(5);
                    var category = _categoryName;
                    
                    var logEntry = $"[{timestamp}] [{level}] [{category}] {message}";
                    
                    if (exception != null)
                    {
                        logEntry += $"{Environment.NewLine}{exception}";
                    }
                    
                    File.AppendAllText(todayLogFile, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if we can't write to log file
            }
        }
    }
}

