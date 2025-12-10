using System;
using System.Collections.Generic;
using System.Linq;
using ChronoView.Models;

namespace ChronoView.Core.Analytics
{
    /// <summary>
    /// Statistical z-score based abnormal condition detector
    /// Uses sliding window approach to detect outliers based on recent data
    /// </summary>
    public class AbnormalDetectorService : IAbnormalDetector
    {
        private readonly List<int> _widthBuffer;
        private readonly List<int> _heightBuffer;

        public int WindowSize { get; set; }
        public int MinSamples { get; set; }
        public double Threshold { get; set; }

        public AbnormalDetectorService(int windowSize = 100, int minSamples = 10, double threshold = 3.0)
        {
            WindowSize = windowSize;
            MinSamples = minSamples;
            Threshold = threshold;

            _widthBuffer = new List<int>();
            _heightBuffer = new List<int>();
        }

        public void Reset()
        {
            _widthBuffer.Clear();
            _heightBuffer.Clear();
        }

        public (bool IsAbnormal, double? ZScoreWidth, double? ZScoreHeight) AddAndCheckImage(int width, int height)
        {
            // If not enough samples, just add and defer judgment
            if (_widthBuffer.Count < MinSamples)
            {
                _widthBuffer.Add(width);
                _heightBuffer.Add(height);
                return (false, null, null);
            }

            // Calculate z-scores based on previous data (excluding current value)
            var zWidth = CalculateZScore(width, _widthBuffer);
            var zHeight = CalculateZScore(height, _heightBuffer);

            // If z-score cannot be calculated (std dev = 0), consider normal
            if (!zWidth.HasValue || !zHeight.HasValue)
            {
                _widthBuffer.Add(width);
                _heightBuffer.Add(height);
                MaintainWindowSize();
                return (false, null, null);
            }

            // Determine if abnormal
            bool isAbnormal = Math.Abs(zWidth.Value) > Threshold || Math.Abs(zHeight.Value) > Threshold;

            // Add to buffer after judgment
            _widthBuffer.Add(width);
            _heightBuffer.Add(height);
            MaintainWindowSize();

            return (isAbnormal, zWidth, zHeight);
        }

        public bool IsGroupAbnormal(FileGroup group)
        {
            if (group == null) return false;

            try
            {
                // Check for NIR-only group (no camera data but has NIR)
                bool hasCamera = !string.IsNullOrEmpty(group.NormalFolder) || 
                                (group.CameraFiles != null && group.CameraFiles.Count > 0);
                bool hasNir = group.HasNir && !string.IsNullOrEmpty(group.NirKey);

                // NIR-only groups are considered abnormal
                if (!hasCamera && hasNir)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                // On error, consider normal
                return false;
            }
        }

        private double? CalculateZScore(double value, List<int> valuesList)
        {
            if (valuesList == null || valuesList.Count < 2)
                return null;

            // Calculate mean
            double mean = valuesList.Average();

            // Calculate variance
            double variance = valuesList.Sum(x => Math.Pow(x - mean, 2)) / valuesList.Count;

            // Calculate standard deviation
            if (variance == 0)
                return null; // All values are identical, cannot calculate z-score

            double std = Math.Sqrt(variance);

            // Calculate z-score
            double zScore = (value - mean) / std;

            return zScore;
        }

        private void MaintainWindowSize()
        {
            // Remove oldest data if exceeds window size
            while (_widthBuffer.Count > WindowSize)
            {
                _widthBuffer.RemoveAt(0);
                _heightBuffer.RemoveAt(0);
            }
        }
    }
}
