using System.Text.Json;

namespace UiAutomation.Commands;

/// <summary>
/// Standardized JSON response models for ChronoView CLI.
/// All commands return consistent response format for reliable parsing.
/// </summary>
public static class JsonResponseModels
{
    /// <summary>
    /// Standard successful response with data payload
    /// </summary>
    public record SuccessResponse<T>(
        bool Success,
        T Data,
        [property: JsonPropertyName("timestamp")] string Timestamp
    );

    /// <summary>
    /// Standard error response with optional retryable hint and suggestion
    /// </summary>
    public record ErrorResponse(
        bool Success,
        string Error,
        [property: JsonPropertyName("errorCode")] int ErrorCode,
        [property: JsonPropertyName("retryable")] bool Retryable,
        [property: JsonPropertyName("suggestion")] string? Suggestion = null,
        [property: JsonPropertyName("timestamp")] string? Timestamp = null
    );

    /// <summary>
    /// Response that may be success or error (for commands with both outcomes)
    /// </summary>
    public record Response<T>(
        bool Success,
        T? Data,
        [property: JsonPropertyName("error")] string? Error = null,
        [property: JsonPropertyName("errorCode")] int? ErrorCode = null,
        [property: JsonPropertyName("retryable")] bool? Retryable = null,
        [property: JsonPropertyName("suggestion")] string? Suggestion = null
    );

    /// <summary>
    /// Empty success response (for commands with no meaningful return data)
    /// </summary>
    public record EmptySuccess(
        bool Success,
        [property: JsonPropertyName("timestamp")] string Timestamp
    );
}
