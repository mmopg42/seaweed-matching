using ChronoView.Models;

namespace ChronoView.Core.Configuration;

/// <summary>
/// Provides factory default configuration for the application.
/// Ensures all paths are set to empty string for safety.
/// </summary>
public static class DefaultConfiguration
{
    public static ApplicationConfiguration GetDefault()
    {
        var config = new ApplicationConfiguration();
        
        // Ensure all paths are empty
        config.BasePath = string.Empty;
        
        // Folder Paths
        config.FolderPaths.Clear();
        
        // Matching Settings - Paths
        config.MatchingSettings.Nir1Path = string.Empty;
        config.MatchingSettings.Normal1Path = string.Empty;
        config.MatchingSettings.Nir2Path = string.Empty;
        config.MatchingSettings.Normal2Path = string.Empty;
        
        config.MatchingSettings.Camera1Path = string.Empty;
        config.MatchingSettings.Camera2Path = string.Empty;
        config.MatchingSettings.Camera3Path = string.Empty;
        config.MatchingSettings.Camera4Path = string.Empty;
        config.MatchingSettings.Camera5Path = string.Empty;
        config.MatchingSettings.Camera6Path = string.Empty;
        
        config.MatchingSettings.OutputPath = string.Empty;
        
        // External Program Settings
        config.ExternalProgramSettings.GeneralCameraProgramPath = string.Empty;
        config.ExternalProgramSettings.Nir1ProgramPath = string.Empty;
        config.ExternalProgramSettings.Nir2ProgramPath = string.Empty;
        config.ExternalProgramSettings.Nir2FilterMonitorPath = string.Empty;
        config.ExternalProgramSettings.Nir2FilterDestinationPath = string.Empty;
        
        // Workflow Settings
        config.WorkflowSettings.DeleteQuarantinePath = string.Empty;

        return config;
    }
}
