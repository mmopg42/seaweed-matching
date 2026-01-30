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
    public void GeneratePathsFromDate_ReturnsConfiguredPaths()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            MatchingSettings = new MatchingSettings
            {
                Nir1Path = "D:/TestRoot/20260128/NIR1",
                Normal1Path = "D:/TestRoot/20260128/Normal1",
                Camera1Path = "D:/TestRoot/20260128/Cam1",
                Camera2Path = "D:/TestRoot/20260128/Cam2",
                Camera3Path = "D:/TestRoot/20260128/Cam3",
                Nir2Path = "D:/TestRoot/20260128/NIR2",
                Normal2Path = "D:/TestRoot/20260128/Normal2",
                Camera4Path = "D:/TestRoot/20260128/Cam4",
                Camera5Path = "D:/TestRoot/20260128/Cam5",
                Camera6Path = "D:/TestRoot/20260128/Cam6",
                OutputPath = "D:/TestRoot/20260128/Output"
            }
        };
        var dateString = "20260128";

        // Act
        var result = _service.GeneratePathsFromDate(dateString, config);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(11, result.Count); // NIR1, Normal1, Cam1-3, NIR2, Normal2, Cam4-6, Output

        // 모든 경로가 설정된 값 그대로 반환되는지 검증
        Assert.Equal("D:/TestRoot/20260128/NIR1", result["NIR1"]);
        Assert.Equal("D:/TestRoot/20260128/Normal1", result["Normal1"]);
        Assert.Equal("D:/TestRoot/20260128/Cam1", result["Cam1"]);
        Assert.Equal("D:/TestRoot/20260128/NIR2", result["NIR2"]);
        Assert.Equal("D:/TestRoot/20260128/Output", result["Output"]);
    }

    [Fact]
    public void GeneratePathsFromDate_EmptyConfig_ReturnsEmptyPaths()
    {
        // Arrange
        var config = new ApplicationConfiguration(); // All paths are empty by default
        var dateString = "20260128";

        // Act
        var result = _service.GeneratePathsFromDate(dateString, config);

        // Assert
        Assert.Equal(11, result.Count);
        // 모든 경로가 빈 문자열이어야 함
        Assert.All(result.Values, v => Assert.Equal("", v));
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

