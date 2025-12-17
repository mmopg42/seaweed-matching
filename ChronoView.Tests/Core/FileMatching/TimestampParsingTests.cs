using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChronoView.Core.FileMatching;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Core.FileMatching
{
    public class TimestampParsingTests
    {
        [Fact]
        public async Task ShouldParseNewFolderFormat()
        {
            // Arrange
            var matcher = new FileGroupMatcherService();
            var unmatchedFiles = new UnmatchedFiles();
            
            // Format: C251204T111028_0 (YYMMDD)
            // Year 25 -> 2025
            var folderName = "C251204T111028_0"; 
            var folderPath = @"C:\test\normal\" + folderName;
            
            unmatchedFiles.NormalFolders["normal1"] = new Dictionary<string, string>
            {
                { folderName, folderPath }
            };

            // Act
            var groups = await matcher.MatchFilesAsync(unmatchedFiles);
            var groupList = groups.ToList();

            // Assert
            Assert.Single(groupList);
            var group = groupList[0];
            
            // Expected: 2025-12-04 11:10:28
            var expectedTime = new DateTime(2025, 12, 04, 11, 10, 28);
            
            Assert.Equal(expectedTime, group.CreatedAt);
            Assert.Equal(folderName, group.NormalFolder);
            // Verify MainImagePath
            Assert.EndsWith("stitched_original.png", group.MainImagePath);
        }

        [Fact]
        public async Task ShouldParseLegacyFolderFormat()
        {
            // Arrange
            var matcher = new FileGroupMatcherService();
            var unmatchedFiles = new UnmatchedFiles();
            
            // Format: C20240115_143022
            var folderName = "C20240115_143022";
            var folderPath = @"C:\test\normal\" + folderName;
            
            unmatchedFiles.NormalFolders["normal1"] = new Dictionary<string, string>
            {
                { folderName, folderPath }
            };

            // Act
            var groups = await matcher.MatchFilesAsync(unmatchedFiles);
            var groupList = groups.ToList();

            // Assert
            Assert.Single(groupList);
            var group = groupList[0];
            
            var expectedTime = new DateTime(2024, 01, 15, 14, 30, 22);
            
            Assert.Equal(expectedTime, group.CreatedAt);
        }
    }
}
