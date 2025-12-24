using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChronoView.Core.FileWatching;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileWatching
{
    /// <summary>
    /// Property-based tests for file system monitoring service
    /// **Feature: python-gui-to-csharp-migration, Property 4: Real-time File System Monitoring**
    /// **Validates: Requirements 4.1**
    /// </summary>
    public class FileSystemMonitoringPropertyTests : IDisposable
    {
        private readonly string _testRootPath;
        private readonly List<string> _createdDirectories = new();
        private readonly List<string> _createdFiles = new();

        public FileSystemMonitoringPropertyTests()
        {
            _testRootPath = Path.Combine(Path.GetTempPath(), "ChronoViewTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testRootPath);
            _createdDirectories.Add(_testRootPath);
        }

        /// <summary>
        /// Property: For any file system operation (create) in monitored directories,
        /// the system should detect and process the event within acceptable time limits
        /// </summary>
        [Property(MaxTest = 50)]
        public async Task FileWatcher_DetectsFileCreation_WithinTimeLimit(int fileNumber)
        {
            // Constrain to valid range
            fileNumber = Math.Abs(fileNumber % 1000);
            
            // Arrange
            var testDir = CreateTestDirectory();
            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var timestampCache = new FolderTimestampCache();
            var watcher = new FileWatcherService(mockLogger.Object, timestampCache);
            var eventsDetected = new List<FileSystemEventArgs>();
            var eventSignal = new ManualResetEventSlim(false);

            watcher.FileChanged += (sender, e) =>
            {
                lock (eventsDetected)
                {
                    eventsDetected.Add(e);
                    eventSignal.Set();
                }
            };

            try
            {
                // Act
                await watcher.StartWatchingAsync(new[] { testDir });
                
                // Give watcher time to initialize
                Thread.Sleep(200);

                var fileName = $"test_{fileNumber}.txt";
                var testFilePath = Path.Combine(testDir, fileName);
                var startTime = DateTime.UtcNow;
                
                File.WriteAllText(testFilePath, "test content");
                _createdFiles.Add(testFilePath);

                // Wait for event with timeout (5 seconds should be more than enough)
                var eventDetected = eventSignal.Wait(TimeSpan.FromSeconds(5));
                var detectionTime = DateTime.UtcNow - startTime;

                // Assert
                Assert.True(eventDetected, $"Event not detected within timeout. Detection time: {detectionTime.TotalMilliseconds}ms");
                Assert.True(detectionTime < TimeSpan.FromSeconds(5), $"Detection time {detectionTime.TotalMilliseconds}ms exceeds 5 seconds");
            }
            finally
            {
                // Cleanup
                await watcher.StopWatchingAsync();
                watcher.Dispose();
            }
        }

        /// <summary>
        /// Property: For any file modification in monitored directories,
        /// the system should detect the modification event
        /// </summary>
        [Property(MaxTest = 50)]
        public async Task FileWatcher_DetectsFileModification(int fileNumber)
        {
            // Constrain to valid range
            fileNumber = Math.Abs(fileNumber % 1000);
            
            // Arrange
            var testDir = CreateTestDirectory();
            var fileName = $"test_{fileNumber}.txt";
            var testFilePath = Path.Combine(testDir, fileName);
            
            // Create file before starting watcher
            File.WriteAllText(testFilePath, "initial content");
            _createdFiles.Add(testFilePath);
            Thread.Sleep(200);

            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var timestampCache = new FolderTimestampCache();
            var watcher = new FileWatcherService(mockLogger.Object, timestampCache);
            var modificationDetected = false;
            var eventSignal = new ManualResetEventSlim(false);

            watcher.FileChanged += (sender, e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    modificationDetected = true;
                    eventSignal.Set();
                }
            };

            try
            {
                // Act
                await watcher.StartWatchingAsync(new[] { testDir });
                Thread.Sleep(200);

                // Modify the file
                File.AppendAllText(testFilePath, "\nmodified content");
                
                // Wait for event
                eventSignal.Wait(TimeSpan.FromSeconds(5));

                // Assert
                Assert.True(modificationDetected, "File modification was not detected");
            }
            finally
            {
                // Cleanup
                await watcher.StopWatchingAsync();
                watcher.Dispose();
            }
        }

        /// <summary>
        /// Property: For any file deletion in monitored directories,
        /// the system should detect the deletion event
        /// </summary>
        [Property(MaxTest = 50)]
        public async Task FileWatcher_DetectsFileDeletion(int fileNumber)
        {
            // Constrain to valid range
            fileNumber = Math.Abs(fileNumber % 1000);
            
            // Arrange
            var testDir = CreateTestDirectory();
            var fileName = $"test_{fileNumber}.txt";
            var testFilePath = Path.Combine(testDir, fileName);
            
            // Create file before starting watcher
            File.WriteAllText(testFilePath, "content to delete");
            Thread.Sleep(200);

            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var timestampCache = new FolderTimestampCache();
            var watcher = new FileWatcherService(mockLogger.Object, timestampCache);
            var deletionDetected = false;
            var eventSignal = new ManualResetEventSlim(false);

            watcher.FileChanged += (sender, e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    deletionDetected = true;
                    eventSignal.Set();
                }
            };

            try
            {
                // Act
                await watcher.StartWatchingAsync(new[] { testDir });
                Thread.Sleep(200);

                // Delete the file
                File.Delete(testFilePath);
                
                // Wait for event
                eventSignal.Wait(TimeSpan.FromSeconds(5));

                // Assert
                Assert.True(deletionDetected, "File deletion was not detected");
            }
            finally
            {
                // Cleanup
                await watcher.StopWatchingAsync();
                watcher.Dispose();
            }
        }

        /// <summary>
        /// Property: The file watcher should filter out temporary files
        /// </summary>
        [Theory]
        [InlineData("~tempfile.txt")]
        [InlineData(".tmp_test.dat")]
        [InlineData("file.tmp")]
        [InlineData("Thumbs.db")]
        [InlineData("desktop.ini")]
        public async Task FileWatcher_FiltersTempFiles(string tempFileName)
        {
            // Arrange
            var testDir = CreateTestDirectory();
            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var timestampCache = new FolderTimestampCache();
            var watcher = new FileWatcherService(mockLogger.Object, timestampCache);
            var eventsDetected = new List<FileSystemEventArgs>();

            watcher.FileChanged += (sender, e) =>
            {
                lock (eventsDetected)
                {
                    eventsDetected.Add(e);
                }
            };

            try
            {
                // Act
                await watcher.StartWatchingAsync(new[] { testDir });
                Thread.Sleep(200);

                var testFilePath = Path.Combine(testDir, tempFileName);
                File.WriteAllText(testFilePath, "temp content");
                _createdFiles.Add(testFilePath);

                // Wait for potential events
                Thread.Sleep(1000);

                // Assert: Temporary files should be filtered out
                Assert.Empty(eventsDetected);
            }
            finally
            {
                // Cleanup
                await watcher.StopWatchingAsync();
                watcher.Dispose();
            }
        }

        /// <summary>
        /// Property: The file watcher health status should be Healthy when all watchers are active
        /// </summary>
        [Property(MaxTest = 30)]
        public async Task FileWatcher_HealthStatus_IsHealthyWhenActive(int dirCount)
        {
            // Constrain to valid range (1-3 directories)
            dirCount = Math.Clamp(Math.Abs(dirCount % 10), 1, 3);
            
            // Arrange
            var testDirs = Enumerable.Range(0, dirCount)
                .Select(_ => CreateTestDirectory())
                .ToList();

            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var timestampCache = new FolderTimestampCache();
            var watcher = new FileWatcherService(mockLogger.Object, timestampCache);

            try
            {
                // Act
                await watcher.StartWatchingAsync(testDirs);
                Thread.Sleep(500); // Wait for health check

                var healthStatus = watcher.HealthStatus;
                var isWatching = watcher.IsWatching;

                // Assert
                Assert.Equal(WatcherHealthStatus.Healthy, healthStatus);
                Assert.True(isWatching);
            }
            finally
            {
                // Cleanup
                await watcher.StopWatchingAsync();
                watcher.Dispose();
            }
        }

        /// <summary>
        /// Property: Starting and stopping the watcher should work correctly
        /// </summary>
        [Property(MaxTest = 30)]
        public async Task FileWatcher_StartStop_WorksCorrectly(int iterations)
        {
            // Constrain to valid range (1-3 iterations)
            iterations = Math.Clamp(Math.Abs(iterations % 10), 1, 3);
            
            // Arrange
            var testDir = CreateTestDirectory();
            var mockLogger = new Mock<ILogger<FileWatcherService>>();
            var timestampCache = new FolderTimestampCache();
            var watcher = new FileWatcherService(mockLogger.Object, timestampCache);

            try
            {
                // Act & Assert
                for (int i = 0; i < iterations; i++)
                {
                    await watcher.StartWatchingAsync(new[] { testDir });
                    Assert.True(watcher.IsWatching, $"Watcher should be watching after start (iteration {i})");
                    
                    Thread.Sleep(100);
                    
                    await watcher.StopWatchingAsync();
                    Assert.False(watcher.IsWatching, $"Watcher should not be watching after stop (iteration {i})");
                    
                    Thread.Sleep(100);
                }
            }
            finally
            {
                // Cleanup
                if (watcher.IsWatching)
                {
                    await watcher.StopWatchingAsync();
                }
                watcher.Dispose();
            }
        }

        #region Helper Methods

        private string CreateTestDirectory()
        {
            var dirPath = Path.Combine(_testRootPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dirPath);
            _createdDirectories.Add(dirPath);
            return dirPath;
        }

        #endregion

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
