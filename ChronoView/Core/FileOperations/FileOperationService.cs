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
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moving file group {GroupId} to {Destination} with subject '{Subject}'", group.GroupId, destinationPath, subject ?? "UnknownSubject");
        
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

            // 2. Handle Normal Folder with structured path (Copy-then-Delete for Stability)
            if (hasNormalFolder)
            {
                currentProcessingPath = group.NormalFolder;
                var folderName = Path.GetFileName(group.NormalFolder);
                
                // Build structured path: <output>/<subject>/with NIR or without NIR/일반 or 일반2/
                var normalRole = group.LineNumber == 1 ? "일반" : "일반2";
                var destNormalDir = BuildStructuredPathForMove(destinationPath, group, normalRole, subject);
                Directory.CreateDirectory(destNormalDir);
                
                var destNormalPath = Path.Combine(destNormalDir, folderName);

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
            
            // 3. Process individual files with structured paths
            // NIR files and Camera files are handled separately with their own role-based folders
            
            // 3a. Handle NIR files (.spc + .txt)
            if (group.HasNir && !string.IsNullOrEmpty(group.NirFilePath))
            {
                var nirDestDir = BuildStructuredPathForMove(destinationPath, group, "Nir", subject);
                Directory.CreateDirectory(nirDestDir);

                // Get all NIR files (both .spc and .txt)
                var nirFiles = new List<string>();
                if (File.Exists(group.NirFilePath))
                {
                    nirFiles.Add(group.NirFilePath);
                }

                // Also get the .txt file
                var nirDirectory = Path.GetDirectoryName(group.NirFilePath);
                if (!string.IsNullOrEmpty(nirDirectory))
                {
                    var nirKey = Path.GetFileNameWithoutExtension(group.NirFilePath);
                    var txtPathA = Path.Combine(nirDirectory, nirKey + "A.txt");
                    if (File.Exists(txtPathA))
                    {
                        nirFiles.Add(txtPathA);
                    }
                    else
                    {
                        var txtPath = Path.Combine(nirDirectory, nirKey + ".txt");
                        if (File.Exists(txtPath))
                        {
                            nirFiles.Add(txtPath);
                        }
                    }
                }

                // Move all NIR files
                foreach (var nirFile in nirFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentProcessingPath = nirFile;

                    var fileName = Path.GetFileName(nirFile);
                    var destPath = Path.Combine(nirDestDir, fileName);

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
                            continue;
                        }
                        else if (resolution == ConflictResolution.Overwrite)
                        {
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

                    await Task.Run(() => File.Copy(nirFile, destPath, overwrite: false), cancellationToken);

                    if (!VerifyFileCopy(nirFile, destPath))
                    {
                        throw new IOException($"File copy verification failed: {nirFile}");
                    }

                    movedItems.Add((nirFile, destPath, false));

                    await Task.Run(() => File.Delete(nirFile), cancellationToken);

                    result.FilesProcessed++;
                    processedCount++;
                }
            }

            // 3b. Handle Camera files (cam1~6)
            foreach (var camEntry in group.CameraFiles)
            {
                var camKey = camEntry.Key;  // "cam1", "cam2", etc.
                var camFile = camEntry.Value;

                if (string.IsNullOrEmpty(camFile) || !File.Exists(camFile))
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                currentProcessingPath = camFile;

                // Build structured path for this camera
                var camDestDir = BuildStructuredPathForMove(destinationPath, group, camKey, subject);
                Directory.CreateDirectory(camDestDir);

                var fileName = Path.GetFileName(camFile);
                var destPath = Path.Combine(camDestDir, fileName);

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
                        continue;
                    }
                    else if (resolution == ConflictResolution.Overwrite)
                    {
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

                await Task.Run(() => File.Copy(camFile, destPath, overwrite: false), cancellationToken);

                if (!VerifyFileCopy(camFile, destPath))
                {
                    throw new IOException($"File copy verification failed: {camFile}");
                }

                movedItems.Add((camFile, destPath, false));

                await Task.Run(() => File.Delete(camFile), cancellationToken);

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

    /// <summary>
    /// Builds structured destination path for DELETE: <base>/<date>/<subject>/Line#/<role>/
    /// </summary>
    private string BuildStructuredPath(
        string basePath,
        FileGroup group,
        string role,  // "nir1", "nir2", "일반1", "일반2", "cam1", etc.
        string? subject = null)
    {
        // 1. Extract date from group timestamp (YYYYMMDD)
        var date = group.Timestamp.ToString("yyyyMMdd");
        
        // 2. Use subject or default "UnknownSubject"
        var subjectFolder = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
        
        // 3. Determine line folder
        var lineFolder = $"Line{group.LineNumber}";
        
        // 4. Construct path
        return Path.Combine(basePath, date, subjectFolder, lineFolder, role);
    }

    /// <summary>
    /// Builds structured destination path for MOVE: <base>/<subject>/with NIR or without NIR/<role>/
    /// </summary>
    private string BuildStructuredPathForMove(
        string basePath,
        FileGroup group,
        string role,  // "Nir", "일반", "일반2", "cam1", etc.
        string? subject = null)
    {
        // 1. Use subject or default "UnknownSubject"
        var subjectFolder = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
        
        // 2. Determine with NIR or without NIR
        var nirFolder = group.HasNir ? "with NIR" : "without NIR";
        
        // 3. Construct path based on role
        // For cameras, group under "복합 카메라"
        if (role.StartsWith("cam"))
        {
            return Path.Combine(basePath, subjectFolder, nirFolder, "복합 카메라", role);
        }
        // For normal cameras, use specific naming
        else if (role == "일반" || role == "일반2")
        {
            // with NIR: "일반" or "일반2"
            // without NIR: "일반 카메라" or "일반2 카메라"
            var normalRole = group.HasNir ? role : $"{role} 카메라";
            return Path.Combine(basePath, subjectFolder, nirFolder, normalRole);
        }
        // For NIR
        else
        {
            return Path.Combine(basePath, subjectFolder, nirFolder, role);
        }
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
                // - If we're rolling back, it means the operation failed/cancelled.
                // - Source may still exist (if delete hadn't happened yet).
                // - Or Source may be gone (if delete succeeded).
                //
                // RESTORE LOGIC:
                // 1. If Source is MISSING -> Move Dest back to Source (Restore).
                // 2. If Source EXISTS -> Delete Dest (Cleanup copy).

                if (isDirectory)
                {
                    if (!Directory.Exists(source))
                    {
                        if (Directory.Exists(dest))
                        {
                            _logger.LogInformation("Restoring directory: {Dest} -> {Source}", dest, source);
                            await Task.Run(() => Directory.Move(dest, source));
                        }
                        else
                        {
                            _logger.LogWarning("Cannot restore directory: Destination {Dest} is also missing", dest);
                            result.FailedFiles.Add(dest); // Mark as failed (lost)
                        }
                    }
                    else
                    {
                        // Source exists, so just clean up the copy at dest
                        if (Directory.Exists(dest))
                        {
                            _logger.LogInformation("Cleaning up destination directory: {Path}", dest);
                            await Task.Run(() => Directory.Delete(dest, recursive: true));
                        }
                    }
                }
                else
                {
                    if (!File.Exists(source))
                    {
                        if (File.Exists(dest))
                        {
                            _logger.LogInformation("Restoring file: {Dest} -> {Source}", dest, source);
                            await Task.Run(() => File.Move(dest, source));
                        }
                        else
                        {
                            _logger.LogWarning("Cannot restore file: Destination {Dest} is also missing", dest);
                            result.FailedFiles.Add(dest); // Mark as failed (lost)
                        }
                    }
                    else
                    {
                        // Source exists, so just clean up the copy at dest
                        if (File.Exists(dest))
                        {
                            _logger.LogInformation("Cleaning up destination file: {Path}", dest);
                            await Task.Run(() => File.Delete(dest));
                        }
                    }
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
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file group {GroupId} with subject '{Subject}'", group.GroupId, subject ?? "UnknownSubject");
        
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
            // Move Normal Folder to quarantine with structured path (soft delete with Copy-then-Delete)
            if (hasNormalFolder)
            {
                currentProcessingPath = group.NormalFolder;
                var folderName = Path.GetFileName(group.NormalFolder);
                
                // Build structured path: <quarantine>/<date>/<subject>/Line#/일반#/
                var normalRole = group.LineNumber == 1 ? "일반1" : "일반2";
                var destNormalDir = BuildStructuredPath(quarantinePath, group, normalRole, subject);
                Directory.CreateDirectory(destNormalDir);
                
                var destNormalPath = Path.Combine(destNormalDir, folderName);

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


            // Process individual files with structured paths
            // NIR files and Camera files are handled separately with their own role-based folders
            
            // 1. Handle NIR files (.spc + .txt)
            if (group.HasNir && !string.IsNullOrEmpty(group.NirFilePath))
            {
                var nirRole = group.LineNumber == 1 ? "nir1" : "nir2";
                var nirDestDir = BuildStructuredPath(quarantinePath, group, nirRole, subject);
                Directory.CreateDirectory(nirDestDir);

                // Get all NIR files (both .spc and .txt)
                var nirFiles = new List<string>();
                if (File.Exists(group.NirFilePath))
                {
                    nirFiles.Add(group.NirFilePath);
                }

                // Also get the .txt file
                var nirDirectory = Path.GetDirectoryName(group.NirFilePath);
                if (!string.IsNullOrEmpty(nirDirectory))
                {
                    var nirKey = Path.GetFileNameWithoutExtension(group.NirFilePath);
                    var txtPathA = Path.Combine(nirDirectory, nirKey + "A.txt");
                    if (File.Exists(txtPathA))
                    {
                        nirFiles.Add(txtPathA);
                    }
                    else
                    {
                        var txtPath = Path.Combine(nirDirectory, nirKey + ".txt");
                        if (File.Exists(txtPath))
                        {
                            nirFiles.Add(txtPath);
                        }
                    }
                }

                // Move all NIR files
                foreach (var nirFile in nirFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentProcessingPath = nirFile;

                    var fileName = Path.GetFileName(nirFile);
                    var destPath = Path.Combine(nirDestDir, fileName);

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
                            continue;
                        }
                        else if (resolution == ConflictResolution.Overwrite)
                        {
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

                        await Task.Run(() => File.Copy(nirFile, destPath, overwrite: false), cancellationToken);

                        if (!VerifyFileCopy(nirFile, destPath))
                        {
                            throw new IOException($"File copy verification failed: {nirFile}");
                        }

                        await Task.Run(() => File.Delete(nirFile), cancellationToken);

                        result.FilesProcessed++;
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete NIR file {Path}", nirFile);
                        result.FailedFiles.Add(nirFile);
                    }
                }
            }

            // 2. Handle Camera files (cam1~6)
            foreach (var camEntry in group.CameraFiles)
            {
                var camKey = camEntry.Key;  // "cam1", "cam2", etc.
                var camFile = camEntry.Value;

                if (string.IsNullOrEmpty(camFile) || !File.Exists(camFile))
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                currentProcessingPath = camFile;

                // Build structured path for this camera
                var camDestDir = BuildStructuredPath(quarantinePath, group, camKey, subject);
                Directory.CreateDirectory(camDestDir);

                var fileName = Path.GetFileName(camFile);
                var destPath = Path.Combine(camDestDir, fileName);

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
                        continue;
                    }
                    else if (resolution == ConflictResolution.Overwrite)
                    {
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

                    await Task.Run(() => File.Copy(camFile, destPath, overwrite: false), cancellationToken);

                    if (!VerifyFileCopy(camFile, destPath))
                    {
                        throw new IOException($"File copy verification failed: {camFile}");
                    }

                    await Task.Run(() => File.Delete(camFile), cancellationToken);

                    result.FilesProcessed++;
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete camera file {Path}", camFile);
                    result.FailedFiles.Add(camFile);
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
