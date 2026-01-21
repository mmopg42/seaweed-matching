namespace UiAutomation.Commands;

/// <summary>
/// Centralized exit code constants for ChronoView UI Automation CLI.
/// All command handlers should return these values instead of calling Environment.Exit().
/// </summary>
public static class ExitCodes
{
    /// <summary>Command executed successfully</summary>
    public const int SUCCESS = 0;

    /// <summary>General error (exception, automation failure, etc.)</summary>
    public const int ERROR = 1;

    /// <summary>Resource not found (window, file, element, etc.)</summary>
    public const int NOT_FOUND = 2;

    /// <summary>Operation timed out</summary>
    public const int TIMEOUT = 3;

    /// <summary>Invalid command-line arguments</summary>
    public const int INVALID_ARGUMENT = 4;

    /// <summary>
    /// Determines if an exit code indicates a retryable error.
    /// Non-retryable errors include: invalid arguments, resource not found (usually permanent).
    /// </summary>
    /// <param name="exitCode">The exit code to check</param>
    /// <returns>True if the operation can be retried, false otherwise</returns>
    public static bool IsRetryable(int exitCode)
    {
        return exitCode switch
        {
            SUCCESS => false,           // Already succeeded
            ERROR => true,              // General errors may be transient (timeout, UI lag)
            NOT_FOUND => false,         // Resource not found is usually permanent
            TIMEOUT => true,            // Timeouts are often transient
            INVALID_ARGUMENT => false,  // Invalid arguments won't work on retry
            _ => false                  // Unknown codes - assume non-retryable for safety
        };
    }

    /// <summary>
    /// Gets a human-readable error name for an exit code.
    /// </summary>
    public static string GetErrorName(int exitCode)
    {
        return exitCode switch
        {
            SUCCESS => "SUCCESS",
            ERROR => "ERROR",
            NOT_FOUND => "NOT_FOUND",
            TIMEOUT => "TIMEOUT",
            INVALID_ARGUMENT => "INVALID_ARGUMENT",
            _ => "UNKNOWN"
        };
    }
}
