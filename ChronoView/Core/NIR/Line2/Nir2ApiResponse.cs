using System.Text.Json.Serialization;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// DTO for NIR2 device HTTP API response.
/// Matches the actual JSON structure returned by the NIR2 device.
/// </summary>
public class Nir2ApiResponse
{
    /// <summary>
    /// Batch information containing timestamps.
    /// </summary>
    [JsonPropertyName("Batch")]
    public Nir2Batch? Batch { get; set; }

    /// <summary>
    /// Array of measured parameters (Protein, Moisture, Presence, etc.).
    /// </summary>
    [JsonPropertyName("Parameters")]
    public List<Nir2Parameter>? Parameters { get; set; }
}

/// <summary>
/// Batch information from NIR2 device.
/// </summary>
public class Nir2Batch
{
    /// <summary>
    /// End timestamp of the batch measurement.
    /// Format: "YYYY-MM-DDTHH:MM:SS.ffff+TZ" (e.g., "2026-01-29T15:55:40.5394+900")
    /// </summary>
    [JsonPropertyName("End")]
    public string? End { get; set; }
}

/// <summary>
/// Single parameter measurement from NIR2 device.
/// </summary>
public class Nir2Parameter
{
    /// <summary>
    /// Parameter name (e.g., "Protein", "Moisture", "Presence").
    /// </summary>
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    /// <summary>
    /// Parameter value.
    /// </summary>
    [JsonPropertyName("Value")]
    public double Value { get; set; }
}
