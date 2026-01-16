using ChronoView.Models;

namespace ChronoView.Core.Analytics;

/// <summary>
/// Interface for detecting abnormal conditions in file groups.
/// Uses aspect ratio deviation for abnormality detection.
/// </summary>
public interface IAbnormalDetector
{
    /// <summary>
    /// Add image dimensions and check if abnormal based on aspect ratio deviation.
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="context">Context string (e.g., "Line1_Cam1").</param>
    /// <returns>Tuple of (IsAbnormal, RatioDiff). RatioDiff is null if insufficient samples.</returns>
    (bool IsAbnormal, double? RatioDiff) AddAndCheckImage(int width, int height, string context);

    /// <summary>
    /// Check if a file group is abnormal.
    /// Currently detects NIR-only groups (NIR present but no camera data).
    /// </summary>
    /// <param name="group">The file group to check.</param>
    /// <returns>True if the group is abnormal.</returns>
    bool IsGroupAbnormal(FileGroup group);

    /// <summary>
    /// Reset the detection buffers and clear all historical data.
    /// </summary>
    void Reset();

    /// <summary>
    /// Sliding window size for ratio history. Default: 40.
    /// </summary>
    int WindowSize { get; set; }

    /// <summary>
    /// Minimum samples required before enabling detection. Default: 5.
    /// </summary>
    int MinSamples { get; set; }

    /// <summary>
    /// Absolute ratio difference threshold for abnormal detection.
    /// If |CurrentRatio - MedianRatio| > Threshold, flag as abnormal.
    /// Default: 0.3.
    /// </summary>
    double Threshold { get; set; }
}
