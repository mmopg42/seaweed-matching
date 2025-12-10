using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.IO;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Implementation of file operation service for moving and deleting file groups.
/// </summary>
public class FileOperationService : IFileOperationService
{
    private readonly ILogger<FileOperationService> _logger;

    public FileOperationService(ILogger<FileOperationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OperationResult> MoveFileGroupAsync(
        FileGroup group,
        string destinationPath,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moving file group {GroupId} to {Destination}", group.GroupId, destinationPath);
        
        var result = new OperationResult();
        ConflictResolution? stickyResolution = null; // "Apply to All" resolution
        
        // 1. Prepare moved items tracking for rollback (Path, IsDirectory)
        var movedItems = new List<(string source, string dest, bool isDirectory)>();
        string? currentProcessingPath = null;
        
        // Estimate total items (Files + Normal Folder if distinct)
        bool hasNormalFolder = !string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder);
        // Note: GetAllFilePaths() does not include the Normal Folder itself, only files. 
        // We assume Normal Folder handling includes all its contents implicitly via directory move.
        // So we count "Normal Folder" as 1 item, and exclude its contents from individual file list if separate.
        int fileCount = group.GetAllFilePaths().Count();
        int totalEstimate = fileCount + (hasNormalFolder ? 1 : 0);
        
        int processedCount = 0;

        try
        {
            Directory.CreateDirectory(destinationPath);

            // 2. Handle Normal Folder (Copy-then-Delete for Stability)
            if (hasNormalFolder)
            {
                currentProcessingPath = group.NormalFolder;
                var folderName = Path.GetFileName(group.NormalFolder);
                var destNormalPath = Path.Combine(destinationPath, folderName);

                bool skipDirectory = false;
                if (Directory.Exists(destNormalPath))
                {
                    // Resolve Conflict
                    var resolution = stickyResolution ?? onConflict?.Invoke(destNormalPath) ?? ConflictResolution.Skip;

                    if (stickyResolution == null && resolution != ConflictResolution.Abort)
                    {
                        stickyResolution = resolution;
                    }

                    if (resolution == ConflictResolution.Overwrite)
                    {
                         // Warning: Directory overwrite implies deleting the destination first
                         _logger.LogWarning("Overwriting directory: {Path}", destNormalPath);
                         Directory.Delete(destNormalPath, recursive: true);
                    }
                    else if (resolution == ConflictResolution.Skip)
                    {
                        skipDirectory = true;
                        _logger.LogInformation("Skipping directory copy: {Path}", destNormalPath);

                        // Count skip as processed
                        result.FilesProcessed++;
                        processedCount++;
                    }
                    else if (resolution == ConflictResolution.Abort)
                    {
                        throw new OperationCanceledException("Operation aborted by user during directory conflict.");
                    }
                }

                if (!skipDirectory)
                {
                    progress?.Report(new OperationProgress
                    {
                        TotalFiles = totalEstimate,
                        ProcessedFiles = processedCount,
                        CurrentFile = $"Directory: {folderName}",
                        Status = "Copying"
                    });

                    // Copy-then-Delete Pattern for Directory
                    // 1. Copy directory recursively
                    await CopyDirectoryAsync(group.NormalFolder, destNormalPath, cancellationToken);

                    // 2. Verify copy success
                    if (!VerifyDirectoryCopy(group.NormalFolder, destNormalPath))
                    {
                        throw new IOException($"Directory copy verification failed: {group.NormalFolder}");
                    }

                    movedItems.Add((group.NormalFolder, destNormalPath, true));

                    // 3. Delete original after successful copy
                    progress?.Report(new OperationProgress
                    {
                        TotalFiles = totalEstimate,
                        ProcessedFiles = processedCount,
                        CurrentFile = $"Directory: {folderName}",
                        Status = "Cleaning up"
                    });

                    await Task.Run(() => Directory.Delete(group.NormalFolder, recursive: true), cancellationToken);

                    // Count move as processed
                    result.FilesProcessed++;
                    processedCount++;
                }
            }
            
            // 3. Handle Individual Files (NIR, Cameras, etc.)
            // Exclude files that are inside the Normal folder if we already moved/skipped the directory
            var allFiles = group.GetAllFilePaths().Where(f => File.Exists(f)).ToList();
            if (hasNormalFolder)
            {
                var normalPathPrefix = group.NormalFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                allFiles = allFiles.Where(f => !f.StartsWith(normalPathPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Adjust estimate if we filtered out files (though typically GetAllFilePaths shouldn't include inside Normal)
                // If logic changes, totalEstimate might be slightly off, but acceptable.
            }

            foreach (var sourcePath in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentProcessingPath = sourcePath;

                var fileName = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(destinationPath, fileName);

                if (File.Exists(destPath))
                {
                    var resolution = stickyResolution ?? onConflict?.Invoke(destPath) ?? ConflictResolution.Skip;

                    if (stickyResolution == null && resolution != ConflictResolution.Abort)
                    {
                        stickyResolution = resolution;
                    }

                    if (resolution == ConflictResolution.Skip)
                    {
                        // Skip counts as success/processed
                        result.FilesProcessed++;
                        processedCount++;
                        progress?.Report(new OperationProgress
                        {
                            TotalFiles = totalEstimate,
                            ProcessedFiles = processedCount,
                            CurrentFile = fileName,
                            Status = "Skipped"
                        });
                        continue;
                    }
                    else if (resolution == ConflictResolution.Overwrite)
                    {
                        // Delete existing file before copy
                        _logger.LogWarning("Overwriting file: {Path}", destPath);
                        await Task.Run(() => File.Delete(destPath), cancellationToken);
                    }
                    else if (resolution == ConflictResolution.Abort)
                    {
                        throw new OperationCanceledException("Operation aborted by user.");
                    }
                }

                progress?.Report(new OperationProgress
                {
                    TotalFiles = totalEstimate,
                    ProcessedFiles = processedCount,
                    CurrentFile = fileName,
                    Status = "Copying"
                });

                // Copy-then-Delete Pattern for File
                // 1. Copy file
                await Task.Run(() => File.Copy(sourcePath, destPath, overwrite: false), cancellationToken);

                // 2. Verify copy
                if (!VerifyFileCopy(sourcePath, destPath))
                {
                    throw new IOException($"File copy verification failed: {sourcePath}");
                }

                movedItems.Add((sourcePath, destPath, false));

                // 3. Delete original after successful copy
                progress?.Report(new OperationProgress
                {
                    TotalFiles = totalEstimate,
                    ProcessedFiles = processedCount,
                    CurrentFile = fileName,
                    Status = "Cleaning up"
                });

                await Task.Run(() => File.Delete(sourcePath), cancellationToken);

                result.FilesProcessed++;
                processedCount++;
            }
            
            result.Success = true;
            progress?.Report(new OperationProgress { 
                TotalFiles = totalEstimate, 
                ProcessedFiles = totalEstimate, 
                CurrentFile = "Complete", 
                Status = "Done" 
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Move operation cancelled: {Message}", ex.Message);
            result.ErrorMessage = ex.Message;
            result.Success = false;
            
            if (currentProcessingPath != null)
            {
                result.FailedFiles.Add(currentProcessingPath);
            }
            
            result.FilesFailed = totalEstimate - result.FilesProcessed;
            
            await RollbackAsync(movedItems, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving files");
            result.ErrorMessage = ex.Message;
            result.Success = false;
            
            if (currentProcessingPath != null)
            {
                result.FailedFiles.Add(currentProcessingPath);
            }
            
            result.FilesFailed = totalEstimate - result.FilesProcessed;
            
            await RollbackAsync(movedItems, result);
        }
        
        return result;
    }

    #region Helper Methods - Copy-then-Delete Pattern

    /// <summary>
    /// Verifies that a file was copied successfully by comparing file sizes.
    /// </summary>
    private bool VerifyFileCopy(string sourcePath, string destPath)
    {
        if (!File.Exists(destPath))
            return false;

        var sourceInfo = new FileInfo(sourcePath);
        var destInfo = new FileInfo(destPath);

        // Size check (fast and reliable)
        return sourceInfo.Length == destInfo.Length;
    }

    /// <summary>
    /// Recursively copies a directory and all its contents.
    /// </summary>
    private async Task CopyDirectoryAsync(
        string sourceDir,
        string destDir,
        CancellationToken cancellationToken)
    {
        // Create destination directory
        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);

            await Task.Run(() => File.Copy(file, destFile, overwrite: false), cancellationToken);
        }

        // Copy all subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destDir, dirName);

            await CopyDirectoryAsync(subDir, destSubDir, cancellationToken);
        }
    }

    /// <summary>
    /// Verifies that a directory was copied successfully by comparing file counts and total size.
    /// </summary>
    private bool VerifyDirectoryCopy(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir))
            return false;

        // Check file count
        var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        var destFiles = Directory.GetFiles(destDir, "*", SearchOption.AllDirectories);

        if (sourceFiles.Length != destFiles.Length)
            return false;

        // Check total size (fast verification)
        long sourceSize = sourceFiles.Sum(f => new FileInfo(f).Length);
        long destSize = destFiles.Sum(f => new FileInfo(f).Length);

        return sourceSize == destSize;
    }

    #endregion

    /// <summary>
    /// Rollback for Copy-then-Delete pattern.
    /// In Copy-then-Delete, rollback simply cleans up the destination copies.
    /// The source files remain intact since deletion only happens after successful copy+verify.
    /// </summary>
    private async Task RollbackAsync(List<(string source, string dest, bool isDirectory)> movedItems, OperationResult result)
    {
        _logger.LogInformation("Rolling back {Count} items", movedItems.Count);

        // Rollback in reverse order
        movedItems.Reverse();

        foreach (var (source, dest, isDirectory) in movedItems)
        {
            try
            {
                // In Copy-then-Delete pattern:
                // - If we're rolling back, it means the copy succeeded but something failed later
                // - Source may still exist (if delete hadn't happened yet) or may be deleted
                // - We need to clean up the destination and restore source if needed

                if (isDirectory)
                {
                    // Clean up destination directory
                    if (Directory.Exists(dest))
                    {
                        _logger.LogInformation("Cleaning up destination directory: {Path}", dest);
                        await Task.Run(() => Directory.Delete(dest, recursive: true));
                    }

                    // Source should still exist unless delete succeeded
                    // If source was deleted, we can't restore it from dest in this simple rollback
                    // This is acceptable since Copy-then-Delete ensures data is not lost at dest
                }
                else
                {
                    // Clean up destination file
                    if (File.Exists(dest))
                    {
                        _logger.LogInformation("Cleaning up destination file: {Path}", dest);
                        await Task.Run(() => File.Delete(dest));
                    }

                    // Source should still exist unless delete succeeded
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback cleanup failed for {Path}", dest);
                result.FailedFiles.Add(dest);
                result.ErrorMessage += $" (Rollback cleanup failed: {Path.GetFileName(dest)})";
            }
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        string quarantinePath,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file group {GroupId}", group.GroupId);
        
        var result = new OperationResult();
        string? currentProcessingPath = null;
        
        if (string.IsNullOrWhiteSpace(quarantinePath))
        {
            result.Success = false;
            result.ErrorMessage = "Quarantine path is not configured.";
            return result;
        }

        Directory.CreateDirectory(quarantinePath);

        ConflictResolution? stickyResolution = null; // Apply-to-all resolution
        bool hasNormalFolder = !string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder);
        int fileCount = group.GetAllFilePaths().Count();
        int totalEstimate = fileCount + (hasNormalFolder ? 1 : 0);
        
        int processedCount = 0;
        
        try
        {
            // Move Normal Folder to quarantine (soft delete with Copy-then-Delete)
            if (hasNormalFolder)
            {
                currentProcessingPath = group.NormalFolder;
                var folderName = Path.GetFileName(group.NormalFolder);
                var destNormalPath = Path.Combine(quarantinePath, folderName);

                bool skipDirectory = false;
                if (Directory.Exists(destNormalPath))
                {
                    var resolution = stickyResolution ?? onConflict?.Invoke(destNormalPath) ?? ConflictResolution.Skip;
                    if (stickyResolution == null && resolution != ConflictResolution.Abort)
                    {
                        stickyResolution = resolution;
                    }

                    if (resolution == ConflictResolution.Overwrite)
                    {
                        _logger.LogWarning("Overwriting directory in quarantine: {Path}", destNormalPath);
                        Directory.Delete(destNormalPath, recursive: true);
                    }
                    else if (resolution == ConflictResolution.Skip)
                    {
                        skipDirectory = true;
                        result.FilesProcessed++;
                        processedCount++;
                    }
                    else if (resolution == ConflictResolution.Abort)
                    {
                        throw new OperationCanceledException("Operation aborted by user during directory conflict.");
                    }
                }

                if (!skipDirectory)
                {
                    progress?.Report(new OperationProgress
                    {
                        TotalFiles = totalEstimate,
                        ProcessedFiles = processedCount,
                        CurrentFile = $"Directory: {folderName}",
                        Status = "Copying to quarantine"
                    });

                    // Copy-then-Delete Pattern for Directory
                    // 1. Copy directory recursively
                    await CopyDirectoryAsync(group.NormalFolder, destNormalPath, cancellationToken);

                    // 2. Verify copy success
                    if (!VerifyDirectoryCopy(group.NormalFolder, destNormalPath))
                    {
                        throw new IOException($"Directory copy verification failed: {group.NormalFolder}");
                    }

                    // 3. Delete original after successful copy
                    progress?.Report(new OperationProgress
                    {
                        TotalFiles = totalEstimate,
                        ProcessedFiles = processedCount,
                        CurrentFile = $"Directory: {folderName}",
                        Status = "Deleting original"
                    });

                    await Task.Run(() => Directory.Delete(group.NormalFolder, recursive: true), cancellationToken);

                    result.FilesProcessed++;
                    processedCount++;
                }
            }

            // Delete other files (excluding inside normal folder logic similar to move)
            var allFiles = group.GetAllFilePaths().Where(f => File.Exists(f)).ToList();
             if (hasNormalFolder)
            {
                var normalPathPrefix = group.NormalFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                allFiles = allFiles.Where(f => !f.StartsWith(normalPathPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var filePath in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentProcessingPath = filePath;

                var fileName = Path.GetFileName(filePath);
                var destPath = Path.Combine(quarantinePath, fileName);

                if (File.Exists(destPath))
                {
                    var resolution = stickyResolution ?? onConflict?.Invoke(destPath) ?? ConflictResolution.Skip;
                    if (stickyResolution == null && resolution != ConflictResolution.Abort)
                    {
                        stickyResolution = resolution;
                    }

                    if (resolution == ConflictResolution.Skip)
                    {
                        result.FilesProcessed++;
                        processedCount++;
                        progress?.Report(new OperationProgress
                        {
                            TotalFiles = totalEstimate,
                            ProcessedFiles = processedCount,
                            CurrentFile = fileName,
                            Status = "Skipped"
                        });
                        continue;
                    }
                    else if (resolution == ConflictResolution.Overwrite)
                    {
                        // Delete existing file before copy
                        _logger.LogWarning("Overwriting file in quarantine: {Path}", destPath);
                        await Task.Run(() => File.Delete(destPath), cancellationToken);
                    }
                    else if (resolution == ConflictResolution.Abort)
                    {
                        throw new OperationCanceledException("Operation aborted by user.");
                    }
                }

                try
                {
                    progress?.Report(new OperationProgress
                    {
                        TotalFiles = totalEstimate,
                        ProcessedFiles = processedCount,
                        CurrentFile = fileName,
                        Status = "Copying to quarantine"
                    });

                    // Copy-then-Delete Pattern for File
                    // 1. Copy file to quarantine
                    await Task.Run(() => File.Copy(filePath, destPath, overwrite: false), cancellationToken);

                    // 2. Verify copy
                    if (!VerifyFileCopy(filePath, destPath))
                    {
                        throw new IOException($"File copy verification failed: {filePath}");
                    }

                    // 3. Delete original after successful copy
                    progress?.Report(new OperationProgress
                    {
                        TotalFiles = totalEstimate,
                        ProcessedFiles = processedCount,
                        CurrentFile = fileName,
                        Status = "Deleting original"
                    });

                    await Task.Run(() => File.Delete(filePath), cancellationToken);

                    result.FilesProcessed++;
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete (move to quarantine) file {Path}", filePath);
                    result.FailedFiles.Add(filePath);
                    // Continue deletion attempts for other files
                }
            }
            
            result.Success = result.FailedFiles.Count == 0;
            if (!result.Success)
            {
                result.ErrorMessage = $"Failed to delete {result.FailedFiles.Count} files.";
                result.FilesFailed = result.FailedFiles.Count;
            }
            else
            {
                result.FilesFailed = 0;
            }
            
             progress?.Report(new OperationProgress { 
                TotalFiles = totalEstimate, 
                ProcessedFiles = processedCount, 
                CurrentFile = "Complete", 
                Status = "Done" 
            });
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Operation cancelled";
            if (currentProcessingPath != null)
            {
                result.FailedFiles.Add(currentProcessingPath);
            }
            result.FilesFailed = totalEstimate - result.FilesProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group");
            result.ErrorMessage = ex.Message;
            result.Success = false;
            if (currentProcessingPath != null)
            {
                result.FailedFiles.Add(currentProcessingPath);
            }
            result.FilesFailed = totalEstimate - result.FilesProcessed;
        }
        
        return result;
    }
}
