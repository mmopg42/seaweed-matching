using System.Text.Json;
using static UiAutomation.Commands.ExitCodes;

namespace UiAutomation.Commands;

/// <summary>
/// Centralized JSON response helper for all CLI commands.
/// Provides standardized output formatting with retryable and suggestion fields.
/// </summary>
public static class JsonResponseHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Print successful response with data payload
    /// </summary>
    public static void PrintSuccess<T>(T data, string? timestamp = null)
    {
        var response = new JsonResponseModels.SuccessResponse<T>(
            Success: true,
            Data: data,
            Timestamp: timestamp ?? DateTime.UtcNow.ToString("o")
        );
        Console.WriteLine(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>
    /// Print error response with retryable and suggestion fields
    /// </summary>
    /// <param name="error">Human-readable error message</param>
    /// <param name="errorCode">Exit code from ExitCodes constants</param>
    /// <param name="suggestion">Optional suggestion for recovery (omit if not applicable)</param>
    /// <param name="timestamp">Optional timestamp (defaults to current UTC time)</param>
    public static void PrintError(
        string error,
        int errorCode = ERROR,
        string? suggestion = null,
        string? timestamp = null)
    {
        var response = new JsonResponseModels.ErrorResponse(
            Success: false,
            Error: error,
            ErrorCode: errorCode,
            Retryable: IsRetryable(errorCode),
            Suggestion: suggestion,
            Timestamp: timestamp ?? DateTime.UtcNow.ToString("o")
        );
        Console.WriteLine(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>
    /// Print error response with automatically-generated suggestion based on error type
    /// </summary>
    public static void PrintErrorWithAutoSuggestion(
        string error,
        int errorCode,
        string commandContext = "")
    {
        string? suggestion = errorCode switch
        {
            NOT_FOUND when commandContext.Contains("MainWindow") =>
                "Ensure ChronoView is running. Try 'app status --json' to check.",
            NOT_FOUND when commandContext.Contains("SetupWindow") =>
                "SetupWindow may have already been completed. Try 'windows main --json'.",
            NOT_FOUND when commandContext.Contains("SettingsDialog") =>
                "SettingsDialog is not open. Try 'settings-dialog open' first.",
            NOT_FOUND when commandContext.Contains("DataGrid") =>
                "DataGrid not available. Ensure monitoring has started and data exists.",
            TIMEOUT =>
                "Operation timed out. The UI may be busy. Consider retrying after a short delay.",
            ERROR when commandContext.Contains("launch") =>
                "Launch failed. Check if ChronoView is already running or if build is required.",
            _ => null
        };

        PrintError(error, errorCode, suggestion);
    }

    /// <summary>
    /// Print empty success response (for commands with no meaningful return data)
    /// </summary>
    public static void PrintEmptySuccess()
    {
        var response = new JsonResponseModels.EmptySuccess(
            Success: true,
            Timestamp: DateTime.UtcNow.ToString("o")
        );
        Console.WriteLine(JsonSerializer.Serialize(response, JsonOptions));
    }
}
