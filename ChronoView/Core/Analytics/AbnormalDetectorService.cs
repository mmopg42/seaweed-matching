using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.Analytics;

/// <summary>
/// Service for detecting abnormal conditions in file groups using aspect ratio deviation.
/// Identifies context from FileGroup.NormalFolder (e.g., "Line1_Cam1") and maintains per-context history.
/// </summary>
public class AbnormalDetectorService : IAbnormalDetector
{
    private readonly ILogger<AbnormalDetectorService>? _logger;
    private readonly IConfigurationManager? _configManager;
    private readonly AbnormalHistoryManager _historyManager;
    private readonly object _lock = new();
    
    // Context pattern: (Line[12]).*(Cam[1-6])
    private static readonly Regex ContextPattern = new(@"(Line[12]).*(Cam[1-6])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FallbackLinePattern = new(@"(Line[12])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FallbackCamPattern = new(@"(Cam[1-6])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Minimum samples required before enabling detection. Default: 5.
    /// </summary>
    private const int MinSamplesDefault = 5;

    /// <inheritdoc />
    public int WindowSize { get; set; }

    /// <inheritdoc />
    public int MinSamples { get; set; }

    /// <inheritdoc />
    public double Threshold { get; set; }

    /// <summary>
    /// Creates a new instance with DI dependencies.
    /// </summary>
    public AbnormalDetectorService(
        IConfigurationManager configManager,
        AbnormalHistoryManager historyManager,
        ILogger<AbnormalDetectorService>? logger = null)
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        _logger = logger;
        
        // Load initial config
        RefreshConfiguration();
        
        // Subscribe to changes
        _configManager.ConfigurationChanged += OnConfigurationChanged;
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        RefreshConfiguration();
    }

    /// <summary>
    /// Creates a new instance with explicit parameters (for testing or fallback).
    /// </summary>
    public AbnormalDetectorService(int windowSize = 40, int minSamples = 5, double threshold = 0.3)
    {
        WindowSize = windowSize;
        MinSamples = minSamples;
        Threshold = threshold;
        _historyManager = new AbnormalHistoryManager();
    }

    /// <summary>
    /// Refresh configuration from manager.
    /// </summary>
    private void RefreshConfiguration()
    {
        if (_configManager != null)
        {
            var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
            WindowSize = config.MatchingSettings?.AbnormalDetectionWindowSize ?? 40;
            Threshold = config.MatchingSettings?.AbnormalRatioThreshold ?? 0.3;
            MinSamples = MinSamplesDefault;
        }
    }
    
    /// <inheritdoc />
    public (bool IsAbnormal, double? RatioDiff) AddAndCheckImage(int width, int height, string context)
    {
        // Input validation
        if (width <= 0 || height <= 0)
        {
            return (false, null);
        }
        
        if (string.IsNullOrWhiteSpace(context))
        {
            context = "Global";
        }

        lock (_lock)
        {
            // Calculate current aspect ratio
            double currentRatio = width / (double)height;
            
            // Get current history (before adding new ratio)
            var historyBefore = _historyManager.GetHistory(context);
            
            // 1. Warm-up Phase: Not enough samples yet
            if (historyBefore.Count < MinSamples)
            {
                _historyManager.Add(context, currentRatio, WindowSize);
                _historyManager.Save();
                _logger?.LogDebug("Building baseline for context {Context}: {Count}/{Min} samples", 
                    context, historyBefore.Count + 1, MinSamples);
                return (false, 0.0);
            }

            // 2. Calculate Median Ratio from history
            double medianRatio = GetMedian(historyBefore);
            
            // 3. Calculate Ratio Difference (Absolute)
            double ratioDiff = Math.Abs(currentRatio - medianRatio);

            // 4. Compare with Threshold
            bool isAbnormal = ratioDiff > Threshold;

            // 5. Decision Logic
            if (isAbnormal)
            {
                _logger?.LogWarning("Abnormal detected for {Context}: Ratio={R:F3} vs Median={M:F3}, Diff={D:F3}, Threshold={T:F2}",
                    context, currentRatio, medianRatio, ratioDiff, Threshold);

                // CLEAN BASELINE POLICY: Do NOT add abnormal samples to history
                // User must manually reset if permanent ratio change occurred
                return (true, ratioDiff);
            }
            else
            {
                // Normal - add to history
                _historyManager.Add(context, currentRatio, WindowSize);
                _historyManager.Save();

                _logger?.LogDebug("Normal for {Context}: Ratio={R:F3}, Diff={D:F3}",
                    context, currentRatio, ratioDiff);
                
                return (false, ratioDiff);
            }
        }
    }

    /// <summary>
    /// Extract context identifier from FileGroup.NormalFolder path.
    /// </summary>
    /// <param name="normalFolder">Path to Normal folder.</param>
    /// <returns>Context string like "Line1_Cam1" or "Global" as fallback.</returns>
    public static string ExtractContext(string? normalFolder)
    {
        if (string.IsNullOrEmpty(normalFolder))
            return "Global";

        // Primary pattern: Line1_Cam1, Line2_Cam3, etc.
        var match = ContextPattern.Match(normalFolder);
        if (match.Success)
        {
            var line = match.Groups[1].Value.ToUpper();
            var cam = match.Groups[2].Value.ToUpper();
            return $"{line}_{cam}";
        }

        // Fallback: Find Line or Cam separately
        var lineMatch = FallbackLinePattern.Match(normalFolder);
        var camMatch = FallbackCamPattern.Match(normalFolder);

        if (lineMatch.Success && camMatch.Success)
        {
            return $"{lineMatch.Groups[1].Value.ToUpper()}_{camMatch.Groups[1].Value.ToUpper()}";
        }
        else if (camMatch.Success)
        {
            return camMatch.Groups[1].Value.ToUpper();
        }
        else if (lineMatch.Success)
        {
            return lineMatch.Groups[1].Value.ToUpper();
        }

        return "Global";
    }

    /// <inheritdoc />
    public bool IsGroupAbnormal(FileGroup group)
    {
        // Check for NIR-only groups (NIR present but no camera data)
        bool hasCamera = !string.IsNullOrEmpty(group.NormalFolder) || 
                        (group.CameraFiles != null && group.CameraFiles.Count > 0);
        bool hasNir = group.HasNir && !string.IsNullOrEmpty(group.NirKey);

        // NIR-only groups are considered abnormal
        if (!hasCamera && hasNir)
        {
            _logger?.LogDebug("Group {GroupId} detected as NIR-only abnormal", group.GroupId);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_lock)
        {
            _historyManager.Clear();
            _logger?.LogInformation("AbnormalDetectorService reset");
        }
    }

    /// <summary>
    /// Calculate median for a collection of ratio values.
    /// </summary>
    private static double GetMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0) return 0;
        
        int count = sorted.Count;
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        else
        {
            return sorted[count / 2];
        }
    }
}
