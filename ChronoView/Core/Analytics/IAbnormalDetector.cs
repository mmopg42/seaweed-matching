using System;
using ChronoView.Models;

namespace ChronoView.Core.Analytics
{
    /// <summary>
    /// Interface for abnormal condition detection using statistical analysis
    /// </summary>
    public interface IAbnormalDetector
    {
        /// <summary>
        /// Add image dimensions and check if abnormal
        /// </summary>
        /// <returns>Tuple of (isAbnormal, zScoreWidth, zScoreHeight)</returns>
        (bool IsAbnormal, double? ZScoreWidth, double? ZScoreHeight) AddAndCheckImage(int width, int height);

        /// <summary>
        /// Check if a file group is abnormal
        /// </summary>
        bool IsGroupAbnormal(FileGroup group);

        /// <summary>
        /// Reset the detection buffers
        /// </summary>
        void Reset();

        /// <summary>
        /// Get or set the window size for sliding window analysis
        /// </summary>
        int WindowSize { get; set; }

        /// <summary>
        /// Get or set the minimum samples required before detection
        /// </summary>
        int MinSamples { get; set; }

        /// <summary>
        /// Get or set the z-score threshold for abnormality
        /// </summary>
        double Threshold { get; set; }
    }
}
