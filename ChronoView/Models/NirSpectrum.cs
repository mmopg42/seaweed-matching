using System.Text.Json.Serialization;

namespace ChronoView.Models;

/// <summary>
/// Represents Near-Infrared (NIR) spectrum data extracted from .spc files.
/// </summary>
public class NirSpectrum : IEquatable<NirSpectrum>
{
    /// <summary>
    /// Name of the spectrum file.
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the spectrum was captured.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Array of wavelength values (X-axis data).
    /// </summary>
    [JsonPropertyName("wavelengths")]
    public double[] Wavelengths { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Array of intensity values (Y-axis data).
    /// </summary>
    [JsonPropertyName("intensities")]
    public double[] Intensities { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Additional metadata extracted from the spectrum file.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Full file path to the spectrum file.
    /// </summary>
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this spectrum has been flagged as abnormal.
    /// </summary>
    [JsonPropertyName("is_abnormal")]
    public bool IsAbnormal { get; set; }

    /// <summary>
    /// Y-axis variation detected in the spectrum (for quality assessment).
    /// </summary>
    [JsonPropertyName("y_variation")]
    public double YVariation { get; set; }

    /// <summary>
    /// Validates the integrity of this NIR spectrum.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(FileName))
            return false;

        if (string.IsNullOrWhiteSpace(FilePath))
            return false;

        if (Wavelengths.Length == 0 || Intensities.Length == 0)
            return false;

        if (Wavelengths.Length != Intensities.Length)
            return false;

        // Check for invalid values
        if (Wavelengths.Any(w => double.IsNaN(w) || double.IsInfinity(w)))
            return false;

        if (Intensities.Any(i => double.IsNaN(i) || double.IsInfinity(i)))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the number of data points in the spectrum.
    /// </summary>
    [JsonIgnore]
    public int DataPointCount => Math.Min(Wavelengths.Length, Intensities.Length);

    /// <summary>
    /// Gets the wavelength range of the spectrum.
    /// </summary>
    [JsonIgnore]
    public (double Min, double Max) WavelengthRange
    {
        get
        {
            if (Wavelengths.Length == 0)
                return (0, 0);
            return (Wavelengths.Min(), Wavelengths.Max());
        }
    }

    /// <summary>
    /// Gets the intensity range of the spectrum.
    /// </summary>
    [JsonIgnore]
    public (double Min, double Max) IntensityRange
    {
        get
        {
            if (Intensities.Length == 0)
                return (0, 0);
            return (Intensities.Min(), Intensities.Max());
        }
    }

    #region Equality Implementation

    public bool Equals(NirSpectrum? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return FileName == other.FileName &&
               Timestamp.Equals(other.Timestamp) &&
               FilePath == other.FilePath &&
               IsAbnormal == other.IsAbnormal &&
               Math.Abs(YVariation - other.YVariation) < 0.0001 &&
               Wavelengths.SequenceEqual(other.Wavelengths) &&
               Intensities.SequenceEqual(other.Intensities);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as NirSpectrum);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FileName);
        hash.Add(Timestamp);
        hash.Add(FilePath);
        hash.Add(IsAbnormal);
        hash.Add(YVariation);
        
        // Hash arrays in a deterministic way
        foreach (var w in Wavelengths)
            hash.Add(w);
        foreach (var i in Intensities)
            hash.Add(i);
        
        return hash.ToHashCode();
    }

    public static bool operator ==(NirSpectrum? left, NirSpectrum? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NirSpectrum? left, NirSpectrum? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
