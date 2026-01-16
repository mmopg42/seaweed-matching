using System;
using System.Collections.Generic;
using System.Linq;
using ChronoView.Core.FileMatching;
using ChronoView.Core.GroupIdGeneration;
using ChronoView.Models;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileMatching
{
    public class FileMatchingEngineTests
    {
        private Mock<IGroupIdGenerator> _mockIdGenerator;
        private DataSequenceSettings _settings;
        private DateTime _baseTime;

        public FileMatchingEngineTests()
        {
            _mockIdGenerator = new Mock<IGroupIdGenerator>();
            _mockIdGenerator.Setup(x => x.GenerateNextId(It.IsAny<int>())).Returns((int line) => $"G_{line}_{Guid.NewGuid().ToString().Substring(0, 4)}");
            
            _baseTime = new DateTime(2025, 1, 1, 12, 0, 0);
            
            _settings = new DataSequenceSettings
            {
                CompareToReferenceCamera = false,
                Sequence = new List<DataSequenceItem>
                {
                    new DataSequenceItem { Type = DataType.Normal, Order = 1, Enabled = true },
                    new DataSequenceItem { Type = DataType.Cam1, Order = 2, Enabled = true, MinDelaySeconds = 0, MaxDelaySeconds = 20 },
                    new DataSequenceItem { Type = DataType.Cam2, Order = 3, Enabled = true, MinDelaySeconds = 0, MaxDelaySeconds = 20 },
                    new DataSequenceItem { Type = DataType.Cam3, Order = 4, Enabled = true, MinDelaySeconds = 0, MaxDelaySeconds = 20 }
                }
            };
        }

        [Fact]
        public void MatchFiles_OptionDisabled_SequentialMatching()
        {
            // Arrange
            _settings.CompareToReferenceCamera = false;
            
            var unmatched = new UnmatchedFiles();
            // Normal @ T=0
            unmatched.NormalFolders["normal1"] = new Dictionary<string, string> { { $"C{_baseTime:yyyyMMdd_HHmmss}", @"C:\data\normal" } };
            
            // Cam1 @ T+5 (Matches Normal)
            unmatched.CameraFiles["cam1"] = CreateCamFile("cam1", _baseTime.AddSeconds(5));
            
            // Cam2 @ T+25 (Diff from Cam1 = 20s. Diff from Normal = 25s)
            // If Normal->Cam1 (Max 20), Cam1->Cam2 (Max 20).
            // T+5 vs T+25 is exactly 20s. Should match if Cam1 is reference.
            // T+0 vs T+25 is 25s. Would fail if Normal was reference.
            unmatched.CameraFiles["cam2"] = CreateCamFile("cam2", _baseTime.AddSeconds(25));

            // Act
            var groups = FileMatchingEngine.MatchFiles(unmatched, _settings, new HashSet<string>(), _mockIdGenerator.Object);

            // Assert
            Assert.Single(groups);
            var g = groups[0];
            Assert.True(g.CameraFiles.ContainsKey("cam1"));
            Assert.True(g.CameraFiles.ContainsKey("cam2"));
        }

        [Fact]
        public void MatchFiles_OptionEnabled_ReferenceMatching()
        {
            // Arrange
            _settings.CompareToReferenceCamera = true;
            // Configure delays: Normal->Cam1 (Max 20), Cam1->Cam2 (Max 10).
            _settings.GetByType(DataType.Cam2)!.MaxDelaySeconds = 10; 

            var unmatched = new UnmatchedFiles();
            
            // Normal @ T=0
            unmatched.NormalFolders["normal1"] = new Dictionary<string, string> { { $"C{_baseTime:yyyyMMdd_HHmmss}", @"C:\data\normal" } };

            // Cam1 @ T+5
            unmatched.CameraFiles["cam1"] = CreateCamFile("cam1", _baseTime.AddSeconds(5));

            // Cam2 @ T+15
            // Diff vs Cam1 (T+5) = 10s. (Matches MaxDelay 10)
            // Diff vs Normal (T+0) = 15s. (Fails MaxDelay 10 if falling back to Normal, but irrelevant here).
            // But wait, if Sequential, Cam2 references Cam1. T+15 vs T+5 is 10s.
            // To distinguish, we need a case where sequential FAILS but reference PASSES?
            // Or Reference PASSES and Sequential FAILS?
            
            // Case: Sequential uses Cam1. Reference uses Cam1.
            // They are the same if sequence is Normal->Cam1->Cam2.
            
            // Use non-sequential times to test.
            // Setup: Normal -> Cam1 -> Cam2.
            // Option True: Cam2 reference is Cam1.
            // Option False: Cam2 reference is Cam1.
            // THIS CONFIGURATION DOES NOT DISTINGUISH. Everything is sequential naturally here.
            
            // To distinguish, we need Cam1 to NOT be the immediate predecessor?
            // No, the sequence IS Normal->Cam1->Cam2.
            // So Cam1 IS the immediate predecessor.
            // So if Option=False, Cam2 compares to Cam1.
            // If Option=True, Cam2 compares to Cam1.
            // THE BEHAVIOR IS IDENTICAL FOR Cam2 IF Cam1 IS IMMEDIATELY PRECEDING.
            
            // Distinguishing case:
            // Sequence: Normal -> Cam2 -> Cam1. (Unlikely but possible).
            // Sequence: Normal -> Cam3 -> Cam2. Order: Normal=1, Cam3=2, Cam2=3.
            // Option False: Cam2 compares to Cam3.
            // Option True: Cam2 compares to Cam1 (which is missing/disabled? No, Cam1 must be enabled).
            
            // Let's use Sequence: Normal -> Cam1 -> Cam2 -> Cam3.
            // Test Cam3.
            // Option False: Cam3 compares to Cam2.
            // Option True: Cam3 compares to Cam1.
            
            // Setup:
            // Cam1 @ T+10.
            // Cam2 @ T+20.
            // Cam3 @ T+25.
            
            // Cam3 vs Cam2 (Diff 5s).
            // Cam3 vs Cam1 (Diff 15s).
            
            // Set MaxDelay for Cam3 = 10s.
            // If Option False (vs Cam2): 5s <= 10s. MATCH.
            // If Option True (vs Cam1): 15s > 10s. FAIL (New Group).
            
            // Let's implement this.
            
            unmatched = new UnmatchedFiles();
            unmatched.NormalFolders["normal1"] = new Dictionary<string, string> { { $"C{_baseTime:yyyyMMdd_HHmmss}", @"C:\data\normal" } };
            unmatched.CameraFiles["cam1"] = CreateCamFile("cam1", _baseTime.AddSeconds(10));
            unmatched.CameraFiles["cam2"] = CreateCamFile("cam2", _baseTime.AddSeconds(20));
            unmatched.CameraFiles["cam3"] = CreateCamFile("cam3", _baseTime.AddSeconds(25));
            
            _settings.GetByType(DataType.Cam3)!.MaxDelaySeconds = 10;
            
            // Act
            var groups = FileMatchingEngine.MatchFiles(unmatched, _settings, new HashSet<string>(), _mockIdGenerator.Object);
            
            // Assert
            Assert.Single(groups); // All in one group?
            var g = groups[0];
            Assert.True(g.CameraFiles.ContainsKey("cam1")); // T+10
            Assert.True(g.CameraFiles.ContainsKey("cam2")); // T+20 (vs Cam1 10s diff, using default 20s max? I set Cam2 max=20 earlier. 10 <= 20 OK)
            
            // Cam3 (T+25).
            // Vs Cam1 (T+10). Diff 15. Max 10. FAIL.
            Assert.False(g.CameraFiles.ContainsKey("cam3")); 
            
            // Check that separate group created for Cam3
            Assert.Equal(2, groups.Count);
            Assert.True(groups[1].CameraFiles.ContainsKey("cam3"));
        }

        [Fact]
        public void MatchFiles_Fallback_WhenCam1Missing()
        {
            // Scenario: Option Enabled. Cam1 Disabled.
            // Sequence: Normal -> Cam2.
            // Cam2 Fallback to Normal.
            
             _settings.CompareToReferenceCamera = true;
             
             // Disable Cam1
             _settings.Sequence.First(x => x.Type == DataType.Cam1).Enabled = false;
             
             var unmatched = new UnmatchedFiles();
             unmatched.NormalFolders["normal1"] = new Dictionary<string, string> { { $"C{_baseTime:yyyyMMdd_HHmmss}", @"C:\data\normal" } };
             
             // Cam2 @ T+5
             unmatched.CameraFiles["cam2"] = CreateCamFile("cam2", _baseTime.AddSeconds(5));
             
             // Act
             var groups = FileMatchingEngine.MatchFiles(unmatched, _settings, new HashSet<string>(), _mockIdGenerator.Object);
             
             // Assert
             Assert.Single(groups);
             Assert.True(groups[0].CameraFiles.ContainsKey("cam2"));
        }
        
        private List<TimestampedFile> CreateCamFile(string key, DateTime ts)
        {
            return new List<TimestampedFile> 
            { 
                new TimestampedFile 
                { 
                    FileName = $"{key}_{ts:yyyyMMdd_HHmmss}.jpg",
                    AbsolutePath = $@"C:\data\{key}\{key}_{ts:yyyyMMdd_HHmmss}.jpg",
                    Timestamp = ts
                } 
            };
        }
    }
}
