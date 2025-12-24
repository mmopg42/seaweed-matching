using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoView.Core.FileMatching;
using ChronoView.Core.FileWatching;
using ChronoView.Core.Nir;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileWatching
{
    /// <summary>
    /// Tests for MonitoringOrchestrator event raising
    /// Validates Requirements: 4.1
    /// </summary>
    public class MonitoringOrchestratorEventTests
    {
        private readonly Mock<IFileGroupMatcher> _mockMatcher;
        private readonly Mock<IFileWatcher> _mockWatcher;
        private readonly Mock<ILogger<MonitoringOrchestrator>> _mockLogger;
        private readonly MonitoringOrchestrator _orchestrator;
        private readonly ApplicationConfiguration _testConfig;
        private readonly Mock<INirFileResolver> _mockNirResolver;
        private readonly FolderTimestampCache _folderTimestamps;

        public MonitoringOrchestratorEventTests()
        {
            _mockMatcher = new Mock<IFileGroupMatcher>();
            _mockWatcher = new Mock<IFileWatcher>();
            _mockLogger = new Mock<ILogger<MonitoringOrchestrator>>();
            _mockNirResolver = new Mock<INirFileResolver>();
            _folderTimestamps = new FolderTimestampCache();
            _orchestrator = new MonitoringOrchestrator(_mockMatcher.Object, _mockWatcher.Object, _mockLogger.Object, _mockNirResolver.Object, _folderTimestamps);

            _testConfig = new ApplicationConfiguration
            {
                MatchingSettings = new MatchingSettings
                {
                    Nir1Path = "C:/Test/NIR",
                    Normal1Path = "C:/Test/Normal",
                    Camera1Path = "C:/Test/Cam1"
                }
            };

            _mockMatcher.Setup(m => m.Configuration).Returns(new MatchingConfiguration
            {
                Nir1Path = _testConfig.MatchingSettings.Nir1Path,
                Normal1Path = _testConfig.MatchingSettings.Normal1Path,
                Camera1Path = _testConfig.MatchingSettings.Camera1Path
            });
        }

        [Fact]
        public async Task GroupCreated_Event_RaisedWhenFileGroupMatcherCreatesGroup()
        {
            // Arrange
            var testGroup = new FileGroup
            {
                GroupId = "test-group-1",
                NirKey = "sample1",
                HasNir = true,
                LineNumber = 1
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(new List<FileGroup> { testGroup });

            FileGroup? raisedGroup = null;
            _orchestrator.GroupCreated += (sender, group) => raisedGroup = group;

            // Act
            await _orchestrator.StartAsync(_testConfig);

            // Assert
            Assert.NotNull(raisedGroup);
            Assert.Equal(testGroup.GroupId, raisedGroup.GroupId);
            Assert.Equal(testGroup.NirKey, raisedGroup.NirKey);
        }

        [Fact]
        public async Task GroupCreated_Event_ContainsCorrectData()
        {
            // Arrange
            var testGroup = new FileGroup
            {
                GroupId = "test-group-2",
                NirKey = "sample2",
                NormalFolder = "C:/Test/Normal/sample2",
                HasNir = true,
                LineNumber = 2,
                CameraFiles = new Dictionary<string, string>
                {
                    { "Cam1", "C:/Test/Cam1/sample2.jpg" }
                }
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(new List<FileGroup> { testGroup });

            FileGroup? raisedGroup = null;
            _orchestrator.GroupCreated += (sender, group) => raisedGroup = group;

            // Act
            await _orchestrator.StartAsync(_testConfig);

            // Assert
            Assert.NotNull(raisedGroup);
            Assert.Equal(testGroup.GroupId, raisedGroup.GroupId);
            Assert.Equal(testGroup.NirKey, raisedGroup.NirKey);
            Assert.Equal(testGroup.NormalFolder, raisedGroup.NormalFolder);
            Assert.Equal(testGroup.LineNumber, raisedGroup.LineNumber);
            Assert.True(raisedGroup.HasNir);
            Assert.Single(raisedGroup.CameraFiles);
            Assert.Equal("C:/Test/Cam1/sample2.jpg", raisedGroup.CameraFiles["Cam1"]);
        }

        [Fact]
        public async Task GroupCreated_Event_RaisedForMultipleGroups()
        {
            // Arrange
            var testGroups = new List<FileGroup>
            {
                new FileGroup { GroupId = "group-1", NirKey = "sample1", HasNir = true, LineNumber = 1 },
                new FileGroup { GroupId = "group-2", NirKey = "sample2", HasNir = true, LineNumber = 1 },
                new FileGroup { GroupId = "group-3", NirKey = "sample3", HasNir = true, LineNumber = 1 }
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(testGroups);

            var raisedGroups = new List<FileGroup>();
            _orchestrator.GroupCreated += (sender, group) => raisedGroups.Add(group);

            // Act
            await _orchestrator.StartAsync(_testConfig);

            // Assert
            Assert.Equal(3, raisedGroups.Count);
            Assert.Contains(raisedGroups, g => g.GroupId == "group-1");
            Assert.Contains(raisedGroups, g => g.GroupId == "group-2");
            Assert.Contains(raisedGroups, g => g.GroupId == "group-3");
        }

        [Fact]
        public async Task GroupRemoved_Event_RaisedWhenRefreshClearsGroups()
        {
            // Arrange
            var initialGroup = new FileGroup
            {
                GroupId = "group-to-remove",
                NirKey = "sample1",
                HasNir = true,
                LineNumber = 1
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(new List<FileGroup> { initialGroup });

            await _orchestrator.StartAsync(_testConfig);

            // Setup for refresh - return empty list
            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(new List<FileGroup>());

            var removedGroupIds = new List<string>();
            _orchestrator.GroupRemoved += (sender, groupId) => removedGroupIds.Add(groupId);

            // Act
            await _orchestrator.RefreshAsync();

            // Assert
            Assert.Single(removedGroupIds);
            Assert.Equal("group-to-remove", removedGroupIds[0]);
        }

        [Fact]
        public async Task MonitoringError_Event_RaisedWhenStartFails()
        {
            // Arrange
            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));

            string? errorMessage = null;
            _orchestrator.MonitoringError += (sender, message) => errorMessage = message;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _orchestrator.StartAsync(_testConfig));

            Assert.NotNull(errorMessage);
            Assert.Contains("Test error", errorMessage);
        }

        [Fact]
        public async Task MultipleSubscribers_ReceiveEvents()
        {
            // Arrange
            var testGroup = new FileGroup
            {
                GroupId = "test-group",
                NirKey = "sample1",
                HasNir = true,
                LineNumber = 1
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(new List<FileGroup> { testGroup });

            FileGroup? subscriber1Group = null;
            FileGroup? subscriber2Group = null;
            FileGroup? subscriber3Group = null;

            _orchestrator.GroupCreated += (sender, group) => subscriber1Group = group;
            _orchestrator.GroupCreated += (sender, group) => subscriber2Group = group;
            _orchestrator.GroupCreated += (sender, group) => subscriber3Group = group;

            // Act
            await _orchestrator.StartAsync(_testConfig);

            // Assert
            Assert.NotNull(subscriber1Group);
            Assert.NotNull(subscriber2Group);
            Assert.NotNull(subscriber3Group);
            Assert.Equal(testGroup.GroupId, subscriber1Group.GroupId);
            Assert.Equal(testGroup.GroupId, subscriber2Group.GroupId);
            Assert.Equal(testGroup.GroupId, subscriber3Group.GroupId);
        }

        [Fact]
        public async Task FileGroupsCreated_BatchEvent_AlsoRaised()
        {
            // Arrange
            var testGroups = new List<FileGroup>
            {
                new FileGroup { GroupId = "group-1", NirKey = "sample1", HasNir = true, LineNumber = 1 },
                new FileGroup { GroupId = "group-2", NirKey = "sample2", HasNir = true, LineNumber = 1 }
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(testGroups);

            FileGroupsCreatedEventArgs? batchEventArgs = null;
            _orchestrator.FileGroupsCreated += (sender, args) => batchEventArgs = args;

            // Act
            await _orchestrator.StartAsync(_testConfig);

            // Assert
            Assert.NotNull(batchEventArgs);
            Assert.Equal(2, batchEventArgs.Groups.Count);
            Assert.Contains(batchEventArgs.Groups, g => g.GroupId == "group-1");
            Assert.Contains(batchEventArgs.Groups, g => g.GroupId == "group-2");
        }

        [Fact]
        public async Task GroupRemoved_Event_RaisedForDeletedFiles()
        {
            // Arrange
            var testGroup = new FileGroup
            {
                GroupId = "group-with-file",
                NirKey = "C:/Test/NIR/sample1.spc",
                HasNir = true,
                LineNumber = 1
            };

            _mockMatcher.Setup(m => m.MatchFilesAsync(It.IsAny<UnmatchedFiles>()))
                .ReturnsAsync(new List<FileGroup> { testGroup });

            await _orchestrator.StartAsync(_testConfig);

            var removedGroupIds = new List<string>();
            _orchestrator.GroupRemoved += (sender, groupId) => removedGroupIds.Add(groupId);

            var deleteEvent = new FileSystemEventArgs(
                WatcherChangeTypes.Deleted,
                "C:/Test/NIR",
                "sample1.spc");

            // Act
            await _orchestrator.ProcessFileEventsAsync(new List<FileSystemEventArgs> { deleteEvent });

            // Assert
            Assert.Single(removedGroupIds);
            Assert.Equal("group-with-file", removedGroupIds[0]);
        }
    }
}
