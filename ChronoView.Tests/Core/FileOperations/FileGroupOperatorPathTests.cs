using ChronoView.Core.FileOperations;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Xunit;

namespace ChronoView.Tests.Core.FileOperations;

public class FileGroupOperatorPathTests
{
    private readonly FileGroupOperator _operator;
    private readonly Mock<ILogger<FileGroupOperator>> _loggerMock;
    private readonly MethodInfo _buildPathMethod;
    private readonly MethodInfo _hasValidDateSegmentMethod;
    private readonly MethodInfo _ensureDateRootMethod;

    public FileGroupOperatorPathTests()
    {
        _loggerMock = new Mock<ILogger<FileGroupOperator>>();
        _operator = new FileGroupOperator(_loggerMock.Object);

        // Get private methods using reflection
        var type = typeof(FileGroupOperator);
        _buildPathMethod = type.GetMethod("BuildPath", BindingFlags.NonPublic | BindingFlags.Instance);
        _hasValidDateSegmentMethod = type.GetMethod("HasValidDateSegment", BindingFlags.NonPublic | BindingFlags.Static);
        _ensureDateRootMethod = type.GetMethod("EnsureDateRoot", BindingFlags.NonPublic | BindingFlags.Static);

        if (_buildPathMethod == null || _hasValidDateSegmentMethod == null || _ensureDateRootMethod == null)
        {
            throw new InvalidOperationException("Required private methods not found. Test setup may need adjustment.");
        }
    }

    [Fact]
    public void HasValidDateSegment_NoDate_ReturnsFalse()
    {
        var path = @"C:\Data\Output";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.False(result);
    }

    [Fact]
    public void HasValidDateSegment_ValidDateInMiddle_ReturnsTrue()
    {
        var path = @"C:\Data\20260107\Output\Backup";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.True(result);
    }

    [Fact]
    public void HasValidDateSegment_InvalidDate_ReturnsFalse()
    {
        var path = @"C:\Data\20261345\Output";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.False(result);
    }

    [Fact]
    public void HasValidDateSegment_MultipleDates_ReturnsTrue()
    {
        var path = @"C:\Data\20260101\Output\20260107\Backup";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.True(result);
    }

    [Fact]
    public void HasValidDateSegment_DateAtStart_ReturnsTrue()
    {
        var path = @"20260107\Output\Backup";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.True(result);
    }

    [Fact]
    public void HasValidDateSegment_DateAtEnd_ReturnsTrue()
    {
        var path = @"C:\Data\Output\20260107";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.True(result);
    }

    [Fact]
    public void HasValidDateSegment_UnixPath_ReturnsTrue()
    {
        var path = "/data/20260107/output";
        var result = (bool)_hasValidDateSegmentMethod.Invoke(null, new object[] { path });
        Assert.True(result);
    }

    [Fact]
    public void EnsureDateRoot_NoDate_AddsToday()
    {
        var basePath = @"C:\Data\Output";
        var result = (string)_ensureDateRootMethod.Invoke(null, new object[] { basePath });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EnsureDateRoot_HasDate_ReturnsOriginal()
    {
        var basePath = @"C:\Data\20260107\Output";
        var result = (string)_ensureDateRootMethod.Invoke(null, new object[] { basePath });
        
        Assert.Equal(basePath, result);
    }

    [Fact]
    public void EnsureDateRoot_InvalidDate_AddsToday()
    {
        var basePath = @"C:\Data\20261345\Output";
        var result = (string)_ensureDateRootMethod.Invoke(null, new object[] { basePath });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today);
        
        Assert.Equal(expected, result);
    }

    private FileGroup CreateTestGroup(int lineNumber = 1, bool hasNir = false)
    {
        return new FileGroup
        {
            GroupId = "test-group",
            LineNumber = lineNumber,
            HasNir = hasNir,
            NormalFolder = @"C:\Source\Normal",
            NirFilePath = hasNir ? @"C:\Source\NIR\test.spc" : null,
            CameraFiles = new Dictionary<string, string>
            {
                { "cam1", @"C:\Source\Cam1\test.jpg" }
            },
            CreatedAt = DateTime.Now,
            Timestamp = DateTime.Now
        };
    }

    [Fact]
    public void BuildPath_MoveSchema_NoDate_AddsToday()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Output";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반", PathSchema.MoveSchema, subject, null 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today, subject, "without NIR", "일반 카메라");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_MoveSchema_HasDate_NoDateAdded()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: true);
        var basePath = @"C:\Data\20260107\Output";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반", PathSchema.MoveSchema, subject, null 
        });
        
        // Should NOT add today's date since 20260107 is already in the path
        var expected = Path.Combine(basePath, subject, "with NIR", "일반");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_MoveSchema_WithFolderName()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Output";
        var subject = "TestSubject";
        var folderName = "20251204_143052_0";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반", PathSchema.MoveSchema, subject, folderName 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseRolePath = Path.Combine(basePath, today, subject, "without NIR", "일반 카메라");
        var expected = Path.Combine(baseRolePath, folderName);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_NoDate_AddsToday()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Quarantine";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반1", PathSchema.QuarantineSchema, subject, null 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        // QuarantineSchema: baseRoot/Line{N}/subject/role
        var expected = Path.Combine(basePath, today, "Line1", subject, "일반1");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_HasDate_NoDateAdded()
    {
        var group = CreateTestGroup(lineNumber: 2, hasNir: true);
        var basePath = @"C:\Data\20260107\Quarantine";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "nir2", PathSchema.QuarantineSchema, subject, null 
        });
        
        // Should NOT add today's date since 20260107 is already in the path
        // QuarantineSchema: baseRoot/Line{N}/subject/role
        var expected = Path.Combine(basePath, "Line2", subject, "nir2");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_WithFolderName()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Quarantine";
        var subject = "TestSubject";
        var folderName = "20251204_143052_0";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반1", PathSchema.QuarantineSchema, subject, folderName 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseQuarantinePath = Path.Combine(basePath, today, "Line1", subject, "일반1");
        var expected = Path.Combine(baseQuarantinePath, folderName);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_Line2()
    {
        var group = CreateTestGroup(lineNumber: 2, hasNir: false);
        var basePath = @"C:\Data\Quarantine";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반2", PathSchema.QuarantineSchema, subject, null 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today, "Line2", subject, "일반2");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_MoveSchema_CameraRole()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Output";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "cam1", PathSchema.MoveSchema, subject, null 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today, subject, "without NIR", "복합 카메라", "cam1");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_MoveSchema_DateInMiddle_NoDateAdded()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: true);
        var basePath = @"C:\Data\20260107\Output\Backup";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "Nir", PathSchema.MoveSchema, subject, null 
        });
        
        // Should NOT add today's date since 20260107 is already in the path
        var expected = Path.Combine(basePath, subject, "with NIR", "Nir");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_DateInMiddle_NoDateAdded()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\20260107\Quarantine\Backup";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반1", PathSchema.QuarantineSchema, subject, null 
        });
        
        // Should NOT add today's date since 20260107 is already in the path
        var expected = Path.Combine(basePath, "Line1", subject, "일반1");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_MoveSchema_InvalidDate_AddsToday()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\20261345\Output";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반", PathSchema.MoveSchema, subject, null 
        });
        
        // Invalid date (20261345) should be ignored, today's date should be added
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today, subject, "without NIR", "일반 카메라");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_MultipleDates_NoDateAdded()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\20260101\Output\20260107\Backup";
        var subject = "TestSubject";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반1", PathSchema.QuarantineSchema, subject, null 
        });
        
        // Should NOT add today's date since valid dates exist in the path
        var expected = Path.Combine(basePath, "Line1", subject, "일반1");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_MoveSchema_NoSubject_UsesUnknownSubject()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Output";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반", PathSchema.MoveSchema, null, null 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today, "UnknownSubject", "without NIR", "일반 카메라");
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildPath_QuarantineSchema_NoSubject_UsesUnknownSubject()
    {
        var group = CreateTestGroup(lineNumber: 1, hasNir: false);
        var basePath = @"C:\Data\Quarantine";
        
        var result = (string)_buildPathMethod.Invoke(_operator, new object[] 
        { 
            basePath, group, "일반1", PathSchema.QuarantineSchema, null, null 
        });
        
        var today = DateTime.Now.ToString("yyyyMMdd");
        var expected = Path.Combine(basePath, today, "Line1", "UnknownSubject", "일반1");
        
        Assert.Equal(expected, result);
    }
}





