using System.Text.Json.Serialization;

namespace ChronoView.Models;

/// <summary>
/// Represents files that have not yet been matched into file groups.
/// </summary>
public class UnmatchedFiles : IEquatable<UnmatchedFiles>
{
    /// <summary>
    /// NIR files awaiting matching, organized by line (nir, nir2), then keyed by NIR identifier.
    /// </summary>
    [JsonPropertyName("nir_files")]
    public Dictionary<string, Dictionary<string, string>> NirFiles { get; set; } = new();

    /// <summary>
    /// Normal folders awaiting matching, organized by line (normal, normal2), then keyed by folder identifier.
    /// </summary>
    [JsonPropertyName("normal_folders")]
    public Dictionary<string, Dictionary<string, string>> NormalFolders { get; set; } = new();

    /// <summary>
    /// Camera files awaiting matching, keyed by camera identifier, with lists of timestamped files.
    /// </summary>
    [JsonPropertyName("camera_files")]
    public Dictionary<string, List<TimestampedFile>> CameraFiles { get; set; } = new();

    /// <summary>
    /// Gets the total count of unmatched files across all categories.
    /// </summary>
    [JsonIgnore]
    public int TotalCount => 
        NirFiles.Values.Sum(dict => dict.Count) + 
        NormalFolders.Values.Sum(dict => dict.Count) + 
        CameraFiles.Values.Sum(list => list.Count);

    /// <summary>
    /// Validates the integrity of the unmatched files collection.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        // Check for null values in NIR files dictionaries
        foreach (var lineDict in NirFiles.Values)
        {
            if (lineDict.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value)))
                return false;
        }

        // Check for null values in normal folders dictionaries
        foreach (var lineDict in NormalFolders.Values)
        {
            if (lineDict.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value)))
                return false;
        }

        if (CameraFiles.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null))
            return false;

        // Validate timestamped files
        foreach (var fileList in CameraFiles.Values)
        {
            if (fileList.Any(f => !f.IsValid()))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Clears all unmatched files.
    /// </summary>
    public void Clear()
    {
        NirFiles.Clear();
        NormalFolders.Clear();
        CameraFiles.Clear();
    }

    #region Equality Implementation

    public bool Equals(UnmatchedFiles? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return DictionariesEqual(NirFiles, other.NirFiles) &&
               DictionariesEqual(NormalFolders, other.NormalFolders) &&
               CameraFilesDictionariesEqual(CameraFiles, other.CameraFiles);
    }

    private static bool DictionariesEqual(
        Dictionary<string, Dictionary<string, string>> dict1, 
        Dictionary<string, Dictionary<string, string>> dict2)
    {
        if (dict1.Count != dict2.Count) return false;
        
        foreach (var kvp in dict1)
        {
            if (!dict2.TryGetValue(kvp.Key, out var otherDict))
                return false;
            
            if (kvp.Value.Count != otherDict.Count)
                return false;
            
            if (!kvp.Value.All(innerKvp => otherDict.TryGetValue(innerKvp.Key, out var value) && value == innerKvp.Value))
                return false;
        }
        
        return true;
    }

    private static bool CameraFilesDictionariesEqual(
        Dictionary<string, List<TimestampedFile>> dict1,
        Dictionary<string, List<TimestampedFile>> dict2)
    {
        if (dict1.Count != dict2.Count) return false;

        foreach (var kvp in dict1)
        {
            if (!dict2.TryGetValue(kvp.Key, out var otherList))
                return false;

            if (kvp.Value.Count != otherList.Count)
                return false;

            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (!kvp.Value[i].Equals(otherList[i]))
                    return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as UnmatchedFiles);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var lineKvp in NirFiles.OrderBy(x => x.Key))
        {
            hash.Add(lineKvp.Key);
            foreach (var kvp in lineKvp.Value.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }
        }

        foreach (var lineKvp in NormalFolders.OrderBy(x => x.Key))
        {
            hash.Add(lineKvp.Key);
            foreach (var kvp in lineKvp.Value.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }
        }

        foreach (var kvp in CameraFiles.OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            foreach (var file in kvp.Value)
            {
                hash.Add(file);
            }
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(UnmatchedFiles? left, UnmatchedFiles? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(UnmatchedFiles? left, UnmatchedFiles? right)
    {
        return !Equals(left, right);
    }

    #endregion
}

/// <summary>
/// Represents a file with an associated timestamp for matching purposes.
/// </summary>
public class TimestampedFile : IEquatable<TimestampedFile>
{
    /// <summary>
    /// File name without path.
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full absolute path to the file.
    /// </summary>
    [JsonPropertyName("absolute_path")]
    public string AbsolutePath { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the file (alias for AbsolutePath for compatibility).
    /// </summary>
    [JsonIgnore]
    public string FilePath
    {
        get => AbsolutePath;
        set => AbsolutePath = value;
    }

    /// <summary>
    /// Timestamp associated with this file (typically creation or modification time).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional metadata associated with the file.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Validates the integrity of this timestamped file.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FileName) && !string.IsNullOrWhiteSpace(AbsolutePath);
    }

    #region Equality Implementation

    public bool Equals(TimestampedFile? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return FileName == other.FileName &&
               AbsolutePath == other.AbsolutePath &&
               Timestamp.Equals(other.Timestamp);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TimestampedFile);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, AbsolutePath, Timestamp);
    }

    public static bool operator ==(TimestampedFile? left, TimestampedFile? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TimestampedFile? left, TimestampedFile? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
