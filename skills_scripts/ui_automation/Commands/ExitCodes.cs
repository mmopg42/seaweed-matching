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
}
