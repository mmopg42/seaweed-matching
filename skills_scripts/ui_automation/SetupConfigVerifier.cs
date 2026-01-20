using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation;

/// <summary>
/// Verifies configuration consistency between data simulator settings and ChronoView WorkflowPanel settings.
/// Addresses SETUP-04 requirement for config validation.
/// </summary>
/// <remarks>
/// Reads simulator_config.json containing:
/// - source_line1, source_line2: Source data paths for Line 1 and Line 2
/// - target_base: Output base path
/// - move_folder: Move destination folder
/// - trash_folder: Trash/deletion folder
///
/// Compares with ChronoView SettingsDialog paths:
/// - Line 1: nir1, normal1, cam1, cam2, cam3 paths
/// - Line 2: nir2, normal2, cam4, cam5, cam6 paths
/// - Output path
/// - Quarantine path
/// </remarks>
public class SetupConfigVerifier : IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly ChronoWindowFinder _windowFinder;
    private readonly string _simulatorConfigPath;

    // Required keys in simulator config
    private static readonly string[] RequiredSimulatorKeys = new[]
    {
        "source_line1",
        "source_line2",
        "target_base",
        "move_folder",
        "trash_folder"
    };

    /// <summary>
    /// Initializes a new instance with the specified simulator config path.
    /// </summary>
    /// <param name="simulatorConfigPath">Path to simulator_config.json</param>
    public SetupConfigVerifier(string simulatorConfigPath)
    {
        _automation = new UIA3Automation();
        _windowFinder = new ChronoWindowFinder(_automation);
        _simulatorConfigPath = simulatorConfigPath ?? throw new ArgumentNullException(nameof(simulatorConfigPath));
    }

    /// <summary>
    /// Initializes a new instance with default simulator config path.
    /// </summary>
    public SetupConfigVerifier() : this("task_helper/data_test/dist/simulator_config.json")
    {
    }

    /// <summary>
    /// Reads and parses the simulator configuration file.
    /// </summary>
    /// <param name="configPath">Optional override path (uses instance path if null)</param>
    /// <returns>Dictionary of config values, or empty dictionary if file not found</returns>
    /// <exception cref="FileNotFoundException">When config file doesn't exist</exception>
    /// <exception cref="JsonException">When JSON is invalid</exception>
    public Dictionary<string, string> ReadSimulatorConfig(string? configPath = null)
    {
        var path = configPath ?? _simulatorConfigPath;

        if (!File.Exists(path))
        {
            Console.WriteLine($"[SetupConfigVerifier] Config file not found: {path}");
            throw new FileNotFoundException($"Simulator config file not found: {path}");
        }

        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);

            if (config == null)
            {
                Console.WriteLine($"[SetupConfigVerifier] Failed to parse config file: {path}");
                return new Dictionary<string, string>();
            }

            Console.WriteLine($"[SetupConfigVerifier] Loaded simulator config with {config.Count} keys");
            return config;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[SetupConfigVerifier] JSON parsing error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates that all required keys exist in the simulator config.
    /// </summary>
    /// <param name="config">The config dictionary to validate</param>
    /// <returns>List of missing required keys</returns>
    public List<string> ValidateRequiredKeys(Dictionary<string, string> config)
    {
        var missing = new List<string>();

        foreach (var key in RequiredSimulatorKeys)
        {
            if (!config.ContainsKey(key))
            {
                missing.Add(key);
            }
        }

        return missing;
    }

    /// <summary>
    /// Reads ChronoView paths from the SettingsDialog.
    /// </summary>
    /// <param name="openSettingsIfNeeded">If true, opens SettingsDialog if not already open</param>
    /// <returns>Dictionary of ChronoView path settings</returns>
    public Dictionary<string, string> ReadChronoViewPaths(bool openSettingsIfNeeded = false)
    {
        var paths = new Dictionary<string, string>();
        var controller = new ChronoSettingsController(_automation);

        var dialog = _windowFinder.FindSettingsDialog();

        if (dialog == null && openSettingsIfNeeded)
        {
            Console.WriteLine("[SetupConfigVerifier] SettingsDialog not open, attempting to open...");
            if (!controller.OpenSettingsDialog())
            {
                Console.WriteLine("[SetupConfigVerifier] Failed to open SettingsDialog");
                return paths;
            }
            dialog = _windowFinder.FindSettingsDialog();
        }

        if (dialog == null)
        {
            Console.WriteLine("[SetupConfigVerifier] Cannot read paths: SettingsDialog not open");
            return paths;
        }

        // Select Paths tab first
        if (!controller.SelectPathsTab())
        {
            Console.WriteLine("[SetupConfigVerifier] Failed to select Paths tab");
            return paths;
        }

        // Read Line 1 paths
        var line1Paths = controller.GetLine1Paths();
        foreach (var kvp in line1Paths)
        {
            paths[$"line1_{kvp.Key}"] = kvp.Value;
        }

        // Read Line 2 paths
        var line2Paths = controller.GetLine2Paths();
        foreach (var kvp in line2Paths)
        {
            paths[$"line2_{kvp.Key}"] = kvp.Value;
        }

        // Read output and quarantine paths
        paths["output"] = controller.GetOutputPath();
        paths["quarantine"] = controller.GetQuarantinePath();

        Console.WriteLine($"[SetupConfigVerifier] Read {paths.Count} paths from ChronoView");
        return paths;
    }

    /// <summary>
    /// Compares simulator config paths with ChronoView paths.
    /// </summary>
    /// <param name="simulatorConfig">Simulator configuration dictionary</param>
    /// <param name="chronoPaths">ChronoView paths dictionary</param>
    /// <returns>Verification result with match/mismatch details</returns>
    public VerificationResult ComparePaths(Dictionary<string, string> simulatorConfig, Dictionary<string, string> chronoPaths)
    {
        var result = new VerificationResult();
        var details = new Dictionary<string, PathComparisonDetail>();

        // Define path mappings from simulator to ChronoView
        // Simulator keys map to ChronoView keys for comparison
        var mappings = new Dictionary<string, string[]>
        {
            ["source_line1"] = new[] { "line1_nir1", "line1_normal1" },
            ["source_line2"] = new[] { "line2_nir2", "line2_normal2" },
            ["target_base"] = new[] { "output" },
            ["move_folder"] = new[] { "quarantine" },  // Move folder maps to quarantine/delete path
            ["trash_folder"] = new[] { "quarantine" }  // Trash folder also maps to quarantine
        };

        foreach (var (simKey, chronoKeys) in mappings)
        {
            if (!simulatorConfig.ContainsKey(simKey))
            {
                result.Missing.Add(simKey);
                continue;
            }

            var simPath = simulatorConfig[simKey];
            var normalizedSimPath = NormalizePath(simPath);

            bool anyMatch = false;
            foreach (var chronoKey in chronoKeys)
            {
                if (chronoPaths.ContainsKey(chronoKey))
                {
                    var chronoPath = chronoPaths[chronoKey];
                    var normalizedChronoPath = NormalizePath(chronoPath);

                    bool matches = normalizedSimPath.Equals(normalizedChronoPath, StringComparison.OrdinalIgnoreCase);

                    var detail = new PathComparisonDetail
                    {
                        SimulatorKey = simKey,
                        ChronoViewKey = chronoKey,
                        SimulatorPath = simPath,
                        ChronoViewPath = chronoPath,
                        Match = matches
                    };
                    details[$"{simKey}_vs_{chronoKey}"] = detail;

                    if (matches)
                    {
                        result.Matched.Add($"{simKey} -> {chronoKey}");
                        anyMatch = true;
                    }
                    else
                    {
                        result.Mismatches.Add(new PathMismatch
                        {
                            SimulatorKey = simKey,
                            ChronoViewKey = chronoKey,
                            SimulatorPath = simPath,
                            ChronoViewPath = chronoPath,
                            Reason = "Paths do not match after normalization"
                        });
                    }
                }
                else
                {
                    result.Missing.Add(chronoKey);
                }
            }

            if (!anyMatch && chronoKeys.All(k => chronoPaths.ContainsKey(k)))
            {
                // All potential comparisons were made but none matched
                // Already added to Mismatches above
            }
        }

        result.Details = details;
        result.Success = result.Mismatches.Count == 0 && result.Missing.Count == 0;

        return result;
    }

    /// <summary>
    /// Normalizes a path for comparison.
    /// Handles WSL paths, trailing slashes, and case differences.
    /// </summary>
    /// <param name="path">The path to normalize</param>
    /// <returns>Normalized path string</returns>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        string normalized = path;

        // Convert WSL paths (/mnt/c/...) to Windows paths (C:\...)
        if (normalized.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = normalized.Substring(5).Split('/', 2);
            if (parts.Length >= 1 && parts[0].Length == 1)
            {
                var driveLetter = parts[0].ToUpperInvariant();
                var rest = parts.Length > 1 ? parts[1].Replace('/', '\\') : string.Empty;
                normalized = $"{driveLetter}:\\{rest}";
            }
        }

        // Remove trailing slashes/backslashes
        normalized = normalized.TrimEnd('/', '\\');

        // Convert all separators to backslash (Windows style)
        normalized = normalized.Replace('/', '\\');

        return normalized;
    }

    /// <summary>
    /// Performs full verification: reads simulator config, reads ChronoView paths, compares them.
    /// </summary>
    /// <param name="openSettingsIfNeeded">If true, opens SettingsDialog if needed</param>
    /// <param name="configPath">Optional override config path</param>
    /// <returns>Verification result</returns>
    public VerificationResult Verify(bool openSettingsIfNeeded = false, string? configPath = null)
    {
        Console.WriteLine("[SetupConfigVerifier] Starting configuration verification...");

        // Read simulator config
        Dictionary<string, string> simulatorConfig;
        try
        {
            simulatorConfig = ReadSimulatorConfig(configPath);
        }
        catch (FileNotFoundException)
        {
            return new VerificationResult
            {
                Success = false,
                Missing = new List<string> { "simulator_config_file" },
                Error = "Simulator config file not found"
            };
        }

        // Validate required keys
        var missingKeys = ValidateRequiredKeys(simulatorConfig);
        if (missingKeys.Count > 0)
        {
            Console.WriteLine($"[SetupConfigVerifier] Missing required keys: {string.Join(", ", missingKeys)}");
        }

        // Read ChronoView paths
        var chronoPaths = ReadChronoViewPaths(openSettingsIfNeeded);

        if (chronoPaths.Count == 0)
        {
            Console.WriteLine("[SetupConfigVerifier] No ChronoView paths read");
            return new VerificationResult
            {
                Success = false,
                Missing = missingKeys,
                Error = "Failed to read ChronoView paths"
            };
        }

        // Compare paths
        var result = ComparePaths(simulatorConfig, chronoPaths);

        // Add missing keys to result
        foreach (var key in missingKeys)
        {
            if (!result.Missing.Contains(key))
            {
                result.Missing.Add(key);
            }
        }

        Console.WriteLine($"[SetupConfigVerifier] Verification complete: Success={result.Success}, Matched={result.Matched.Count}, Mismatches={result.Mismatches.Count}, Missing={result.Missing.Count}");

        return result;
    }

    /// <summary>
    /// Releases resources used by the UIA3 automation.
    /// </summary>
    public void Dispose()
    {
        _automation?.Dispose();
    }
}

/// <summary>
/// Result of configuration verification.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// True if all paths match and no required keys are missing.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of path mismatches with details.
    /// </summary>
    public List<PathMismatch> Mismatches { get; set; } = new();

    /// <summary>
    /// List of matched path mappings (formatted as "simKey -> chronoKey").
    /// </summary>
    public List<string> Matched { get; set; } = new();

    /// <summary>
    /// List of missing required keys or paths.
    /// </summary>
    public List<string> Missing { get; set; } = new();

    /// <summary>
    /// Detailed comparison for all path pairs.
    /// </summary>
    public Dictionary<string, PathComparisonDetail> Details { get; set; } = new();

    /// <summary>
    /// Error message if verification failed catastrophically.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Converts the result to JSON string.
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var output = new
        {
            success = Success,
            data = Success || Error == null ? new
            {
                allMatch = Mismatches.Count == 0,
                matched = Matched,
                mismatches = Mismatches.Select(m => new
                {
                    simulatorKey = m.SimulatorKey,
                    chronoViewKey = m.ChronoViewKey,
                    simulatorPath = m.SimulatorPath,
                    chronoViewPath = m.ChronoViewPath,
                    reason = m.Reason
                }),
                missing = Missing,
                details = Details.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        simulatorKey = kvp.Value.SimulatorKey,
                        chronoViewKey = kvp.Value.ChronoViewKey,
                        simulatorPath = kvp.Value.SimulatorPath,
                        chronoViewPath = kvp.Value.ChronoViewPath,
                        match = kvp.Value.Match
                    })
            } : null,
            error = Error
        };

        return JsonSerializer.Serialize(output, options);
    }
}

/// <summary>
/// Details of a path comparison between simulator and ChronoView.
/// </summary>
public class PathComparisonDetail
{
    /// <summary>
    /// Key from simulator config.
    /// </summary>
    public string SimulatorKey { get; set; } = string.Empty;

    /// <summary>
    /// Key from ChronoView settings.
    /// </summary>
    public string ChronoViewKey { get; set; } = string.Empty;

    /// <summary>
    /// Path value from simulator config.
    /// </summary>
    public string SimulatorPath { get; set; } = string.Empty;

    /// <summary>
    /// Path value from ChronoView settings.
    /// </summary>
    public string ChronoViewPath { get; set; } = string.Empty;

    /// <summary>
    /// True if paths match after normalization.
    /// </summary>
    public bool Match { get; set; }
}

/// <summary>
/// Represents a path mismatch between simulator and ChronoView.
/// </summary>
public class PathMismatch
{
    /// <summary>
    /// Key from simulator config.
    /// </summary>
    public string SimulatorKey { get; set; } = string.Empty;

    /// <summary>
    /// Key from ChronoView settings.
    /// </summary>
    public string ChronoViewKey { get; set; } = string.Empty;

    /// <summary>
    /// Path value from simulator config.
    /// </summary>
    public string SimulatorPath { get; set; } = string.Empty;

    /// <summary>
    /// Path value from ChronoView settings.
    /// </summary>
    public string ChronoViewPath { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable reason for mismatch.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
