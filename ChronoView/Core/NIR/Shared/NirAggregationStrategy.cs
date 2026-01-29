namespace ChronoView.Core.NIR.Shared;

/// <summary>
/// Aggregation strategies for NIR2 chunk data processing.
/// Determines which sample's values and timestamp to use when a chunk completes.
/// </summary>
public enum NirAggregationStrategy
{
    /// <summary>
    /// Use the first sample's values and timestamp.
    /// Earliest data point in the chunk.
    /// </summary>
    First,

    /// <summary>
    /// Use the second sample's values and timestamp.
    /// Useful for skipping initial noise.
    /// </summary>
    Second,

    /// <summary>
    /// Use the median sample's values and timestamp.
    /// Middle value when sorted by time; robust against outliers.
    /// </summary>
    Median,

    /// <summary>
    /// Use the last sample's values and timestamp.
    /// Latest data point in the chunk.
    /// </summary>
    Last,

    /// <summary>
    /// Use the mean (average) of all sample values, and mean timestamp.
    /// Smooths out variations across the chunk.
    /// </summary>
    Mean
}
