using ChronoView.Models;
using ChronoView.Core.FileOperations.Line1;
using ChronoView.Core.FileOperations.Line2;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

public class FileGroupOperator : IFileGroupOperator
{
    private readonly ILogger<FileGroupOperator> _logger;
    private readonly IPathBuilder _line1PathBuilder;
    private readonly IPathBuilder _line2PathBuilder;

    public FileGroupOperator(
        ILogger<FileGroupOperator> logger,
        Line1PathBuilder line1PathBuilder,
        Line2PathBuilder line2PathBuilder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _line1PathBuilder = line1PathBuilder ?? throw new ArgumentNullException(nameof(line1PathBuilder));
        _line2PathBuilder = line2PathBuilder ?? throw new ArgumentNullException(nameof(line2PathBuilder));
    }

    private IPathBuilder GetPathBuilder(int lineNumber)
    {
        return lineNumber == 1 ? _line1PathBuilder : _line2PathBuilder;
    }

    public async Task<OperationResult> ExecuteOpAsync(FileGroup group, string targetBase, OpType opType, PathSchema schema, string? subject = null, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new OperationResult();
        var movedItems = new List<(string source, string dest, bool isDirectory)>();
        var pathBuilder = GetPathBuilder(group.LineNumber);

        _logger.LogInformation("ExecuteOpAsync started: GroupId={GroupId}, TargetBase={TargetBase}, OpType={OpType}, Schema={Schema}, Subject={Subject}", group.GroupId, targetBase, opType, schema, subject ?? "null");
        try {
            if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
                string? folderName = null;
                // MoveSchema와 QuarantineSchema 모두에서 folderName 추출
                if (schema == PathSchema.QuarantineSchema || schema == PathSchema.MoveSchema)
                {
                    folderName = Path.GetFileName(group.NormalFolder);
                }

                var destPath = pathBuilder.BuildNormalFolderPath(targetBase, subject ?? "UnknownSubject", group, schema, folderName);
                _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
                await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
            }
            if (group.HasNir && !string.IsNullOrEmpty(group.NirFilePath)) {
                var destDir = pathBuilder.BuildNirFolderPath(targetBase, subject ?? "UnknownSubject", group, schema);
                _logger.LogInformation("Moving NIR files to: {Dest}", destDir);
                foreach (var file in GetNirFileSet(group.NirFilePath)) {
                    // Camera 파일과 동일한 패턴: 존재 확인 후 이동
                    if (string.IsNullOrEmpty(file) || !File.Exists(file)) {
                        _logger.LogWarning("NIR file not found, skipping: {File}", file);
                        continue;
                    }
                    await MoveFileAtomicAsync(file, Path.Combine(destDir, Path.GetFileName(file)), movedItems, ct);
                }
            }
            foreach (var cam in group.CameraFiles) {
                if (string.IsNullOrEmpty(cam.Value) || !File.Exists(cam.Value)) continue;
                var destDir = pathBuilder.BuildCameraFolderPath(targetBase, subject ?? "UnknownSubject", group, schema, cam.Key);
                _logger.LogInformation("Moving camera file {Cam}: {Src} -> {Dest}", cam.Key, cam.Value, destDir);
                await MoveFileAtomicAsync(cam.Value, Path.Combine(destDir, Path.GetFileName(cam.Value)), movedItems, ct);
            }
            result.Success = true;
            _logger.LogInformation("ExecuteOpAsync completed successfully: {Count} items moved", movedItems.Count);
        } catch (Exception ex) {
            _logger.LogError(ex, "ExecuteOpAsync failed, rolling back {Count} items", movedItems.Count);
            await RollbackAsync(movedItems);
            result.Success = false; result.ErrorMessage = ex.Message;
        }
        return result;
    }

    public async Task<OperationResult> DeleteComponentsAsync(FileGroup group, IEnumerable<string> components, string quarantinePath, string? subject = null, CancellationToken ct = default)
    {
        var result = new OperationResult();
        var movedItems = new List<(string source, string dest, bool isDirectory)>();
        var pathBuilder = GetPathBuilder(group.LineNumber);

        _logger.LogInformation("DeleteComponentsAsync started: GroupId={GroupId}, Components={Components}, QuarantinePath={QuarantinePath}", group.GroupId, string.Join(",", components), quarantinePath);
        try {
            foreach (var comp in components) {
                if (comp == "Normal" && !string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
                    var folderName = Path.GetFileName(group.NormalFolder);
                    var destPath = pathBuilder.BuildNormalFolderPath(quarantinePath, subject ?? "UnknownSubject", group, PathSchema.QuarantineSchema, folderName);
                    _logger.LogDebug("Moving Normal folder: {Src} -> {Dest}", group.NormalFolder, destPath);
                    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
                } else if (comp == "Nir" && group.HasNir && !string.IsNullOrEmpty(group.NirFilePath)) {
                    var destDir = pathBuilder.BuildNirFolderPath(quarantinePath, subject ?? "UnknownSubject", group, PathSchema.QuarantineSchema);
                    foreach (var file in GetNirFileSet(group.NirFilePath)) {
                        // Camera 파일과 동일한 패턴: 존재 확인 후 이동
                        if (string.IsNullOrEmpty(file) || !File.Exists(file)) {
                            _logger.LogWarning("NIR file not found during delete, skipping: {File}", file);
                            continue;
                        }
                        var destFile = Path.Combine(destDir, Path.GetFileName(file));
                        _logger.LogDebug("Moving NIR file: {Src} -> {Dest}", file, destFile);
                        await MoveFileAtomicAsync(file, destFile, movedItems, ct);
                    }
                } else if (comp.StartsWith("Cam", StringComparison.OrdinalIgnoreCase)) {
                    var camKey = comp.ToLower();
                    if (group.CameraFiles.TryGetValue(camKey, out var path) && File.Exists(path)) {
                        var destDir = pathBuilder.BuildCameraFolderPath(quarantinePath, subject ?? "UnknownSubject", group, PathSchema.QuarantineSchema, camKey);
                        var destFile = Path.Combine(destDir, Path.GetFileName(path));
                        _logger.LogDebug("Moving Camera file ({Cam}): {Src} -> {Dest}", camKey, path, destFile);
                        await MoveFileAtomicAsync(path, destFile, movedItems, ct);
                    }
                }
            }
            result.Success = true;
            _logger.LogInformation("DeleteComponentsAsync completed: {Count} items moved to quarantine", movedItems.Count);
        } catch (Exception ex) {
            _logger.LogError(ex, "DeleteComponentsAsync failed, rolling back {Count} items", movedItems.Count);
            await RollbackAsync(movedItems);
            result.Success = false; result.ErrorMessage = ex.Message;
        }
        return result;
    }

    private async Task MoveFileAtomicAsync(string src, string dest, List<(string, string, bool)> tracking, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        await Task.Run(() => File.Copy(src, dest, true), ct);
        tracking.Add((src, dest, false));
        File.Delete(src);
    }

    private async Task MoveDirectoryAtomicAsync(string src, string dest, List<(string, string, bool)> tracking, CancellationToken ct)
    {
        // Ensure parent directory exists
        var parentDir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(parentDir)) Directory.CreateDirectory(parentDir);

        // Check if cross-volume move (Directory.Move doesn't work across volumes)
        var srcRoot = Path.GetPathRoot(src);
        var destRoot = Path.GetPathRoot(dest);
        var isCrossVolume = !string.Equals(srcRoot, destRoot, StringComparison.OrdinalIgnoreCase);

        await Task.Run(() => {
            if (isCrossVolume)
            {
                // Cross-volume: use copy + delete
                CopyDirectoryRecursive(src, dest);
                Directory.Delete(src, true);
            }
            else if (Directory.Exists(dest))
            {
                // Same volume but destination exists: merge by moving contents
                foreach (var file in Directory.GetFiles(src)) {
                    var destFile = Path.Combine(dest, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                    File.Delete(file);
                }
                foreach (var subDir in Directory.GetDirectories(src)) {
                    var destSubDir = Path.Combine(dest, Path.GetFileName(subDir));
                    Directory.Move(subDir, destSubDir);
                }
                Directory.Delete(src);
            }
            else
            {
                // Same volume, destination doesn't exist: atomic move
                Directory.Move(src, dest);
            }
        }, ct);
        tracking.Add((src, dest, true));
        _logger.LogInformation("Moved directory: {Src} -> {Dest} (CrossVolume={CrossVolume})", src, dest, isCrossVolume);
    }

    private static void CopyDirectoryRecursive(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
        {
            var destFile = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        foreach (var subDir in Directory.GetDirectories(src))
        {
            var destSubDir = Path.Combine(dest, Path.GetFileName(subDir));
            CopyDirectoryRecursive(subDir, destSubDir);
        }
    }

    private async Task RollbackAsync(List<(string source, string dest, bool isDirectory)> items)
    {
        foreach (var item in Enumerable.Reverse(items)) {
            try { if (item.isDirectory) Directory.Move(item.dest, item.source); else { File.Copy(item.dest, item.source, true); File.Delete(item.dest); } } catch { }
        }
    }

    private List<string> GetNirFileSet(string spcPath)
    {
        var files = new List<string> { spcPath };
        var dir = Path.GetDirectoryName(spcPath);
        
        // Deduce base name: if input ends with A.txt or .txt or .spc, strip it.
        var fileName = Path.GetFileName(spcPath);
        string key;
        
        if (fileName.EndsWith("A.txt", StringComparison.OrdinalIgnoreCase))
            key = fileName.Substring(0, fileName.Length - 5);
        else if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            key = fileName.Substring(0, fileName.Length - 4);
        else if (fileName.EndsWith(".spc", StringComparison.OrdinalIgnoreCase))
            key = fileName.Substring(0, fileName.Length - 4);
        else
            key = Path.GetFileNameWithoutExtension(spcPath);

        // Try to find both .spc, .txt, and A.txt
        var possibleExtensions = new[] { ".spc", ".txt", "A.txt" };
        foreach (var ext in possibleExtensions)
        {
            var p = Path.Combine(dir!, key + ext);
            if (File.Exists(p)) files.Add(p);
        }

        // Return distinct full paths to avoid duplicates (e.g. if spcPath was added initially and found again)
        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
