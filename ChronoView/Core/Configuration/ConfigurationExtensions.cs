using ChronoView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChronoView.Core.Configuration;

/// <summary>
/// Extension methods for ApplicationConfiguration to handle path updates.
/// </summary>
public static class ConfigurationExtensions
{
    // Pattern for yyyyMMdd format (compact, no separators)
    private static readonly Regex DateRegex = new Regex(@"(19|20)\d{6}", RegexOptions.Compiled);
    
    // Pattern for yyyy\MM\dd or yyyy/MM/dd format (with separators)
    private static readonly Regex SlashDateRegex = new Regex(@"\d{4}[\\/]\d{2}[\\/]\d{2}", RegexOptions.Compiled);

    /// <summary>
    /// Updates all path-related properties in the configuration by replacing 8-digit date strings (e.g., 20260105) 
    /// with the current date.
    /// </summary>
    /// <param name="config">The configuration instance to update.</param>
    public static void UpdatePathsWithCurrentDate(this ApplicationConfiguration config)
    {
        if (config == null) return;

        string todayCompact = DateTime.Now.ToString("yyyyMMdd");
        string todaySlash = DateTime.Now.ToString(@"yyyy\\MM\\dd");

        // 1. Update BasePath (yyyyMMdd format)
        config.BasePath = UpdatePath("BasePath", config.BasePath, todayCompact, DateRegex);

        // 2. Update MatchingSettings paths (yyyyMMdd format)
        if (config.MatchingSettings != null)
        {
            config.MatchingSettings.Nir1Path = UpdatePath("Nir1Path", config.MatchingSettings.Nir1Path, todayCompact, DateRegex);
            config.MatchingSettings.Normal1Path = UpdatePath("Normal1Path", config.MatchingSettings.Normal1Path, todayCompact, DateRegex);
            config.MatchingSettings.Camera1Path = UpdatePath("Camera1Path", config.MatchingSettings.Camera1Path, todayCompact, DateRegex);
            config.MatchingSettings.Camera2Path = UpdatePath("Camera2Path", config.MatchingSettings.Camera2Path, todayCompact, DateRegex);
            config.MatchingSettings.Camera3Path = UpdatePath("Camera3Path", config.MatchingSettings.Camera3Path, todayCompact, DateRegex);
            config.MatchingSettings.Nir2Path = UpdatePath("Nir2Path", config.MatchingSettings.Nir2Path, todayCompact, DateRegex);
            config.MatchingSettings.Normal2Path = UpdatePath("Normal2Path", config.MatchingSettings.Normal2Path, todayCompact, DateRegex);
            config.MatchingSettings.Camera4Path = UpdatePath("Camera4Path", config.MatchingSettings.Camera4Path, todayCompact, DateRegex);
            config.MatchingSettings.Camera5Path = UpdatePath("Camera5Path", config.MatchingSettings.Camera5Path, todayCompact, DateRegex);
            config.MatchingSettings.Camera6Path = UpdatePath("Camera6Path", config.MatchingSettings.Camera6Path, todayCompact, DateRegex);
            config.MatchingSettings.OutputPath = UpdatePath("OutputPath", config.MatchingSettings.OutputPath, todayCompact, DateRegex);
        }

        // 3. Update WorkflowSettings paths (yyyyMMdd format)
        if (config.WorkflowSettings != null)
        {
            config.WorkflowSettings.DeleteQuarantinePath = UpdatePath("DeleteQuarantinePath", config.WorkflowSettings.DeleteQuarantinePath, todayCompact, DateRegex);
        }

        // 4. Update ExternalProgramSettings paths
        if (config.ExternalProgramSettings != null)
        {
            // Program paths typically don't have date in them, but check anyway
            config.ExternalProgramSettings.GeneralCameraProgramPath = UpdatePath("GeneralProgram", config.ExternalProgramSettings.GeneralCameraProgramPath, todayCompact, DateRegex);
            config.ExternalProgramSettings.Nir1ProgramPath = UpdatePath("Nir1Program", config.ExternalProgramSettings.Nir1ProgramPath, todayCompact, DateRegex);
            config.ExternalProgramSettings.Nir2ProgramPath = UpdatePath("Nir2Program", config.ExternalProgramSettings.Nir2ProgramPath, todayCompact, DateRegex);
            
            // NIR Filter paths use yyyy\MM\dd format with backslash separators
            config.ExternalProgramSettings.Nir2FilterMonitorPath = UpdatePath("Nir2Monitor", config.ExternalProgramSettings.Nir2FilterMonitorPath, todaySlash, SlashDateRegex);
            config.ExternalProgramSettings.Nir2FilterDestinationPath = UpdatePath("Nir2Dest", config.ExternalProgramSettings.Nir2FilterDestinationPath, todaySlash, SlashDateRegex);
        }
    }

    private static string UpdatePath(string name, string path, string replacement, Regex pattern)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        
        string newPath = pattern.Replace(path, replacement);
        if (newPath != path)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigDateUpdater] Updated {name}: {path} -> {newPath}");
        }
        return newPath;
    }
}
