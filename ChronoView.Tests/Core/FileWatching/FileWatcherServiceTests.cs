using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChronoView.Core.FileWatching;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileWatching
{
    /// <summary>
    /// Unit tests for FileWatcherService
    /// **Feature: python-gui-to-csharp-migration, Property 4: Real-time File System Monitoring**
    /// **Validates: Requirements 4.1**
    /// </summary>
    public class FileWatcherServiceTests : IDisposable
    {
        private readonly string _testRootPath;
        private readonly List<string> _createdDirectories = new();
        private readonly List<string> _createdFiles = new();

        public FileWatcherServiceTests()
        {
            _testRootPath = Path.Combine(Path.GetTempPath(), "ChronoViewTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testRootPath);
            _createdDirectories.Add(_testRootPath);
        }

        [Fact]
        public async Task FileWatcher_DetectsFileCreation()
        {
            // Arrange
            var testDir = CreateTestDirectory();
            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var watcher = new FileWatcherService(mockLogger.Object);
            var eventDetected = false;
            var eventSignal = new ManualResetEventSlim(false);

            watcher.FileChanged += (sender, e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    eventDetected = true;
                    eventSignal.Set();
                }
            };

            // Act
            await watcher.StartWatchingAsync(new[] { testDir });
            Thread.Sleep(200); // Give watcher time to initialize

            var testFilePath = Path.Combine(testDir, "test.txt");
            File.WriteAllText(testFilePath, "test content");
            _createdFiles.Add(testFilePath);

            // Wait for event
            var detected = eventSignal.Wait(TimeSpan.FromSeconds(5));

            // Cleanup
            await watcher.StopWatchingAsync();
            watcher.Dispose();

            // Assert
            Assert.True(detected, "File creation event was not detected");
            Assert.True(eventDetected);
        }

        [Fact]
        public async Task FileWatcher_HealthStatus_IsHealthyWhenActive()
        {
            // Arrange
            var testDir = CreateTestDirectory();
            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var watcher = new FileWatcherService(mockLogger.Object);

            // Act
            await watcher.StartWatchingAsync(new[] { testDir });
            Thread.Sleep(500); // Wait for health check

            var healthStatus = watcher.HealthStatus;
            var isWatching = watcher.IsWatching;

            // Cleanup
            await watcher.StopWatchingAsync();
            watcher.Dispose();

            // Assert
            Assert.Equal(WatcherHealthStatus.Healthy, healthStatus);
            Assert.True(isWatching);
        }

        [Fact]
        public async Task FileWatcher_FiltersTempFiles()
        {
            // Arrange
            var testDir = CreateTestDirectory();
            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var watcher = new FileWatcherService(mockLogger.Object);
            var eventsDetected = new List<FileSystemEventArgs>();

            watcher.FileChanged += (sender, e) =>
            {
                lock (eventsDetected)
                {
                    eventsDetected.Add(e);
                }
            };

            // Act
            await watcher.StartWatchingAsync(new[] { testDir });
            Thread.Sleep(200);

            var testFilePath = Path.Combine(testDir, "Thumbs.db");
            File.WriteAllText(testFilePath, "temp content");
            _createdFiles.Add(testFilePath);

            // Wait for potential events
            Thread.Sleep(1000);

            // Cleanup
            await watcher.StopWatchingAsync();
            watcher.Dispose();

            // Assert: Temporary files should be filtered out
            Assert.Empty(eventsDetected);
        }

        private string CreateTestDirectory()
        {
            var dirPath = Path.Combine(_testRootPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirPath);
            _createdDirectories.Add(dirPath);
            return dirPath;
        }

        public void Dispose()
        {
            // Clean up test files
            foreach (var file in _createdFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch { /* Ignore cleanup errors */ }
            }

            // Clean up test directories
            foreach (var dir in _createdDirectories.OrderByDescending(d => d.Length))
            {
                try
                {
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }
}
