using System.Text.Json;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Models;

/// <summary>
/// Unit tests for data model serialization, validation, and equality.
/// Tests Requirements 1.1 and 1.5.
/// </summary>
public class DataModelTests
{
    #region FileGroup Tests

    [Fact]
    public void FileGroup_IsValid_ReturnsTrueForValidGroup()
    {
        // Arrange
        var fileGroup = new FileGroup
        {
            GroupId = "group-001",
            NirKey = "nir-001",
            NormalFolder = "/path/to/folder",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete
        };

        // Act
        var isValid = fileGroup.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void FileGroup_IsValid_ReturnsFalseForEmptyGroupId()
    {
        // Arrange
        var fileGroup = new FileGroup
        {
            GroupId = "",
            NirKey = "nir-001",
            LineNumber = 1
        };

        // Act
        var isValid = fileGroup.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void FileGroup_IsValid_ReturnsFalseForNegativeLineNumber()
    {
        // Arrange
        var fileGroup = new FileGroup
        {
            GroupId = "group-001",
            LineNumber = -1
        };

        // Act
        var isValid = fileGroup.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void FileGroup_IsValid_ReturnsFalseWhenHasNirTrueButNirKeyEmpty()
    {
        // Arrange
        var fileGroup = new FileGroup
        {
            GroupId = "group-001",
            HasNir = true,
            NirKey = "",
            LineNumber = 1
        };

        // Act
        var isValid = fileGroup.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void FileGroup_Equality_ReturnsTrueForIdenticalGroups()
    {
        // Arrange
        var group1 = new FileGroup
        {
            GroupId = "group-001",
            NirKey = "nir-001",
            NormalFolder = "/path/to/folder",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete,
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            CameraFiles = new Dictionary<string, string>
            {
                { "cam1", "/path/to/cam1.jpg" },
                { "cam2", "/path/to/cam2.jpg" }
            }
        };

        var group2 = new FileGroup
        {
            GroupId = "group-001",
            NirKey = "nir-001",
            NormalFolder = "/path/to/folder",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete,
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            CameraFiles = new Dictionary<string, string>
            {
                { "cam1", "/path/to/cam1.jpg" },
                { "cam2", "/path/to/cam2.jpg" }
            }
        };

        // Act & Assert
        Assert.Equal(group1, group2);
        Assert.True(group1 == group2);
        Assert.False(group1 != group2);
        Assert.Equal(group1.GetHashCode(), group2.GetHashCode());
    }

    [Fact]
    public void FileGroup_Equality_ReturnsFalseForDifferentGroups()
    {
        // Arrange
        var group1 = new FileGroup { GroupId = "group-001", LineNumber = 1 };
        var group2 = new FileGroup { GroupId = "group-002", LineNumber = 1 };

        // Act & Assert
        Assert.NotEqual(group1, group2);
        Assert.False(group1 == group2);
        Assert.True(group1 != group2);
    }

    [Fact]
    public void FileGroup_Serialization_RoundTripPreservesData()
    {
        // Arrange
        var original = new FileGroup
        {
            GroupId = "group-001",
            NirKey = "nir-001",
            NormalFolder = "/path/to/folder",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete,
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            CameraFiles = new Dictionary<string, string>
            {
                { "cam1", "/path/to/cam1.jpg" },
                { "cam2", "/path/to/cam2.jpg" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<FileGroup>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
    }

    #endregion

    #region UnmatchedFiles Tests

    [Fact]
    public void UnmatchedFiles_IsValid_ReturnsTrueForValidData()
    {
        // Arrange
        var unmatchedFiles = new UnmatchedFiles
        {
            NirFiles = new Dictionary<string, Dictionary<string, string>>
            {
                { "nir", new Dictionary<string, string>
                    {
                        { "nir1", "/path/to/nir1.spc" }
                    }
                }
            },
            NormalFolders = new Dictionary<string, Dictionary<string, string>>
            {
                { "normal", new Dictionary<string, string>
                    {
                        { "folder1", "/path/to/folder1" }
                    }
                }
            },
            CameraFiles = new Dictionary<string, List<TimestampedFile>>
            {
                {
                    "cam1",
                    new List<TimestampedFile>
                    {
                        new TimestampedFile
                        {
                            FileName = "image.jpg",
                            AbsolutePath = "/path/to/image.jpg",
                            Timestamp = DateTime.UtcNow
                        }
                    }
                }
            }
        };

        // Act
        var isValid = unmatchedFiles.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void UnmatchedFiles_IsValid_ReturnsFalseForEmptyKeys()
    {
        // Arrange
        var unmatchedFiles = new UnmatchedFiles
        {
            NirFiles = new Dictionary<string, Dictionary<string, string>>
            {
                { "nir", new Dictionary<string, string>
                    {
                        { "", "/path/to/nir1.spc" }
                    }
                }
            }
        };

        // Act
        var isValid = unmatchedFiles.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void UnmatchedFiles_TotalCount_ReturnsCorrectSum()
    {
        // Arrange
        var unmatchedFiles = new UnmatchedFiles
        {
            NirFiles = new Dictionary<string, Dictionary<string, string>>
            {
                { "nir", new Dictionary<string, string>
                    {
                        { "nir1", "/path/to/nir1.spc" },
                        { "nir2", "/path/to/nir2.spc" }
                    }
                }
            },
            NormalFolders = new Dictionary<string, Dictionary<string, string>>
            {
                { "normal", new Dictionary<string, string>
                    {
                        { "folder1", "/path/to/folder1" }
                    }
                }
            },
            CameraFiles = new Dictionary<string, List<TimestampedFile>>
            {
                {
                    "cam1",
                    new List<TimestampedFile>
                    {
                        new TimestampedFile { FileName = "image1.jpg", AbsolutePath = "/path/to/image1.jpg", Timestamp = DateTime.UtcNow },
                        new TimestampedFile { FileName = "image2.jpg", AbsolutePath = "/path/to/image2.jpg", Timestamp = DateTime.UtcNow }
                    }
                }
            }
        };

        // Act
        var totalCount = unmatchedFiles.TotalCount;

        // Assert
        Assert.Equal(5, totalCount); // 2 NIR + 1 folder + 2 camera files
    }

    [Fact]
    public void UnmatchedFiles_Clear_RemovesAllFiles()
    {
        // Arrange
        var unmatchedFiles = new UnmatchedFiles
        {
            NirFiles = new Dictionary<string, Dictionary<string, string>> 
            { 
                { "nir", new Dictionary<string, string> { { "nir1", "/path" } } }
            },
            NormalFolders = new Dictionary<string, Dictionary<string, string>> 
            { 
                { "normal", new Dictionary<string, string> { { "folder1", "/path" } } }
            },
            CameraFiles = new Dictionary<string, List<TimestampedFile>>
            {
                { "cam1", new List<TimestampedFile> { new TimestampedFile { FileName = "img.jpg", AbsolutePath = "/path" } } }
            }
        };

        // Act
        unmatchedFiles.Clear();

        // Assert
        Assert.Empty(unmatchedFiles.NirFiles);
        Assert.Empty(unmatchedFiles.NormalFolders);
        Assert.Empty(unmatchedFiles.CameraFiles);
        Assert.Equal(0, unmatchedFiles.TotalCount);
    }

    [Fact]
    public void UnmatchedFiles_Serialization_RoundTripPreservesData()
    {
        // Arrange
        var original = new UnmatchedFiles
        {
            NirFiles = new Dictionary<string, Dictionary<string, string>>
            {
                { "nir", new Dictionary<string, string>
                    {
                        { "nir1", "/path/to/nir1.spc" }
                    }
                }
            },
            NormalFolders = new Dictionary<string, Dictionary<string, string>>
            {
                { "normal", new Dictionary<string, string>
                    {
                        { "folder1", "/path/to/folder1" }
                    }
                }
            },
            CameraFiles = new Dictionary<string, List<TimestampedFile>>
            {
                {
                    "cam1",
                    new List<TimestampedFile>
                    {
                        new TimestampedFile
                        {
                            FileName = "image.jpg",
                            AbsolutePath = "/path/to/image.jpg",
                            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
                        }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<UnmatchedFiles>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
    }

    #endregion

    #region TimestampedFile Tests

    [Fact]
    public void TimestampedFile_IsValid_ReturnsTrueForValidFile()
    {
        // Arrange
        var file = new TimestampedFile
        {
            FileName = "file.jpg",
            AbsolutePath = "/path/to/file.jpg",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var isValid = file.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void TimestampedFile_IsValid_ReturnsFalseForEmptyPath()
    {
        // Arrange
        var file = new TimestampedFile
        {
            FilePath = "",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var isValid = file.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void TimestampedFile_Equality_WorksCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var file1 = new TimestampedFile { FilePath = "/path/to/file.jpg", Timestamp = timestamp };
        var file2 = new TimestampedFile { FilePath = "/path/to/file.jpg", Timestamp = timestamp };
        var file3 = new TimestampedFile { FilePath = "/path/to/other.jpg", Timestamp = timestamp };

        // Act & Assert
        Assert.Equal(file1, file2);
        Assert.NotEqual(file1, file3);
        Assert.Equal(file1.GetHashCode(), file2.GetHashCode());
    }

    #endregion

    #region ImageMetadata Tests

    [Fact]
    public void ImageMetadata_IsValid_ReturnsTrueForValidMetadata()
    {
        // Arrange
        var metadata = new ImageMetadata
        {
            Width = 1920,
            Height = 1080,
            FileSize = 1024000,
            Format = "JPEG",
            FilePath = "/path/to/image.jpg",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var isValid = metadata.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ImageMetadata_IsValid_ReturnsFalseForInvalidDimensions()
    {
        // Arrange
        var metadata = new ImageMetadata
        {
            Width = 0,
            Height = 1080,
            FileSize = 1024000,
            Format = "JPEG",
            FilePath = "/path/to/image.jpg"
        };

        // Act
        var isValid = metadata.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ImageMetadata_IsValid_ReturnsFalseForNegativeFileSize()
    {
        // Arrange
        var metadata = new ImageMetadata
        {
            Width = 1920,
            Height = 1080,
            FileSize = -1,
            Format = "JPEG",
            FilePath = "/path/to/image.jpg"
        };

        // Act
        var isValid = metadata.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ImageMetadata_AspectRatio_CalculatesCorrectly()
    {
        // Arrange
        var metadata = new ImageMetadata
        {
            Width = 1920,
            Height = 1080
        };

        // Act
        var aspectRatio = metadata.AspectRatio;

        // Assert
        Assert.Equal(1920.0 / 1080.0, aspectRatio, precision: 5);
    }

    [Fact]
    public void ImageMetadata_Equality_WorksCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var metadata1 = new ImageMetadata
        {
            Width = 1920,
            Height = 1080,
            FileSize = 1024000,
            Format = "JPEG",
            FilePath = "/path/to/image.jpg",
            CreatedAt = timestamp,
            IsAbnormal = false
        };

        var metadata2 = new ImageMetadata
        {
            Width = 1920,
            Height = 1080,
            FileSize = 1024000,
            Format = "JPEG",
            FilePath = "/path/to/image.jpg",
            CreatedAt = timestamp,
            IsAbnormal = false
        };

        // Act & Assert
        Assert.Equal(metadata1, metadata2);
        Assert.Equal(metadata1.GetHashCode(), metadata2.GetHashCode());
    }

    [Fact]
    public void ImageMetadata_Serialization_RoundTripPreservesData()
    {
        // Arrange
        var original = new ImageMetadata
        {
            Width = 1920,
            Height = 1080,
            FileSize = 1024000,
            Format = "JPEG",
            FilePath = "/path/to/image.jpg",
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            IsAbnormal = true
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ImageMetadata>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
    }

    #endregion

    #region NirSpectrum Tests

    [Fact]
    public void NirSpectrum_IsValid_ReturnsTrueForValidSpectrum()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            FileName = "spectrum.spc",
            FilePath = "/path/to/spectrum.spc",
            Timestamp = DateTime.UtcNow,
            Wavelengths = new double[] { 4500, 5000, 5500, 6000, 6500 },
            Intensities = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 }
        };

        // Act
        var isValid = spectrum.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void NirSpectrum_IsValid_ReturnsFalseForEmptyFileName()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            FileName = "",
            FilePath = "/path/to/spectrum.spc",
            Wavelengths = new double[] { 4500, 5000 },
            Intensities = new double[] { 0.1, 0.2 }
        };

        // Act
        var isValid = spectrum.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void NirSpectrum_IsValid_ReturnsFalseForMismatchedArrayLengths()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            FileName = "spectrum.spc",
            FilePath = "/path/to/spectrum.spc",
            Wavelengths = new double[] { 4500, 5000, 5500 },
            Intensities = new double[] { 0.1, 0.2 }
        };

        // Act
        var isValid = spectrum.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void NirSpectrum_IsValid_ReturnsFalseForNaNValues()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            FileName = "spectrum.spc",
            FilePath = "/path/to/spectrum.spc",
            Wavelengths = new double[] { 4500, double.NaN, 5500 },
            Intensities = new double[] { 0.1, 0.2, 0.3 }
        };

        // Act
        var isValid = spectrum.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void NirSpectrum_DataPointCount_ReturnsCorrectValue()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            Wavelengths = new double[] { 4500, 5000, 5500 },
            Intensities = new double[] { 0.1, 0.2, 0.3 }
        };

        // Act
        var count = spectrum.DataPointCount;

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void NirSpectrum_WavelengthRange_CalculatesCorrectly()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            Wavelengths = new double[] { 4500, 5000, 5500, 6000, 6500 }
        };

        // Act
        var range = spectrum.WavelengthRange;

        // Assert
        Assert.Equal(4500, range.Min);
        Assert.Equal(6500, range.Max);
    }

    [Fact]
    public void NirSpectrum_IntensityRange_CalculatesCorrectly()
    {
        // Arrange
        var spectrum = new NirSpectrum
        {
            Intensities = new double[] { 0.1, 0.5, 0.3, 0.8, 0.2 }
        };

        // Act
        var range = spectrum.IntensityRange;

        // Assert
        Assert.Equal(0.1, range.Min);
        Assert.Equal(0.8, range.Max);
    }

    [Fact]
    public void NirSpectrum_Equality_WorksCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var spectrum1 = new NirSpectrum
        {
            FileName = "spectrum.spc",
            FilePath = "/path/to/spectrum.spc",
            Timestamp = timestamp,
            Wavelengths = new double[] { 4500, 5000, 5500 },
            Intensities = new double[] { 0.1, 0.2, 0.3 },
            IsAbnormal = false,
            YVariation = 0.05
        };

        var spectrum2 = new NirSpectrum
        {
            FileName = "spectrum.spc",
            FilePath = "/path/to/spectrum.spc",
            Timestamp = timestamp,
            Wavelengths = new double[] { 4500, 5000, 5500 },
            Intensities = new double[] { 0.1, 0.2, 0.3 },
            IsAbnormal = false,
            YVariation = 0.05
        };

        // Act & Assert
        Assert.Equal(spectrum1, spectrum2);
        Assert.Equal(spectrum1.GetHashCode(), spectrum2.GetHashCode());
    }

    [Fact]
    public void NirSpectrum_Serialization_RoundTripPreservesData()
    {
        // Arrange
        var original = new NirSpectrum
        {
            FileName = "spectrum.spc",
            FilePath = "/path/to/spectrum.spc",
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Wavelengths = new double[] { 4500, 5000, 5500, 6000, 6500 },
            Intensities = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 },
            IsAbnormal = true,
            YVariation = 0.08,
            Metadata = new Dictionary<string, object>
            {
                { "instrument", "NIR-2000" },
                { "operator", "John Doe" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<NirSpectrum>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original, deserialized);
    }

    #endregion
}
