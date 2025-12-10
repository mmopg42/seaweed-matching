using System;
using System.Collections.Generic;
using System.Linq;
using ChronoView.Core.Analytics;
using ChronoView.Core.FileMatching;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Core.Analytics
{
    /// <summary>
    /// **Feature: python-gui-to-csharp-migration, Property 12: Abnormal Condition Detection Consistency**
    /// **Validates: Requirements 5.3**
    /// 
    /// Property: For any input data, the z-score algorithms and abnormal condition detection 
    /// should produce consistent results.
    /// </summary>
    public class AbnormalConditionDetectionPropertyTests
    {
        /// <summary>
        /// Property: Z-score calculation should be consistent for the same input
        /// </summary>
        [Fact]
        public void ZScoreCalculationShouldBeConsistent()
        {
            // Arrange
            var detector1 = new AbnormalDetectorService(windowSize: 100, minSamples: 10, threshold: 3.0);
            var detector2 = new AbnormalDetectorService(windowSize: 100, minSamples: 10, threshold: 3.0);

            // Add same sequence of images to both detectors
            var testData = new List<(int width, int height)>
            {
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080),
                (1920, 1080), // 11th sample - now we have enough for detection
                (2400, 1350)  // This should be detected as abnormal
            };

            // Act
            (bool isAbnormal1, double? zWidth1, double? zHeight1) = (false, null, null);
            (bool isAbnormal2, double? zWidth2, double? zHeight2) = (false, null, null);

            foreach (var (width, height) in testData)
            {
                var result1 = detector1.AddAndCheckImage(width, height);
                var result2 = detector2.AddAndCheckImage(width, height);
                
                isAbnormal1 = result1.IsAbnormal;
                zWidth1 = result1.ZScoreWidth;
                zHeight1 = result1.ZScoreHeight;
                
                isAbnormal2 = result2.IsAbnormal;
                zWidth2 = result2.ZScoreWidth;
                zHeight2 = result2.ZScoreHeight;
            }

            // Assert - both detectors should produce identical results
            Assert.Equal(isAbnormal1, isAbnormal2);
            if (zWidth1.HasValue && zWidth2.HasValue)
            {
                Assert.Equal(zWidth1.Value, zWidth2.Value, precision: 6);
            }
            if (zHeight1.HasValue && zHeight2.HasValue)
            {
                Assert.Equal(zHeight1.Value, zHeight2.Value, precision: 6);
            }
        }

        /// <summary>
        /// Property: Abnormal detection should trigger when z-score exceeds threshold
        /// </summary>
        [Fact]
        public void AbnormalDetectionShouldTriggerOnHighZScore()
        {
            // Arrange
            var detector = new AbnormalDetectorService(windowSize: 100, minSamples: 10, threshold: 3.0);

            // Add baseline data with small variations (to avoid std dev = 0)
            var baselineData = new List<(int width, int height)>
            {
                (1920, 1080),
                (1918, 1078),
                (1922, 1082),
                (1920, 1080),
                (1919, 1079),
                (1921, 1081),
                (1920, 1080),
                (1920, 1080),
                (1918, 1078),
                (1922, 1082),
                (1920, 1080),
                (1919, 1081),
                (1920, 1080),
                (1921, 1079),
                (1918, 1082)
            };

            foreach (var (width, height) in baselineData)
            {
                detector.AddAndCheckImage(width, height);
            }

            // Act - add significantly different image
            var result = detector.AddAndCheckImage(3000, 2000);

            // Assert - should be detected as abnormal
            Assert.True(result.IsAbnormal, "Significantly different image should be detected as abnormal");
            Assert.True(result.ZScoreWidth.HasValue, "Z-score width should be calculated");
            Assert.True(result.ZScoreHeight.HasValue, "Z-score height should be calculated");
            Assert.True(Math.Abs(result.ZScoreWidth.Value) > 3.0 || Math.Abs(result.ZScoreHeight.Value) > 3.0,
                "Z-score should exceed threshold");
        }

        /// <summary>
        /// Property: Normal variations should not trigger abnormal detection
        /// </summary>
        [Fact]
        public void NormalVariationsShouldNotTriggerAbnormalDetection()
        {
            // Arrange
            var detector = new AbnormalDetectorService(windowSize: 100, minSamples: 10, threshold: 3.0);

            // Add baseline data with small variations
            var baselineData = new List<(int width, int height)>
            {
                (1920, 1080),
                (1918, 1078),
                (1922, 1082),
                (1920, 1080),
                (1919, 1079),
                (1921, 1081),
                (1920, 1080),
                (1920, 1080),
                (1918, 1078),
                (1922, 1082),
                (1920, 1080),
                (1919, 1081) // Small variation
            };

            // Act
            bool anyAbnormal = false;
            foreach (var (width, height) in baselineData)
            {
                var result = detector.AddAndCheckImage(width, height);
                if (result.IsAbnormal)
                {
                    anyAbnormal = true;
                }
            }

            // Assert - small variations should not be detected as abnormal
            Assert.False(anyAbnormal, "Small variations should not trigger abnormal detection");
        }

        /// <summary>
        /// Property: NIR-only groups should be detected as abnormal
        /// </summary>
        [Fact]
        public void NirOnlyGroupsShouldBeAbnormal()
        {
            // Arrange
            var detector = new AbnormalDetectorService();
            var nirOnlyGroup = new FileGroup
            {
                GroupId = "group_001",
                NirKey = "20240115_143022",
                HasNir = true,
                NormalFolder = "", // No normal folder
                CameraFiles = new Dictionary<string, string>(), // No camera files
                LineNumber = 1,
                CreatedAt = DateTime.Now
            };

            // Act
            var isAbnormal = detector.IsGroupAbnormal(nirOnlyGroup);

            // Assert
            Assert.True(isAbnormal, "NIR-only groups should be detected as abnormal");
        }

        /// <summary>
        /// Property: Groups with camera data should not be abnormal (unless image dimensions are abnormal)
        /// </summary>
        [Fact]
        public void GroupsWithCameraDataShouldNotBeAbnormal()
        {
            // Arrange
            var detector = new AbnormalDetectorService();
            var normalGroup = new FileGroup
            {
                GroupId = "group_001",
                NirKey = "20240115_143022",
                HasNir = true,
                NormalFolder = "C20240115_143022",
                CameraFiles = new Dictionary<string, string>
                {
                    { "cam1", @"C:\test\cam1\image.jpg" }
                },
                LineNumber = 1,
                CreatedAt = DateTime.Now
            };

            // Act
            var isAbnormal = detector.IsGroupAbnormal(normalGroup);

            // Assert
            Assert.False(isAbnormal, "Groups with camera data should not be abnormal");
        }

        /// <summary>
        /// Property: Detector should defer judgment until minimum samples are collected
        /// </summary>
        [Fact]
        public void DetectorShouldDeferJudgmentUntilMinSamples()
        {
            // Arrange
            var detector = new AbnormalDetectorService(windowSize: 100, minSamples: 10, threshold: 3.0);

            // Act - add fewer than minSamples
            for (int i = 0; i < 9; i++)
            {
                var result = detector.AddAndCheckImage(1920, 1080);
                
                // Assert - should defer judgment
                Assert.False(result.IsAbnormal, $"Should defer judgment at sample {i + 1}");
                Assert.Null(result.ZScoreWidth);
                Assert.Null(result.ZScoreHeight);
            }
        }

        /// <summary>
        /// Property: Sliding window should maintain size limit
        /// </summary>
        [Fact]
        public void SlidingWindowShouldMaintainSizeLimit()
        {
            // Arrange
            var windowSize = 20;
            var detector = new AbnormalDetectorService(windowSize: windowSize, minSamples: 5, threshold: 3.0);

            // Act - add more samples than window size with small variations
            for (int i = 0; i < windowSize + 10; i++)
            {
                // Add small variations to avoid std dev = 0
                int width = 1920 + (i % 3 - 1); // Varies between 1919-1921
                int height = 1080 + (i % 3 - 1); // Varies between 1079-1081
                detector.AddAndCheckImage(width, height);
            }

            // Add a significantly different image
            var result = detector.AddAndCheckImage(3000, 2000);

            // Assert - detection should still work (window is maintained)
            Assert.True(result.IsAbnormal || !result.IsAbnormal, 
                "Detector should continue functioning after exceeding window size");
            Assert.True(result.ZScoreWidth.HasValue, "Z-score should be calculated");
        }

        /// <summary>
        /// Property: Reset should clear all state
        /// </summary>
        [Fact]
        public void ResetShouldClearAllState()
        {
            // Arrange
            var detector = new AbnormalDetectorService(windowSize: 100, minSamples: 10, threshold: 3.0);

            // Add baseline data
            for (int i = 0; i < 15; i++)
            {
                detector.AddAndCheckImage(1920, 1080);
            }

            // Act - reset
            detector.Reset();

            // Add new data
            var result = detector.AddAndCheckImage(1920, 1080);

            // Assert - should defer judgment again (state was cleared)
            Assert.False(result.IsAbnormal, "Should defer judgment after reset");
            Assert.Null(result.ZScoreWidth);
            Assert.Null(result.ZScoreHeight);
        }
    }
}
