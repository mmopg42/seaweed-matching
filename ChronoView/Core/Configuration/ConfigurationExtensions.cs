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
    private static readonly Regex DateRegex = new Regex(@"(19|20)\d{6}", RegexOptions.Compiled);

    /// <summary>
    /// Updates all path-related properties in the configuration by replacing 8-digit date strings (e.g., 20260105) 
    /// with the current date.
    /// </summary>
    /// <param name="config">The configuration instance to update.</param>
    public static void UpdatePathsWithCurrentDate(this ApplicationConfiguration config)
    {
        if (config == null) return;

        string today = DateTime.Now.ToString("yyyyMMdd");

        // 1. Update BasePath
        config.BasePath = UpdatePath("BasePath", config.BasePath, today);

        // 2. Update MatchingSettings paths
        if (config.MatchingSettings != null)
        {
            config.MatchingSettings.Nir1Path = UpdatePath("Nir1Path", config.MatchingSettings.Nir1Path, today);
            config.MatchingSettings.Normal1Path = UpdatePath("Normal1Path", config.MatchingSettings.Normal1Path, today);
            config.MatchingSettings.Camera1Path = UpdatePath("Camera1Path", config.MatchingSettings.Camera1Path, today);
            config.MatchingSettings.Camera2Path = UpdatePath("Camera2Path", config.MatchingSettings.Camera2Path, today);
            config.MatchingSettings.Camera3Path = UpdatePath("Camera3Path", config.MatchingSettings.Camera3Path, today);
            config.MatchingSettings.Nir2Path = UpdatePath("Nir2Path", config.MatchingSettings.Nir2Path, today);
            config.MatchingSettings.Normal2Path = UpdatePath("Normal2Path", config.MatchingSettings.Normal2Path, today);
            config.MatchingSettings.Camera4Path = UpdatePath("Camera4Path", config.MatchingSettings.Camera4Path, today);
            config.MatchingSettings.Camera5Path = UpdatePath("Camera5Path", config.MatchingSettings.Camera5Path, today);
            config.MatchingSettings.Camera6Path = UpdatePath("Camera6Path", config.MatchingSettings.Camera6Path, today);
            config.MatchingSettings.OutputPath = UpdatePath("OutputPath", config.MatchingSettings.OutputPath, today);
        }

        // 3. Update WorkflowSettings paths
        if (config.WorkflowSettings != null)
        {
            config.WorkflowSettings.DeleteQuarantinePath = UpdatePath("DeleteQuarantinePath", config.WorkflowSettings.DeleteQuarantinePath, today);
        }

        // 4. Update ExternalProgramSettings paths
        if (config.ExternalProgramSettings != null)
        {
            config.ExternalProgramSettings.GeneralCameraProgramPath = UpdatePath("GeneralProgram", config.ExternalProgramSettings.GeneralCameraProgramPath, today);
            config.ExternalProgramSettings.Nir1ProgramPath = UpdatePath("Nir1Program", config.ExternalProgramSettings.Nir1ProgramPath, today);
            config.ExternalProgramSettings.Nir2ProgramPath = UpdatePath("Nir2Program", config.ExternalProgramSettings.Nir2ProgramPath, today);
            config.ExternalProgramSettings.Nir2FilterMonitorPath = UpdatePath("Nir2Monitor", config.ExternalProgramSettings.Nir2FilterMonitorPath, today);
            config.ExternalProgramSettings.Nir2FilterDestinationPath = UpdatePath("Nir2Dest", config.ExternalProgramSettings.Nir2FilterDestinationPath, today);
        }
    }

    private static string UpdatePath(string name, string path, string today)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        
        string newPath = DateRegex.Replace(path, today);
        if (newPath != path)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigDateUpdater] Updated {name}: {path} -> {newPath}");
        }
        return newPath;
    }
}
