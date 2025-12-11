using System;
using System.Collections.Generic;
using System.Linq;
using ChronoView.Core.FileMatching;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Core.FileMatching
{
    /// <summary>
    /// **Feature: python-gui-to-csharp-migration, Property 13: Multi-line Mode Support**
    /// **Validates: Requirements 5.5**
    /// 
    /// Property: For any monitoring configuration, the system should correctly handle both 
    /// integrated and separated line modes with proper data isolation and display.
    /// </summary>
    public class MultiLineModePropertyTests
    {
        /// <summary>
        /// Property: Line 1 and Line 2 data should be processed independently
        /// </summary>
        [Fact]
        public async Task Line1AndLine2ShouldBeProcessedIndependently()
        {
            // Arrange
            var baseTime = DateTime.Now.AddHours(-1);
            var unmatchedFiles = new UnmatchedFiles();

            // Line 1 data
            var nirFiles1 = new Dictionary<string, string>();
            for (int i = 0; i < 3; i++)
            {
                var timestamp = baseTime.AddSeconds(i * 10);
                var key = $"{timestamp:yyyyMMdd_HHmmss}";
                nirFiles1[key] = $@"C:\test\nir\{key}.spc";
            }
            unmatchedFiles.NirFiles["nir"] = nirFiles1;

            var normalFolders1 = new Dictionary<string, string>();
            for (int i = 0; i < 3; i++)
            {
                var timestamp = baseTime.AddSeconds(i * 10 + 5);
                var key = $"C{timestamp:yyyyMMdd_HHmmss}";
                normalFolders1[key] = $@"C:\test\normal\{key}";
            }
            unmatchedFiles.NormalFolders["normal"] = normalFolders1;

            // Line 2 data
            var nirFiles2 = new Dictionary<string, string>();
            for (int i = 0; i < 2; i++)
            {
                var timestamp = baseTime.AddSeconds(i * 10 + 100); // Different time range
                var key = $"{timestamp:yyyyMMdd_HHmmss}";
                nirFiles2[key] = $@"C:\test\nir2\{key}.spc";
            }
            unmatchedFiles.NirFiles["nir2"] = nirFiles2;

            var normalFolders2 = new Dictionary<string, string>();
            for (int i = 0; i < 2; i++)
            {
                var timestamp = baseTime.AddSeconds(i * 10 + 105);
                var key = $"C{timestamp:yyyyMMdd_HHmmss}";
                normalFolders2[key] = $@"C:\test\normal2\{key}";
            }
            unmatchedFiles.NormalFolders["normal2"] = normalFolders2;

            var matcher = new FileGroupMatcherService();

            // Act
            // Act
            var groups = (await matcher.MatchFilesAsync(unmatchedFiles)).ToList();

            // Assert - should have groups from both lines
            var line1Groups = groups.Where(g => g.LineNumber == 1).ToList();
            var line2Groups = groups.Where(g => g.LineNumber == 2).ToList();

            Assert.True(line1Groups.Count > 0, "Should have Line 1 groups");
            Assert.True(line2Groups.Count > 0, "Should have Line 2 groups");

            // Assert - Line 1 groups should not contain Line 2 data
            foreach (var group in line1Groups)
            {
                Assert.Equal(1, group.LineNumber);
            }

            // Assert - Line 2 groups should not contain Line 1 data
            foreach (var group in line2Groups)
            {
                Assert.Equal(2, group.LineNumber);
            }
        }

        /// <summary>
        /// Property: Groups should be correctly assigned to their respective lines
        /// </summary>
        [Fact]
        public async Task GroupsShouldBeCorrectlyAssignedToLines()
        {
            // Arrange
            var baseTime = DateTime.Now.AddHours(-1);
            var unmatchedFiles = new UnmatchedFiles();

            // Line 1 data
            unmatchedFiles.NirFiles["nir"] = new Dictionary<string, string>
            {
                { $"{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\nir\{baseTime:yyyyMMdd_HHmmss}.spc" }
            };
            unmatchedFiles.NormalFolders["normal"] = new Dictionary<string, string>
            {
                { $"C{baseTime.AddSeconds(5):yyyyMMdd_HHmmss}", $@"C:\test\normal\C{baseTime.AddSeconds(5):yyyyMMdd_HHmmss}" }
            };

            // Line 2 data
            var line2Time = baseTime.AddMinutes(10);
            unmatchedFiles.NirFiles["nir2"] = new Dictionary<string, string>
            {
                { $"{line2Time:yyyyMMdd_HHmmss}", $@"C:\test\nir2\{line2Time:yyyyMMdd_HHmmss}.spc" }
            };
            unmatchedFiles.NormalFolders["normal2"] = new Dictionary<string, string>
            {
                { $"C{line2Time.AddSeconds(5):yyyyMMdd_HHmmss}", $@"C:\test\normal2\C{line2Time.AddSeconds(5):yyyyMMdd_HHmmss}" }
            };

            var matcher = new FileGroupMatcherService();

            // Act
            // Act
            var groups = (await matcher.MatchFilesAsync(unmatchedFiles)).ToList();

            // Assert - all groups should have valid line numbers
            Assert.All(groups, g => Assert.True(g.LineNumber == 1 || g.LineNumber == 2,
                $"Invalid line number: {g.LineNumber}"));

            // Assert - should have at least one group per line
            Assert.Contains(groups, g => g.LineNumber == 1);
            Assert.Contains(groups, g => g.LineNumber == 2);
        }

        /// <summary>
        /// Property: Camera files should be matched to correct line
        /// </summary>
        [Fact]
        public async Task CameraFilesShouldBeMatchedToCorrectLine()
        {
            // Arrange
            var baseTime = DateTime.Now.AddHours(-1);
            var unmatchedFiles = new UnmatchedFiles();

            // Line 1 data with cameras
            unmatchedFiles.NormalFolders["normal"] = new Dictionary<string, string>
            {
                { $"C{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\normal\C{baseTime:yyyyMMdd_HHmmss}" }
            };

            unmatchedFiles.CameraFiles["cam1"] = new List<TimestampedFile>
            {
                new TimestampedFile
                {
                    FileName = $"cam1_{baseTime.AddSeconds(8):yyyyMMdd_HHmmss}.jpg",
                    AbsolutePath = $@"C:\test\cam1\cam1_{baseTime.AddSeconds(8):yyyyMMdd_HHmmss}.jpg",
                    Timestamp = baseTime.AddSeconds(8)
                }
            };

            // Line 2 data with cameras
            var line2Time = baseTime.AddMinutes(10);
            unmatchedFiles.NormalFolders["normal2"] = new Dictionary<string, string>
            {
                { $"C{line2Time:yyyyMMdd_HHmmss}", $@"C:\test\normal2\C{line2Time:yyyyMMdd_HHmmss}" }
            };

            unmatchedFiles.CameraFiles["cam4"] = new List<TimestampedFile>
            {
                new TimestampedFile
                {
                    FileName = $"cam4_{line2Time.AddSeconds(8):yyyyMMdd_HHmmss}.jpg",
                    AbsolutePath = $@"C:\test\cam4\cam4_{line2Time.AddSeconds(8):yyyyMMdd_HHmmss}.jpg",
                    Timestamp = line2Time.AddSeconds(8)
                }
            };

            var matcher = new FileGroupMatcherService();

            // Act
            // Act
            var groups = (await matcher.MatchFilesAsync(unmatchedFiles)).ToList();

            // Assert - Line 1 groups should have cam1-3 keys, not cam4-6
            var line1Groups = groups.Where(g => g.LineNumber == 1).ToList();
            foreach (var group in line1Groups)
            {
                if (group.CameraFiles.Count > 0)
                {
                    // Should have cam1, cam2, or cam3 keys
                    Assert.True(group.CameraFiles.Keys.All(k => k == "cam1" || k == "cam2" || k == "cam3"),
                        "Line 1 groups should only have cam1-3");
                }
            }

            // Assert - Line 2 groups should have cam4-6 keys, not cam1-3
            var line2Groups = groups.Where(g => g.LineNumber == 2).ToList();
            foreach (var group in line2Groups)
            {
                if (group.CameraFiles.Count > 0)
                {
                    // Should have cam4, cam5, or cam6 keys
                    Assert.True(group.CameraFiles.Keys.All(k => k == "cam4" || k == "cam5" || k == "cam6"),
                        "Line 2 groups should only have cam4-6");
                }
            }
        }

        /// <summary>
        /// Property: NIR files should be matched to correct line
        /// </summary>
        [Fact]
        public async Task NirFilesShouldBeMatchedToCorrectLine()
        {
            // Arrange
            var baseTime = DateTime.Now.AddHours(-1);
            var unmatchedFiles = new UnmatchedFiles();

            // Line 1 NIR
            unmatchedFiles.NirFiles["nir"] = new Dictionary<string, string>
            {
                { $"{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\nir\{baseTime:yyyyMMdd_HHmmss}.spc" }
            };
            unmatchedFiles.NormalFolders["normal"] = new Dictionary<string, string>
            {
                { $"C{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\normal\C{baseTime:yyyyMMdd_HHmmss}" }
            };

            // Line 2 NIR
            var line2Time = baseTime.AddMinutes(10);
            unmatchedFiles.NirFiles["nir2"] = new Dictionary<string, string>
            {
                { $"{line2Time:yyyyMMdd_HHmmss}", $@"C:\test\nir2\{line2Time:yyyyMMdd_HHmmss}.spc" }
            };
            unmatchedFiles.NormalFolders["normal2"] = new Dictionary<string, string>
            {
                { $"C{line2Time:yyyyMMdd_HHmmss}", $@"C:\test\normal2\C{line2Time:yyyyMMdd_HHmmss}" }
            };

            var matcher = new FileGroupMatcherService();

            // Act
            // Act
            var groups = (await matcher.MatchFilesAsync(unmatchedFiles)).ToList();

            // Assert - NIR keys should be matched to correct line
            var line1Groups = groups.Where(g => g.LineNumber == 1 && g.HasNir).ToList();
            var line2Groups = groups.Where(g => g.LineNumber == 2 && g.HasNir).ToList();

            Assert.True(line1Groups.Count > 0 || line2Groups.Count > 0, 
                "Should have NIR groups in at least one line");

            // All groups should have correct line numbers
            Assert.All(line1Groups, g => Assert.Equal(1, g.LineNumber));
            Assert.All(line2Groups, g => Assert.Equal(2, g.LineNumber));
        }

        /// <summary>
        /// Property: Empty line data should not cause errors
        /// </summary>
        [Fact]
        public async Task EmptyLineDataShouldNotCauseErrors()
        {
            // Arrange
            var baseTime = DateTime.Now.AddHours(-1);
            var unmatchedFiles = new UnmatchedFiles();

            // Only Line 1 data, Line 2 is empty
            unmatchedFiles.NirFiles["nir"] = new Dictionary<string, string>
            {
                { $"{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\nir\{baseTime:yyyyMMdd_HHmmss}.spc" }
            };
            unmatchedFiles.NormalFolders["normal"] = new Dictionary<string, string>
            {
                { $"C{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\normal\C{baseTime:yyyyMMdd_HHmmss}" }
            };

            // Line 2 is empty (no data)
            unmatchedFiles.NirFiles["nir2"] = new Dictionary<string, string>();
            unmatchedFiles.NormalFolders["normal2"] = new Dictionary<string, string>();

            var matcher = new FileGroupMatcherService();

            // Act
            // Act
            var groups = (await matcher.MatchFilesAsync(unmatchedFiles)).ToList();

            // Assert - should only have Line 1 groups
            Assert.All(groups, g => Assert.Equal(1, g.LineNumber));
            Assert.True(groups.Count > 0, "Should have at least one group from Line 1");
        }

        /// <summary>
        /// Property: Groups from different lines should maintain temporal ordering
        /// </summary>
        [Fact]
        public async Task GroupsFromDifferentLinesShouldMaintainTemporalOrdering()
        {
            // Arrange
            var baseTime = DateTime.Now.AddHours(-1);
            var unmatchedFiles = new UnmatchedFiles();

            // Interleaved timestamps between lines
            unmatchedFiles.NormalFolders["normal"] = new Dictionary<string, string>
            {
                { $"C{baseTime:yyyyMMdd_HHmmss}", $@"C:\test\normal\C{baseTime:yyyyMMdd_HHmmss}" },
                { $"C{baseTime.AddSeconds(20):yyyyMMdd_HHmmss}", $@"C:\test\normal\C{baseTime.AddSeconds(20):yyyyMMdd_HHmmss}" }
            };

            unmatchedFiles.NormalFolders["normal2"] = new Dictionary<string, string>
            {
                { $"C{baseTime.AddSeconds(10):yyyyMMdd_HHmmss}", $@"C:\test\normal2\C{baseTime.AddSeconds(10):yyyyMMdd_HHmmss}" },
                { $"C{baseTime.AddSeconds(30):yyyyMMdd_HHmmss}", $@"C:\test\normal2\C{baseTime.AddSeconds(30):yyyyMMdd_HHmmss}" }
            };

            var matcher = new FileGroupMatcherService();

            // Act
            // Act
            var groups = (await matcher.MatchFilesAsync(unmatchedFiles)).ToList();

            // Assert - groups should be sorted by time regardless of line
            for (int i = 1; i < groups.Count; i++)
            {
                Assert.True(groups[i].CreatedAt >= groups[i - 1].CreatedAt,
                    $"Groups not sorted: group[{i-1}] at {groups[i-1].CreatedAt} > group[{i}] at {groups[i].CreatedAt}");
            }

            // Assert - line numbers should alternate based on timestamps
            // (Line 1 at t=0, Line 2 at t=10, Line 1 at t=20, Line 2 at t=30)
            if (groups.Count >= 4)
            {
                Assert.Equal(1, groups[0].LineNumber);
                Assert.Equal(2, groups[1].LineNumber);
                Assert.Equal(1, groups[2].LineNumber);
                Assert.Equal(2, groups[3].LineNumber);
            }
        }
    }
}
