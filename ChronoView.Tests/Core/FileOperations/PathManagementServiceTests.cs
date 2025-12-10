using ChronoView.Core.FileOperations;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.Core.FileOperations;

/// <summary>
/// Unit tests for PathManagementService.
/// </summary>
public class PathManagementServiceTests
{
    private readonly Mock<ILogger<PathManagementService>> _mockLogger;
    private readonly PathManagementService _service;

    public PathManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<PathManagementService>>();
        _service = new PathManagementService(_mockLogger.Object);
    }

    [Fact]
    public void GeneratePathsFromDate_ValidDate_ReturnsCorrectPaths()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            BasePath = "D:/TestData"
        };
        var dateString = "20251204";

        // Act
        var result = _service.GeneratePathsFromDate(dateString, config);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(11, result.Count); // NIR1, Normal1, Cam1-3, NIR2, Normal2, Cam4-6, Output
        
        // Line 1 경로 검증
        Assert.Contains("NIR1", result.Keys);
        Assert.Contains("Normal1", result.Keys);
        Assert.Contains("Cam1", result.Keys);
        Assert.Contains("Cam2", result.Keys);
        Assert.Contains("Cam3", result.Keys);
        
        // Line 2 경로 검증
        Assert.Contains("NIR2", result.Keys);
        Assert.Contains("Normal2", result.Keys);
        Assert.Contains("Cam4", result.Keys);
        Assert.Contains("Cam5", result.Keys);
        Assert.Contains("Cam6", result.Keys);
        
        // Output 경로 검증
        Assert.Contains("Output", result.Keys);
        
        // 경로 형식 검증 (예: D:/TestData/2025/12/04/NIR1)
        Assert.Contains("2025", result["NIR1"]);
        Assert.Contains("12", result["NIR1"]);
        Assert.Contains("04", result["NIR1"]);
        Assert.EndsWith("NIR1", result["NIR1"]);
    }

    [Fact]
    public void GeneratePathsFromDate_InvalidDate_ReturnsEmptyDictionary()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        var invalidDate = "invalid";

        // Act
        var result = _service.GeneratePathsFromDate(invalidDate, config);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GeneratePathsFromDate_NoBasePath_UsesDefaultPath()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            BasePath = "" // Empty BasePath should use default
        };
        var dateString = "20251204";

        // Act
        var result = _service.GeneratePathsFromDate(dateString, config);

        // Assert
        Assert.NotEmpty(result);
        // 기본 경로 "D:/Data" 사용 확인
        Assert.Contains("D:/Data", result["NIR1"]);
    }

    [Fact]
    public void ValidatePaths_ValidPaths_ReturnsTrue()
    {
        // Arrange
        var paths = new Dictionary<string, string>
        {
            { "NIR1", "D:/Data/2025/12/04/NIR1" },
            { "Normal1", "D:/Data/2025/12/04/Normal1" },
            { "Cam1", "D:/Data/2025/12/04/Cam1" }
        };

        // Act
        var result = _service.ValidatePaths(paths);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePaths_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var paths = new Dictionary<string, string>
        {
            { "NIR1", "" },
            { "Normal1", "D:/Data/2025/12/04/Normal1" }
        };

        // Act
        var result = _service.ValidatePaths(paths);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePaths_NullPaths_ReturnsFalse()
    {
        // Act
        var result = _service.ValidatePaths(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePaths_EmptyDictionary_ReturnsFalse()
    {
        // Arrange
        var paths = new Dictionary<string, string>();

        // Act
        var result = _service.ValidatePaths(paths);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateSampleFoldersAsync_WithValidPaths_CreatesDirectories()
    {
        // Arrange
        var tempBasePath = Path.Combine(Path.GetTempPath(), $"ChronoViewTest_{Guid.NewGuid()}");
        var sampleName = "TestSample";
        
        var config = new ApplicationConfiguration
        {
            MatchingSettings = new MatchingSettings
            {
                Nir1Path = Path.Combine(tempBasePath, "NIR1"),
                Normal1Path = Path.Combine(tempBasePath, "Normal1"),
                Camera1Path = Path.Combine(tempBasePath, "Cam1"),
                Nir2Path = Path.Combine(tempBasePath, "NIR2"),
                Normal2Path = Path.Combine(tempBasePath, "Normal2"),
                Camera4Path = Path.Combine(tempBasePath, "Cam4")
            }
        };

        try
        {
            // 기본 디렉토리 생성
            Directory.CreateDirectory(config.MatchingSettings.Nir1Path);
            Directory.CreateDirectory(config.MatchingSettings.Normal1Path);
            Directory.CreateDirectory(config.MatchingSettings.Camera1Path);

            // Act
            var result = await _service.CreateSampleFoldersAsync(sampleName, config);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(Path.Combine(config.MatchingSettings.Nir1Path, sampleName)));
            Assert.True(Directory.Exists(Path.Combine(config.MatchingSettings.Normal1Path, sampleName)));
            Assert.True(Directory.Exists(Path.Combine(config.MatchingSettings.Camera1Path, sampleName)));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempBasePath))
            {
                Directory.Delete(tempBasePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CreateSampleFoldersAsync_ExistingFolders_SkipsCreation()
    {
        // Arrange
        var tempBasePath = Path.Combine(Path.GetTempPath(), $"ChronoViewTest_{Guid.NewGuid()}");
        var sampleName = "TestSample";
        
        var config = new ApplicationConfiguration
        {
            MatchingSettings = new MatchingSettings
            {
                Nir1Path = Path.Combine(tempBasePath, "NIR1")
            }
        };

        try
        {
            // 기본 디렉토리 및 샘플 폴더 미리 생성
            var samplePath = Path.Combine(config.MatchingSettings.Nir1Path, sampleName);
            Directory.CreateDirectory(samplePath);

            // Act
            var result = await _service.CreateSampleFoldersAsync(sampleName, config);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(samplePath)); // 여전히 존재해야 함
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempBasePath))
            {
                Directory.Delete(tempBasePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CreateSampleFoldersAsync_CancellationRequested_ReturnsFalse()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            MatchingSettings = new MatchingSettings
            {
                Nir1Path = "D:/TestPath"
            }
        };
        var sampleName = "TestSample";
        var cts = new CancellationTokenSource();
        cts.Cancel(); // 즉시 취소

        // Act
        var result = await _service.CreateSampleFoldersAsync(sampleName, config, cts.Token);

        // Assert
        Assert.False(result);
    }
}

