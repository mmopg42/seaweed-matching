using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
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
        _logger.LogInformation("Generating paths from date: {Date}", dateString);
        
        var paths = new Dictionary<string, string>();
        
        // 날짜 형식 검증
        if (!DateTime.TryParseExact(dateString, "yyyyMMdd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            _logger.LogWarning("Invalid date format: {Date}", dateString);
            return paths;
        }
        
        // 기본 경로 패턴: BasePath/{YYYY}/{MM}/{DD}/
        var basePath = !string.IsNullOrEmpty(config.BasePath) 
            ? config.BasePath 
            : "D:/Data";
        
        var datePath = Path.Combine(basePath, 
            date.Year.ToString(), 
            date.Month.ToString("D2"), 
            date.Day.ToString("D2"));
        
        // ====== Line 1 경로 ======
        paths["NIR1"] = Path.Combine(datePath, "NIR1");
        paths["Normal1"] = Path.Combine(datePath, "Normal1");
        paths["Cam1"] = Path.Combine(datePath, "Cam1");
        paths["Cam2"] = Path.Combine(datePath, "Cam2");
        paths["Cam3"] = Path.Combine(datePath, "Cam3");
        
        // ====== Line 2 경로 ======
        paths["NIR2"] = Path.Combine(datePath, "NIR2");
        paths["Normal2"] = Path.Combine(datePath, "Normal2");
        paths["Cam4"] = Path.Combine(datePath, "Cam4");
        paths["Cam5"] = Path.Combine(datePath, "Cam5");
        paths["Cam6"] = Path.Combine(datePath, "Cam6");
        
        // ====== 공통 경로 ======
        paths["Output"] = Path.Combine(datePath, "Output");
        
        _logger.LogInformation("Generated {Count} paths for date {Date}", paths.Count, dateString);
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
