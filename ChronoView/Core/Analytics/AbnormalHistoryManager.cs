using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ChronoView.Core.Configuration;

namespace ChronoView.Core.Analytics;

/// <summary>
/// Manages persistent storage of aspect ratio history for abnormal detection.
/// Data is grouped by context (e.g., "Line1_Cam1") for targeted statistical analysis.
/// </summary>
public class AbnormalHistoryManager
{
    private readonly ILogger<AbnormalHistoryManager>? _logger;
    private readonly string _historyFilePath;
    private readonly object _lock = new();
    
    private Dictionary<string, List<double>> _history = new();

    /// <summary>
    /// Default path: IConfigurationManager.HistoryFilePath or %APPDATA%\ChronoView\abnormal_history.json
    /// </summary>
    public AbnormalHistoryManager(IConfigurationManager? configurationManager = null, ILogger<AbnormalHistoryManager>? logger = null, string? customPath = null)
    {
        _logger = logger;
        
        if (!string.IsNullOrEmpty(customPath))
        {
            _historyFilePath = customPath;
        }
        else if (configurationManager != null)
        {
            _historyFilePath = configurationManager.HistoryFilePath;
        }
        else
        {
            _historyFilePath = PathHelper.HistoryFilePath;
        }
        
        Load();
    }

    /// <summary>
    /// Load history from disk.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    _history = JsonSerializer.Deserialize<Dictionary<string, List<double>>>(json) 
                               ?? new Dictionary<string, List<double>>();
                    _logger?.LogInformation("Loaded abnormal history: {Count} contexts", _history.Count);
                }
                else
                {
                    _history = new Dictionary<string, List<double>>();
                    _logger?.LogDebug("No history file found, starting fresh");
                }
            }
            catch (Exception ex)
            {
                // Old format (SizeData) or corrupted file - reset
                _logger?.LogWarning(ex, "Failed to load abnormal history (format changed?), resetting history");
                _history = new Dictionary<string, List<double>>();
                // Delete old file to avoid repeated errors
                try { File.Delete(_historyFilePath); } catch { /* ignore */ }
            }
        }
    }

    /// <summary>
    /// Save history to disk.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFilePath, json);
                _logger?.LogDebug("Saved abnormal history: {Count} contexts", _history.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save abnormal history to {Path}", _historyFilePath);
            }
        }
    }

    /// <summary>
    /// Add an aspect ratio entry for a context and return the updated history for that context.
    /// Maintains window size limit.
    /// </summary>
    public List<double> Add(string context, double ratio, int windowSize)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(context, out var list))
            {
                list = new List<double>();
                _history[context] = list;
            }

            list.Add(ratio);

            // Maintain window size
            while (list.Count > windowSize)
            {
                list.RemoveAt(0);
            }

            return new List<double>(list); // Return a copy
        }
    }

    /// <summary>
    /// Get history for a specific context.
    /// </summary>
    public List<double> GetHistory(string context)
    {
        lock (_lock)
        {
            if (_history.TryGetValue(context, out var list))
            {
                return new List<double>(list);
            }
            return new List<double>();
        }
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _history.Clear();
            Save();
        }
    }

    /// <summary>
    /// Clear history for a specific context.
    /// </summary>
    /// <param name="context">The context to clear.</param>
    public void ClearContext(string context)
    {
        lock (_lock)
        {
            if (_history.Remove(context))
            {
                Save();
            }
        }
    }
    
    /// <summary>
    /// Get all context keys.
    /// </summary>
    public IReadOnlyCollection<string> GetContextKeys()
    {
        lock (_lock)
        {
            return _history.Keys.ToArray();
        }
    }
}
