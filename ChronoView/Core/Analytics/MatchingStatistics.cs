namespace ChronoView.Core.Analytics;

/// <summary>
/// Represents matching statistics for file groups.
/// </summary>
public class MatchingStatistics
{
    public int TotalGroups { get; set; }
    public int WithNir { get; set; }
    public int WithoutNir { get; set; }
    public int Failed { get; set; }
    
    /// <summary>
    /// Gets the match rate as a percentage (0-100).
    /// </summary>


    /// <summary>
    /// Statistics for Line 1 (separated mode).
    /// </summary>
    public LineStatistics? Line1 { get; set; }

    /// <summary>
    /// Statistics for Line 2 (separated mode).
    /// </summary>
    public LineStatistics? Line2 { get; set; }

    public MatchingStatistics()
    {
    }

    public MatchingStatistics(int totalGroups, int withNir, int withoutNir, int failed)
    {
        TotalGroups = totalGroups;
        WithNir = withNir;
        WithoutNir = withoutNir;
        Failed = failed;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MatchingStatistics other) return false;
        
        return TotalGroups == other.TotalGroups &&
               WithNir == other.WithNir &&
               WithoutNir == other.WithoutNir &&
               Failed == other.Failed;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TotalGroups, WithNir, WithoutNir, Failed);
    }

    public override string ToString()
    {
        return $"Total: {TotalGroups}, With NIR: {WithNir}, Without NIR: {WithoutNir}, Failed: {Failed}";
    }
}

/// <summary>
/// Represents matching statistics for a single line (separated mode).
/// </summary>
public class LineStatistics
{
    public int TotalGroups { get; set; }
    public int WithNir { get; set; }
    public int WithoutNir { get; set; }
    public int Failed { get; set; }
    
    /// <summary>
    /// Gets the match rate as a percentage (0-100).
    /// </summary>


    public LineStatistics()
    {
    }

    public LineStatistics(int totalGroups, int withNir, int withoutNir, int failed)
    {
        TotalGroups = totalGroups;
        WithNir = withNir;
        WithoutNir = withoutNir;
        Failed = failed;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not LineStatistics other) return false;
        
        return TotalGroups == other.TotalGroups &&
               WithNir == other.WithNir &&
               WithoutNir == other.WithoutNir &&
               Failed == other.Failed;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TotalGroups, WithNir, WithoutNir, Failed);
    }

    public override string ToString()
    {
        return $"Total: {TotalGroups}, With NIR: {WithNir}, Without NIR: {WithoutNir}, Failed: {Failed}";
    }
}
