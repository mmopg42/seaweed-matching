namespace UiAutomation.Commands;

/// <summary>
/// Standardized JSON response models for ChronoView CLI.
/// All commands return consistent response format for reliable parsing.
/// Property names are converted to camelCase via JsonNamingPolicy.CamelCase in JsonResponseHelper.
/// </summary>
public static class JsonResponseModels
{
    /// <summary>
    /// Standard successful response with data payload
    /// </summary>
    public record SuccessResponse<T>(
        bool Success,
        T Data,
        string Timestamp
    );

    /// <summary>
    /// Standard error response with optional retryable hint and suggestion
    /// </summary>
    public record ErrorResponse(
        bool Success,
        string Error,
        int ErrorCode,
        bool Retryable,
        string? Suggestion = null,
        string? Timestamp = null
    );

    /// <summary>
    /// Response that may be success or error (for commands with both outcomes)
    /// </summary>
    public record Response<T>(
        bool Success,
        T? Data,
        string? Error = null,
        int? ErrorCode = null,
        bool? Retryable = null,
        string? Suggestion = null
    );

    /// <summary>
    /// Empty success response (for commands with no meaningful return data)
    /// </summary>
    public record EmptySuccess(
        bool Success,
        string Timestamp
    );
}
