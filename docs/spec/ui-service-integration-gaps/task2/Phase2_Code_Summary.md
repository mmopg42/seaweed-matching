# Phase 2 Implementation Summary (Service Layer)

This document contains the source code for the services implemented during Phase 2, focusing on File Operations, Matching, and Path Management.

## 1. File Operation Service (Move/Delete)
Handles robust file moving and deleting with conflict resolution (Apply-to-All) and directory-level handling for Normal folders.

### IFileOperationService.cs
```csharp
using ChronoView.Models;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Service interface for file operations (move, delete) on file groups.
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Moves all files in a file group to the specified destination path.
    /// </summary>
    /// <param name="group">The file group to move.</param>
    /// <param name="destinationPath">The destination directory path.</param>
    /// <param name="progress">Progress reporter for the operation.</param>
    /// <param name="onConflict">Callback for resolving name conflicts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the move operation.</returns>
    Task<OperationResult> MoveFileGroupAsync(
        FileGroup group,
        string destinationPath,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all files in a file group.
    /// </summary>
    /// <param name="group">The file group to delete.</param>
    /// <param name="progress">Progress reporter for the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the delete operation.</returns>
    Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolution strategy for file conflicts.
/// </summary>
public enum ConflictResolution
{
    /// <summary>Overwrite the existing file.</summary>
    Overwrite,
    /// <summary>Skip the file (counts as processed).</summary>
    Skip,
    /// <summary>Abort the entire operation.</summary>
    Abort
}

/// <summary>
/// Result of a file operation.
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int FilesProcessed { get; set; }
    public int FilesFailed { get; set; }
    
    /// <summary>
    /// List of files that failed to process (due to errors, not skips).
    /// </summary>
    public List<string> FailedFiles { get; } = new();
}

/// <summary>
/// Progress information for file operations.
/// </summary>
public class OperationProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}
```

### FileOperationService.cs
```csharp
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
        
        try
        {
            Directory.CreateDirectory(destinationPath);
            int totalEstimate = group.GetAllFilePaths().Count(); // Estimate
            int processedCount = 0;

            // 2. Handle Normal Folder (Directory Move)
            if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder))
            {
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
                        _logger.LogInformation("Skipping directory move: {Path}", destNormalPath);
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
                        Status = "Moving"
                    });

                    await Task.Run(() => Directory.Move(group.NormalFolder, destNormalPath), cancellationToken);
                    movedItems.Add((group.NormalFolder, destNormalPath, true));
                }
            }
            
            // 3. Handle Individual Files (NIR, Cameras, etc.)
            // Exclude files that are inside the Normal folder if we already moved/skipped the directory
            var allFiles = group.GetAllFilePaths().Where(f => File.Exists(f)).ToList();
            if (!string.IsNullOrEmpty(group.NormalFolder))
            {
                // Filter out files that start with NormalFolderPath
                // Ensure NormalFolder path ends with separator for correct prefix check
                var normalPathPrefix = group.NormalFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                allFiles = allFiles.Where(f => !f.StartsWith(normalPathPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var sourcePath in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var fileName = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(destinationPath, fileName);
                
                bool overwrite = false;
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
                        overwrite = true;
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
                    Status = "Moving"
                });

                await Task.Run(() => File.Move(sourcePath, destPath, overwrite), cancellationToken);
                movedItems.Add((sourcePath, destPath, false));
                
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
            await RollbackAsync(movedItems, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving files");
            result.ErrorMessage = ex.Message;
            result.Success = false;
            // Calculating remaining failed count is tricky with filtered lists, 
            // but we can approximate or leave it to "Moved vs Total"
             result.FilesFailed = Math.Max(0, group.GetAllFilePaths().Count() - result.FilesProcessed);
            
            await RollbackAsync(movedItems, result);
        }
        
        return result;
    }

    private async Task RollbackAsync(List<(string source, string dest, bool isDirectory)> movedItems, OperationResult result)
    {
        // Rollback in reverse order
        movedItems.Reverse();
        
        foreach (var (source, dest, isDirectory) in movedItems)
        {
            try
            {
                if (isDirectory)
                {
                    if (Directory.Exists(dest) && !Directory.Exists(source))
                    {
                        await Task.Run(() => Directory.Move(dest, source));
                    }
                }
                else
                {
                    if (File.Exists(dest) && !File.Exists(source))
                    {
                        await Task.Run(() => File.Move(dest, source, overwrite: true));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback failed for {Path}", dest);
                result.FailedFiles.Add(dest);
                result.ErrorMessage += $" (Rollback failed: {Path.GetFileName(dest)})";
            }
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file group {GroupId}", group.GroupId);
        
        var result = new OperationResult();
        // 1. Delete Normal Folder logic
        int totalEstimate = group.GetAllFilePaths().Count(); 
        // Adding 1 for folder itself if exists? Keep simple file count.
        
        int processedCount = 0;
        
        try
        {
            // Delete Normal Folder
            if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder))
            {
                 progress?.Report(new OperationProgress { 
                    TotalFiles = totalEstimate, 
                    ProcessedFiles = processedCount, 
                    CurrentFile = $"Directory: {Path.GetFileName(group.NormalFolder)}",
                    Status = "Deleting"
                });
                
                await Task.Run(() => Directory.Delete(group.NormalFolder, recursive: true), cancellationToken);
            }
            
            // Delete other files (excluding inside normal folder logic similar to move)
            var allFiles = group.GetAllFilePaths().Where(f => File.Exists(f)).ToList();
             if (!string.IsNullOrEmpty(group.NormalFolder))
            {
                var normalPathPrefix = group.NormalFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                allFiles = allFiles.Where(f => !f.StartsWith(normalPathPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var filePath in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(filePath);
                
                progress?.Report(new OperationProgress
                {
                    TotalFiles = totalEstimate,
                    ProcessedFiles = processedCount,
                    CurrentFile = fileName,
                    Status = "Deleting"
                });
                
                try
                {
                    await Task.Run(() => File.Delete(filePath), cancellationToken);
                    result.FilesProcessed++;
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file {Path}", filePath);
                    result.FailedFiles.Add(filePath);
                    // Continue deletion? Usually yes for delete operations. 
                    // But if strict, we might stop. 
                    // Requirement said: "Exception: Record failure and Log. Continue or Abort depending on policy."
                    // We will continue but mark result as partial success if intended.
                    // But usually exception breaks the loop unless we catch inside loop.
                    // I wrapped this individual delete in try-catch to continue.
                }
            }
            
            result.Success = result.FailedFiles.Count == 0;
            if (!result.Success)
            {
                result.ErrorMessage = $"Failed to delete {result.FailedFiles.Count} files.";
                result.FilesFailed = result.FailedFiles.Count;
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group");
            result.ErrorMessage = ex.Message;
            result.Success = false;
        }
        
        return result;
    }
}
```

## 2. Models
Extended `FileGroup` model with new properties and helper methods.

### FileGroup.cs
```csharp
using System.IO;
using System.Text.Json.Serialization;

namespace ChronoView.Models;

/// <summary>
/// Represents a group of related files (NIR, camera images, normal folders) matched by timestamp correlation.
/// </summary>
public class FileGroup : IEquatable<FileGroup>
{
    // ... Existing Properties ...
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [JsonPropertyName("nir_key")]
    public string NirKey { get; set; } = string.Empty;

    [JsonPropertyName("normal_folder")]
    public string NormalFolder { get; set; } = string.Empty;

    [JsonPropertyName("main_image_path")]
    public string MainImagePath { get; set; } = string.Empty;

    [JsonPropertyName("camera_files")]
    public Dictionary<string, string> CameraFiles { get; set; } = new();

    [JsonPropertyName("line_number")]
    public int LineNumber { get; set; } = 1;

    [JsonPropertyName("has_nir")]
    public bool HasNir { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("status")]
    public GroupStatus Status { get; set; } = GroupStatus.Pending;

    // ... New Properties & Methods ...

    /// <summary>
    /// Path to the NIR file associated with this group.
    /// </summary>
    [JsonPropertyName("nir_file_path")]
    public string NirFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets all file paths associated with this group (MainImage, NIR, Cameras).
    /// </summary>
    public IEnumerable<string> GetAllFilePaths()
    {
        if (!string.IsNullOrEmpty(MainImagePath))
            yield return MainImagePath;

        if (HasNir && !string.IsNullOrEmpty(NirFilePath))
            yield return NirFilePath;

        foreach (var path in CameraFiles.Values)
        {
            if (!string.IsNullOrEmpty(path))
                yield return path;
        }
    }
    
    public static int GetLineNumberFromNormalFolder(string normalFolderName)
    {
        if (string.IsNullOrEmpty(normalFolderName)) return 1;
        var folderName = Path.GetFileName(normalFolderName);
        if (folderName.EndsWith("_0")) return 1;
        if (folderName.EndsWith("_1")) return 2;
        return 1;
    }
    
    // ... Equals/HashCode/Status Enum ...
}
```

## 3. Matching Logic
Updated matcher to populate `NirFilePath`.

### FileGroupMatcherService.cs
```csharp
// Only showing relevant MatchFiles logic snippet
// ...
                if (targetIdx.HasValue)
                {
                    // Attach to existing group
                    groups[targetIdx.Value].NirKey = nir.Key;
                    groups[targetIdx.Value].NirFilePath = nir.Path; // Newly added property
                    groups[targetIdx.Value].HasNir = true;
                }
                else
                {
                    // Create NIR-only group
                    var nirOnlyGroup = new FileGroup
                    {
                        GroupId = "",
                        NirKey = nir.Key,
                        NirFilePath = nir.Path, // Newly added property
                        CreatedAt = nir.Timestamp!.Value,
                        Status = GroupStatus.Complete,
                        HasNir = true,
                        // ...
                    };
                    groups.Add(nirOnlyGroup);
                }
// ...
```

## 4. Path Management (Task 2.2)

### PathManagementService.cs
```csharp
using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;

namespace ChronoView.Core.FileOperations;

public class PathManagementService : IPathManagementService
{
    private readonly ILogger<PathManagementService> _logger;

    public PathManagementService(ILogger<PathManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Dictionary<string, string> GeneratePathsFromDate(string dateString, ApplicationConfiguration config)
    {
        var paths = new Dictionary<string, string>();
        if (!DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return paths;
        }
        
        var basePath = config.FolderPaths.TryGetValue("BasePath", out var bp) ? bp : "D:/Data";
        var datePath = Path.Combine(basePath, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd"));
        
        paths["NIR1"] = Path.Combine(datePath, "NIR1");
        paths["Normal1"] = Path.Combine(datePath, "Normal1");
        // ... Cam1-3 ...
        
        paths["NIR2"] = Path.Combine(datePath, "NIR2");
        paths["Normal2"] = Path.Combine(datePath, "Normal2");
        // ... Cam4-6 ...
        
        paths["Output"] = Path.Combine(datePath, "Output");
        return paths;
    }

    public async Task<bool> CreateSampleFoldersAsync(string sampleName, ApplicationConfiguration config, CancellationToken cancellationToken = default)
    {
        // Creates folders for all configured Line 1/2 paths with sampleName appended
        // ... Implementation matches the provided code ...
        return true;
    }
    
    public bool ValidatePaths(Dictionary<string, string> paths)
    {
        // Validates paths do not contain invalid chars
        // ...
        return true;
    }
}
```
