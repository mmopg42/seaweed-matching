using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Implementation of path management service for path generation and folder creation.
/// </summary>
public class PathManagementService : IPathManagementService
{
    private readonly ILogger<PathManagementService> _logger;

    public PathManagementService(ILogger<PathManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Dictionary<string, string> GeneratePathsFromDate(string dateString, ApplicationConfiguration config)
    {
        _logger.LogInformation("Getting configured paths for date: {Date}", dateString);

        var paths = new Dictionary<string, string>();
        var settings = config.MatchingSettings;

        // 설정된 경로들 그대로 반환 (비어 있으면 빈 문자열)
        paths["NIR1"] = settings.Nir1Path ?? "";
        paths["Normal1"] = settings.Normal1Path ?? "";
        paths["Cam1"] = settings.Camera1Path ?? "";
        paths["Cam2"] = settings.Camera2Path ?? "";
        paths["Cam3"] = settings.Camera3Path ?? "";
        paths["NIR2"] = settings.Nir2Path ?? "";
        paths["Normal2"] = settings.Normal2Path ?? "";
        paths["Cam4"] = settings.Camera4Path ?? "";
        paths["Cam5"] = settings.Camera5Path ?? "";
        paths["Cam6"] = settings.Camera6Path ?? "";
        paths["Output"] = settings.OutputPath ?? "";

        var configuredCount = paths.Values.Count(p => !string.IsNullOrWhiteSpace(p));
        _logger.LogInformation("Retrieved {Count} configured paths out of {Total}", configuredCount, paths.Count);

        return paths;
    }

    /// <inheritdoc/>
    public bool ValidatePaths(Dictionary<string, string> paths)
    {
        if (paths == null || paths.Count == 0)
        {
            _logger.LogWarning("No paths to validate");
            return false;
        }
        
        _logger.LogInformation("Validating {Count} paths", paths.Count);
        
        var invalidChars = Path.GetInvalidPathChars();
        foreach (var kvp in paths)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value))
            {
                _logger.LogWarning("Path for {Key} is empty", kvp.Key);
                return false;
            }
            
            if (kvp.Value.IndexOfAny(invalidChars) >= 0)
            {
                _logger.LogWarning("Path for {Key} contains invalid characters: {Path}", 
                    kvp.Key, kvp.Value);
                return false;
            }
        }
        
        _logger.LogInformation("All paths validated successfully");
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> CreateSampleFoldersAsync(
        string sampleName,
        ApplicationConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating sample folders for: {SampleName}", sampleName);
        
        try
        {
            var settings = config.MatchingSettings;
            
            // ====== Line 1 + Line 2 전체 경로 수집 ======
            var basePaths = new[]
            {
                // Line 1
                settings.Nir1Path,
                settings.Normal1Path,
                settings.Camera1Path,
                settings.Camera2Path,
                settings.Camera3Path,
                // Line 2
                settings.Nir2Path,
                settings.Normal2Path,
                settings.Camera4Path,
                settings.Camera5Path,
                settings.Camera6Path
            }.Where(p => !string.IsNullOrEmpty(p));
            
            var createdCount = 0;
            var skippedCount = 0;
            
            foreach (var basePath in basePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var samplePath = Path.Combine(basePath, sampleName);
                
                if (!Directory.Exists(samplePath))
                {
                    await Task.Run(() => Directory.CreateDirectory(samplePath), 
                        cancellationToken);
                    createdCount++;
                    _logger.LogInformation("Created folder: {Path}", samplePath);
                }
                else
                {
                    skippedCount++;
                    _logger.LogDebug("Folder already exists, skipped: {Path}", samplePath);
                }
            }
            
            _logger.LogInformation(
                "Sample folder creation complete. Created: {Created}, Skipped: {Skipped}, Name: '{Name}'", 
                createdCount, skippedCount, sampleName);
            
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sample folder creation was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sample folders for '{SampleName}'", sampleName);
            return false;
        }
    }
}
