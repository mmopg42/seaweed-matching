using ChronoView.Core.FileOperations;
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ChronoView.Tests.Core.FileOperations;

public class FileOperationServiceTests : IDisposable
{
    private readonly FileOperationService _service;
    private readonly Mock<ILogger<FileOperationService>> _loggerMock;
    private readonly string _testBaseDir;
    private readonly string _sourceDir;
    private readonly string _destDir;

    public FileOperationServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileOperationService>>();
        _service = new FileOperationService(_loggerMock.Object);

        // Setup temporary locations
        _testBaseDir = Path.Combine(Path.GetTempPath(), "ChronoViewTests", Guid.NewGuid().ToString());
        _sourceDir = Path.Combine(_testBaseDir, "Source");
        _destDir = Path.Combine(_testBaseDir, "Dest");

        Directory.CreateDirectory(_sourceDir);
        // Dest dir will be created by the service or test
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBaseDir))
                Directory.Delete(_testBaseDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task Rollback_ShouldRestoreSource_WhenSourceDeleted()
    {
        // 1. Arrange
        // Create 2 files in source
        var file1 = Path.Combine(_sourceDir, "file1.txt");
        var file2 = Path.Combine(_sourceDir, "file2.txt");
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        var fileGroup = new FileGroup
        {
            GroupId = "TestGroup",
            NirFilePath = file1, // Just using for property holding
            // We need to manipulate GetAllFilePaths behavior or use valid paths
            // FileGroup usually doesn't hold list, it scans. 
            // BUT FileOperationService calls group.GetAllFilePaths().
            // We need to ensure FileGroup can return these files.
            // Let's rely on FileGroup implementation: it usually returns specific properties.
            // If FileGroup logic is complex, we might need to mock it or setup paths correctly.
        };
        
        // Let's check FileGroup implementation or create a dummy sub-class if needed, 
        // but FileOperationService takes 'FileGroup' type.
        // Assuming FileGroup has properties: NirFilePath, NormalFolder, etc.
        // Let's set NirFilePath to file1, and Camera1Path to file2?
        fileGroup.NirFilePath = file1;
        fileGroup.HasNir = true;
        fileGroup.CameraFiles["cam1"] = file2;
        
        // Ensure FileGroup.GetAllFilePaths returns these
        // Validating assumptions about FileGroup below:
        // var paths = fileGroup.GetAllFilePaths();
        
        var cts = new CancellationTokenSource();
        int filesProcssedCount = 0;

        var progress = new Progress<OperationProgress>(p =>
        {
            // 2. Act (Trigger Cancellation)
            // Wait until first file (file1) is "Cleaning up" (meaning copied and about to be deleted) or "Done"?
            // In FileOperationService:
            // Report "Cleaning up" -> Delete Source -> Report update? No.
            // Report "Cleaning up" is the last report for a file.
            // MoveFileGroupAsync:
            //   processedCount++ (after loop iteration finish)
            
            // Note: FileOperationService loop checks cancellation at START of loop.
            // So we need to cancel AFTER first file is done, but BEFORE second file starts properly.
            
            // "Cleaning up" is reported right before Delete(source).
            // So if we cancel at "Cleaning up", the delete might still happen, then next loop -> cancel throws.
            
            if (p.Status == "Cleaning up")
            {
               filesProcssedCount++; 
            }
            
            // If we have processed one file completely, cancel.
            // The service reports (ProcessedFiles, TotalFiles).
            // When ProcessedFiles becomes 1, it means 1 file is done/skipped.
            
            if (p.ProcessedFiles >= 1 && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        });

        // 3. Act
        var result = await _service.MoveFileGroupAsync(fileGroup, _destDir, progress, cancellationToken: cts.Token);

        // 4. Assert
        Assert.False(result.Success, "Operation should fail due to cancellation");
        
        // Check File1
        // It was processed first. Source deleted. Dest created.
        // Then Cancellation happened. Rollback triggered.
        // CURRENT BUG: dest is deleted. source is already gone. -> File1 lost.
        // EXPECTED FIX: dest moved back to source. -> File1 exists at source.
        
        bool file1AtSource = File.Exists(file1);
        bool file1AtDest = File.Exists(Path.Combine(_destDir, "file1.txt"));
        
        Assert.True(file1AtSource, $"File1 should be restored to source. (Dest exists? {file1AtDest})");
        Assert.False(file1AtDest, "File1 should be removed from dest");
        
        // Check File2
        // Should be untouched
        Assert.True(File.Exists(file2), "File2 should still exist at source");
    }
}
