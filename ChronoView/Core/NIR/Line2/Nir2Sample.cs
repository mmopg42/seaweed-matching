using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Represents a single NIR2 data sample from the API.
/// Contains sensor readings including protein, moisture, and presence detection.
/// </summary>
public class Nir2Sample
{
    /// <summary>
    /// Timestamp when the sample was collected.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Protein content percentage.
    /// </summary>
    [JsonPropertyName("protein")]
    public double Protein { get; set; }

    /// <summary>
    /// Moisture content percentage.
    /// </summary>
    [JsonPropertyName("moisture")]
    public double Moisture { get; set; }

    /// <summary>
    /// Presence detection flag.
    /// 2 = No object present
    /// 1 = Object present (sample valid)
    /// </summary>
    [JsonPropertyName("presence")]
    public int Presence { get; set; }

    /// <summary>
    /// Indicates whether this sample is valid (object present).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => Presence == 1;

    /// <summary>
    /// Validates the sample data integrity.
    /// </summary>
    public bool HasValidData()
    {
        return IsValid &&
               Protein >= 0 &&
               Moisture >= 0 &&
               !double.IsNaN(Protein) &&
               !double.IsNaN(Moisture) &&
               !double.IsInfinity(Protein) &&
               !double.IsInfinity(Moisture);
    }

    /// <summary>
    /// Creates a copy of this sample.
    /// </summary>
    public Nir2Sample Clone()
    {
        return new Nir2Sample
        {
            Timestamp = Timestamp,
            Protein = Protein,
            Moisture = Moisture,
            Presence = Presence
        };
    }

    /// <summary>
    /// Creates a Nir2Sample from the raw NIR2 API response.
    /// </summary>
    /// <param name="response">The API response from the NIR2 device.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>A Nir2Sample if parsing succeeds, null otherwise.</returns>
    public static Nir2Sample? FromApiResponse(Nir2ApiResponse response, ILogger? logger = null)
    {
        if (response?.Batch == null || response.Parameters == null)
        {
            logger?.LogWarning("NIR2 API response is null or missing Batch/Parameters");
            return null;
        }

        // Parse timestamp from Batch.End (strip timezone, parse datetime only)
        // Input format: "2026-01-29T15:55:40.5394+0900" or similar
        // We only need: "2026-01-29T15:55:40.5394"
        var timestampStr = response.Batch.End;
        DateTime timestamp = DateTime.MinValue; // Default if parsing fails

        if (!string.IsNullOrEmpty(timestampStr))
        {
            // Find timezone separator (+ or -) and take everything before it
            int tzIndex = timestampStr.LastIndexOfAny(new[] { '+', '-' });

            if (tzIndex > 0) // Found timezone, strip it
            {
                var datetimeOnly = timestampStr.Substring(0, tzIndex);
                if (DateTime.TryParse(datetimeOnly, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }
                else
                {
                    logger?.LogWarning("Failed to parse NIR2 timestamp: {Timestamp}", timestampStr);
                }
            }
            else // No timezone, try parsing as-is
            {
                if (DateTime.TryParse(timestampStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedTimestamp))
                {
                    timestamp = parsedTimestamp;
                }
                else
                {
                    logger?.LogWarning("Failed to parse NIR2 timestamp: {Timestamp}", timestampStr);
                }
            }
        }

        // Extract parameter values by name
        double protein = 0;
        double moisture = 0;
        double presenceRaw = 0;

        foreach (var param in response.Parameters)
        {
            if (string.IsNullOrEmpty(param.Name)) continue;

            switch (param.Name)
            {
                case "Protein":
                    protein = param.Value;
                    break;
                case "Moisture":
                    moisture = param.Value;
                    break;
                case "Presence":
                    presenceRaw = param.Value;
                    break;
            }
        }

        // Round presence to nearest integer, using AwayFromZero to ensure 0.5 rounds to 1
        int presence = (int)Math.Round(presenceRaw, MidpointRounding.AwayFromZero);

        // Ensure presence is only 1 or 2 (clamp: if < 1, set to 1; if > 2, set to 2)
        if (presence < 1) presence = 1;
        if (presence > 2) presence = 2;

        return new Nir2Sample
        {
            Timestamp = timestamp,
            Protein = protein,
            Moisture = moisture,
            Presence = presence
        };
    }
}
