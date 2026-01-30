using System.IO;
using System.Text.Json.Serialization;
using ChronoView.Core.NIR.Shared;

namespace ChronoView.Core.Configuration;

/// <summary>
/// Runtime settings for NIR2 data collection via API.
/// Separate from ExternalProgramSettings which only handles program execution paths.
/// </summary>
public class Nir2Settings
{
    /// <summary>
    /// API URL for NIR2 data collection.
    /// Default: "http://127.0.0.1:10024/1"
    /// </summary>
    [JsonPropertyName("apiUrl")]
    public string ApiUrl { get; set; } = "http://127.0.0.1:10024/1";

    /// <summary>
    /// Directory path for CSV file storage.
    /// Default: "%APPDATA%\ChronoView\NIR2\"
    /// </summary>
    [JsonPropertyName("csvDirectory")]
    public string CsvDirectory { get; set; } = "";

    /// <summary>
    /// Gets the full CSV directory path, expanding environment variables.
    /// Returns empty string if CsvDirectory is not configured.
    /// </summary>
    [JsonIgnore]
    public string FullCsvDirectory =>
        string.IsNullOrEmpty(CsvDirectory)
            ? ""
            : Environment.ExpandEnvironmentVariables(CsvDirectory);

    /// <summary>
    /// API polling interval in milliseconds.
    /// Default: 100ms
    /// </summary>
    [JsonPropertyName("pollingInterval")]
    public int PollingInterval { get; set; } = 100;

    /// <summary>
    /// Aggregation strategy for computing chunk values.
    /// Default: First
    /// </summary>
    [JsonPropertyName("aggregationStrategy")]
    public NirAggregationStrategy AggregationStrategy { get; set; } = NirAggregationStrategy.First;

    /// <summary>
    /// HTTP request timeout in seconds.
    /// Default: 5 seconds
    /// </summary>
    [JsonPropertyName("httpTimeout")]
    public int HttpTimeout { get; set; } = 5;

    /// <summary>
    /// Maximum number of retry attempts for failed API requests.
    /// Default: 3
    /// </summary>
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable NIR2 data collection.
    /// Default: true
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Validates the settings.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ApiUrl) &&
               PollingInterval > 0 &&
               HttpTimeout > 0 &&
               MaxRetries >= 0;
    }

    /// <summary>
    /// Creates a copy of this settings instance.
    /// </summary>
    public Nir2Settings Clone()
    {
        return new Nir2Settings
        {
            ApiUrl = ApiUrl,
            CsvDirectory = CsvDirectory,
            PollingInterval = PollingInterval,
            AggregationStrategy = AggregationStrategy,
            HttpTimeout = HttpTimeout,
            MaxRetries = MaxRetries,
            IsEnabled = IsEnabled
        };
    }
}
