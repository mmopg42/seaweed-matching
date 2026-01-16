namespace ChronoView.UI.ViewModels;

/// <summary>
/// Represents a log message for display in the UI.
/// </summary>
public class LogMessage
{
    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Severity level of the message.
    /// </summary>
    public LogSeverity Severity { get; set; } = LogSeverity.Info;

    /// <summary>
    /// Event type string for display (Info, Warning, Error).
    /// </summary>
    public string EventType => Severity.ToString();

    /// <summary>
    /// Source of the log message (e.g., "System", "GroupManager", "AbnormalDetect").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The log message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Description property for DataGrid binding (alias for Message).
    /// </summary>
    public string Description => Message;

    /// <summary>
    /// Production line number for filtering (1=Line1, 2=Line2, null=System/Common).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Creates a new log message.
    /// </summary>
    public LogMessage()
    {
    }

    /// <summary>
    /// Creates a new log message with specified parameters.
    /// </summary>
    public LogMessage(LogSeverity severity, string source, string message)
    {
        Severity = severity;
        Source = source;
        Message = message;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// Severity levels for log messages.
/// </summary>
public enum LogSeverity
{
    /// <summary>
    /// Debug message.
    /// </summary>
    Debug,

    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}
