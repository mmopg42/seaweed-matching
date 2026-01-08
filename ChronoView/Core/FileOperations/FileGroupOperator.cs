using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

public class FileGroupOperator : IFileGroupOperator
{
    private readonly ILogger<FileGroupOperator> _logger;

    public FileGroupOperator(ILogger<FileGroupOperator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult> ExecuteOpAsync(FileGroup group, string targetBase, OpType opType, PathSchema schema, string? subject = null, IProgress<OperationProgress>? progress = null, CancellationToken ct = default)
    {
        var result = new OperationResult();
        var movedItems = new List<(string source, string dest, bool isDirectory)>();
        _logger.LogInformation("ExecuteOpAsync started: GroupId={GroupId}, TargetBase={TargetBase}, OpType={OpType}, Schema={Schema}, Subject={Subject}", group.GroupId, targetBase, opType, schema, subject ?? "null");
        try {
            if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
                var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
                string? folderName = null;
                // MoveSchema와 QuarantineSchema 모두에서 folderName 추출
                if (schema == PathSchema.QuarantineSchema || schema == PathSchema.MoveSchema)
                {
                    folderName = Path.GetFileName(group.NormalFolder);
                }

                var destPath = BuildPath(targetBase, group, role, schema, subject, folderName);
                _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
                await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
            }
            if (group.HasNir && !string.IsNullOrEmpty(group.NirFilePath)) {
                var role = schema == PathSchema.MoveSchema ? "Nir" : (group.LineNumber == 1 ? "nir1" : "nir2");
                var destDir = BuildPath(targetBase, group, role, schema, subject);
                _logger.LogInformation("Moving NIR files to: {Dest}", destDir);
                foreach (var file in GetNirFileSet(group.NirFilePath)) await MoveFileAtomicAsync(file, Path.Combine(destDir, Path.GetFileName(file)), movedItems, ct);
            }
            foreach (var cam in group.CameraFiles) {
                if (string.IsNullOrEmpty(cam.Value) || !File.Exists(cam.Value)) continue;
                var destDir = BuildPath(targetBase, group, cam.Key, schema, subject);
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
        try {
            foreach (var comp in components) {
                if (comp == "Normal" && !string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
                    var role = group.LineNumber == 1 ? "일반1" : "일반2";
                    var folderName = Path.GetFileName(group.NormalFolder);
                    await MoveDirectoryAtomicAsync(group.NormalFolder, BuildPath(quarantinePath, group, role, PathSchema.QuarantineSchema, subject, folderName), movedItems, ct);
                } else if (comp == "Nir" && group.HasNir && !string.IsNullOrEmpty(group.NirFilePath)) {
                    var role = group.LineNumber == 1 ? "nir1" : "nir2";
                    var destDir = BuildPath(quarantinePath, group, role, PathSchema.QuarantineSchema, subject);
                    foreach (var file in GetNirFileSet(group.NirFilePath)) await MoveFileAtomicAsync(file, Path.Combine(destDir, Path.GetFileName(file)), movedItems, ct);
                } else if (comp.StartsWith("Cam", StringComparison.OrdinalIgnoreCase)) {
                    var camKey = comp.ToLower();
                    if (group.CameraFiles.TryGetValue(camKey, out var path) && File.Exists(path)) {
                        var destDir = BuildPath(quarantinePath, group, camKey, PathSchema.QuarantineSchema, subject);
                        await MoveFileAtomicAsync(path, Path.Combine(destDir, Path.GetFileName(path)), movedItems, ct);
                    }
                }
            }
            result.Success = true;
        } catch (Exception ex) {
            await RollbackAsync(movedItems);
            result.Success = false; result.ErrorMessage = ex.Message;
        }
        return result;
    }

    /// <summary>
    /// Checks if the path contains a valid yyyyMMdd date segment.
    /// Validates each path segment using DateTime.TryParseExact to ensure it's a real date.
    /// </summary>
    private static bool HasValidDateSegment(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var datePattern = new Regex("^(19|20)\\d{6}$", RegexOptions.Compiled);

        foreach (var segment in segments)
        {
            // Quick filter: must be 8 characters matching yyyyMMdd pattern
            if (segment.Length == 8 && datePattern.IsMatch(segment))
            {
                // Final validation: must be a valid date
                if (DateTime.TryParseExact(segment, "yyyyMMdd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Ensures the base path has a date segment. If not, adds today's date.
    /// </summary>
    private static string EnsureDateRoot(string basePath)
    {
        if (HasValidDateSegment(basePath))
        {
            return basePath;
        }

        var today = DateTime.Now.ToString("yyyyMMdd");
        return Path.Combine(basePath, today);
    }

    private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
    {
        var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
        
        // Calculate baseRoot: use basePath as-is if it contains a valid date, otherwise add today's date
        var baseRoot = EnsureDateRoot(basePath);
        
        if (schema == PathSchema.QuarantineSchema) {
            // QuarantineSchema: baseRoot/Line{N}/subject/role[/folderName]
            var baseQuarantinePath = Path.Combine(baseRoot, $"Line{group.LineNumber}", subj, role);
            if (!string.IsNullOrEmpty(folderName))
            {
                return Path.Combine(baseQuarantinePath, folderName);
            }
            
            return baseQuarantinePath;
        } else {
            // MoveSchema: baseRoot/subject/... (no line separation)
            var nir = group.HasNir ? "with NIR" : "without NIR";
            if (role.StartsWith("cam")) return Path.Combine(baseRoot, subj, nir, "복합 카메라", role);
            
            // MoveSchema에서 일반 카메라 경로 생성 시 folderName 포함
            if (role == "일반" || role == "일반2") 
            {
                var baseRolePath = Path.Combine(baseRoot, subj, nir, group.HasNir ? role : $"{role} 카메라");
                if (!string.IsNullOrEmpty(folderName))
                {
                    return Path.Combine(baseRolePath, folderName);
                }
                return baseRolePath;
            }
            
            return Path.Combine(baseRoot, subj, nir, role);
        }
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
        
        // Move entire directory at once (atomic if on same volume)
        await Task.Run(() => {
            if (Directory.Exists(dest)) {
                // If destination exists, merge by moving contents
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
            } else {
                Directory.Move(src, dest);
            }
        }, ct);
        tracking.Add((src, dest, true));
        _logger.LogInformation("Moved directory: {Src} -> {Dest}", src, dest);
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
