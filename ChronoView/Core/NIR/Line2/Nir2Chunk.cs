using ChronoView.Core.NIR.Shared;

namespace ChronoView.Core.NIR.Line2;

/// <summary>
/// Represents a chunk of NIR2 samples collected during a single detection event.
/// A chunk starts when Presence transitions from 2 to 1, and ends when Presence returns to 2.
/// </summary>
public class Nir2Chunk
{
    /// <summary>
    /// Unique identifier for this chunk.
    /// </summary>
    public string ChunkId { get; set; } = string.Empty;

    /// <summary>
    /// Line number (1 or 2) for this chunk.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Collection of samples in this chunk.
    /// </summary>
    public List<Nir2Sample> Samples { get; set; } = new();

    /// <summary>
    /// Timestamp when the chunk started (first sample).
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the chunk ended (last sample).
    /// </summary>
    public DateTime EndedAt { get; set; }

    /// <summary>
    /// Aggregated protein value based on the configured strategy.
    /// </summary>
    public double? AggregatedProtein { get; set; }

    /// <summary>
    /// Aggregated moisture value based on the configured strategy.
    /// </summary>
    public double? AggregatedMoisture { get; set; }

    /// <summary>
    /// Aggregated timestamp based on the configured strategy.
    /// </summary>
    public DateTime? AggregatedTimestamp { get; set; }

    /// <summary>
    /// The aggregation strategy used to compute aggregated values.
    /// </summary>
    public NirAggregationStrategy AggregationStrategy { get; set; }

    /// <summary>
    /// Gets the number of samples in this chunk.
    /// </summary>
    public int SampleCount => Samples.Count;

    /// <summary>
    /// Adds a sample to this chunk.
    /// </summary>
    public void AddSample(Nir2Sample sample)
    {
        Samples.Add(sample);

        if (Samples.Count == 1)
        {
            StartedAt = sample.Timestamp;
        }

        EndedAt = sample.Timestamp;
    }

    /// <summary>
    /// Applies the specified aggregation strategy to compute final values.
    /// </summary>
    /// <param name="strategy">The aggregation strategy to apply.</param>
    public void Aggregate(NirAggregationStrategy strategy)
    {
        if (Samples.Count == 0)
            return;

        // Sort samples by timestamp for consistent aggregation
        var sortedSamples = Samples.OrderBy(s => s.Timestamp).ToList();

        AggregatedProtein = strategy switch
        {
            NirAggregationStrategy.First => sortedSamples[0].Protein,
            NirAggregationStrategy.Second => sortedSamples.Count > 1 ? sortedSamples[1].Protein : sortedSamples[0].Protein,
            NirAggregationStrategy.Last => sortedSamples[^1].Protein,
            NirAggregationStrategy.Median => CalculateMedian(sortedSamples.Select(s => s.Protein)),
            NirAggregationStrategy.Mean => sortedSamples.Average(s => s.Protein),
            _ => sortedSamples[0].Protein
        };

        AggregatedMoisture = strategy switch
        {
            NirAggregationStrategy.First => sortedSamples[0].Moisture,
            NirAggregationStrategy.Second => sortedSamples.Count > 1 ? sortedSamples[1].Moisture : sortedSamples[0].Moisture,
            NirAggregationStrategy.Last => sortedSamples[^1].Moisture,
            NirAggregationStrategy.Median => CalculateMedian(sortedSamples.Select(s => s.Moisture)),
            NirAggregationStrategy.Mean => sortedSamples.Average(s => s.Moisture),
            _ => sortedSamples[0].Moisture
        };

        AggregatedTimestamp = strategy switch
        {
            NirAggregationStrategy.First => sortedSamples[0].Timestamp,
            NirAggregationStrategy.Second => sortedSamples.Count > 1 ? sortedSamples[1].Timestamp : sortedSamples[0].Timestamp,
            NirAggregationStrategy.Last => sortedSamples[^1].Timestamp,
            NirAggregationStrategy.Median => sortedSamples[sortedSamples.Count / 2].Timestamp,
            NirAggregationStrategy.Mean => CalculateMeanTimestamp(sortedSamples),
            _ => sortedSamples[0].Timestamp
        };

        // Save the aggregation strategy used
        AggregationStrategy = strategy;
    }

    /// <summary>
    /// Calculates the median value from a collection of doubles.
    /// </summary>
    private static double CalculateMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;

        if (count == 0)
            return 0;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }

        return sorted[count / 2];
    }

    /// <summary>
    /// Calculates the mean timestamp from a collection of samples.
    /// </summary>
    private static DateTime CalculateMeanTimestamp(List<Nir2Sample> samples)
    {
        if (samples.Count == 0)
            return DateTime.MinValue;

        // Convert to ticks for arithmetic precision
        long totalTicks = 0;
        foreach (var sample in samples)
        {
            totalTicks += sample.Timestamp.Ticks;
        }

        return new DateTime(totalTicks / samples.Count);
    }

    /// <summary>
    /// Gets a summary string for debugging.
    /// </summary>
    public string GetSummary()
    {
        return $"Chunk {ChunkId}: {SampleCount} samples, " +
               $"{StartedAt:HH:mm:ss} → {EndedAt:HH:mm:ss}, " +
               $"P={AggregatedProtein:F2}%, M={AggregatedMoisture:F2}%";
    }
}
