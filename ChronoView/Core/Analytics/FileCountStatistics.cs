namespace ChronoView.Core.Analytics;

/// <summary>
/// Represents real-time file count statistics for all monitored folders.
/// </summary>
public class FileCountStatistics
{
    public int NirCount { get; set; }
    public int Nir2Count { get; set; }
    public int NormalCount { get; set; }
    public int Normal2Count { get; set; }
    public int Cam1Count { get; set; }
    public int Cam2Count { get; set; }
    public int Cam3Count { get; set; }
    public int Cam4Count { get; set; }
    public int Cam5Count { get; set; }
    public int Cam6Count { get; set; }
    public DateTime LastUpdated { get; set; }

    public FileCountStatistics()
    {
        LastUpdated = DateTime.Now;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FileCountStatistics other) return false;
        
        return NirCount == other.NirCount &&
               Nir2Count == other.Nir2Count &&
               NormalCount == other.NormalCount &&
               Normal2Count == other.Normal2Count &&
               Cam1Count == other.Cam1Count &&
               Cam2Count == other.Cam2Count &&
               Cam3Count == other.Cam3Count &&
               Cam4Count == other.Cam4Count &&
               Cam5Count == other.Cam5Count &&
               Cam6Count == other.Cam6Count;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(NirCount);
        hash.Add(Nir2Count);
        hash.Add(NormalCount);
        hash.Add(Normal2Count);
        hash.Add(Cam1Count);
        hash.Add(Cam2Count);
        hash.Add(Cam3Count);
        hash.Add(Cam4Count);
        hash.Add(Cam5Count);
        hash.Add(Cam6Count);
        return hash.ToHashCode();
    }
}
