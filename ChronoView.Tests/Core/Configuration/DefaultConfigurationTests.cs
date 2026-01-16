using Xunit;
using ChronoView.Core.Configuration;
using System.Linq;

namespace ChronoView.Tests.Core.Configuration;

public class DefaultConfigurationTests
{
    [Fact]
    public void GetDefault_ReturnsConfigurationWithEmptyPaths()
    {
        // Act
        var config = DefaultConfiguration.GetDefault();

        // Assert
        Assert.NotNull(config);
        
        // Base Path must be overridden to empty (default is "D:/Data")
        Assert.Equal(string.Empty, config.BasePath);
        Assert.Empty(config.FolderPaths);
        
        // Matching Settings Paths
        Assert.Equal(string.Empty, config.MatchingSettings.Nir1Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Normal1Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Nir2Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Normal2Path);
        
        Assert.Equal(string.Empty, config.MatchingSettings.Camera1Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Camera2Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Camera3Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Camera4Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Camera5Path);
        Assert.Equal(string.Empty, config.MatchingSettings.Camera6Path);
        Assert.Equal(string.Empty, config.MatchingSettings.OutputPath);

        // External Programs
        Assert.Equal(string.Empty, config.ExternalProgramSettings.GeneralCameraProgramPath);
        Assert.Equal(string.Empty, config.ExternalProgramSettings.Nir1ProgramPath);
        Assert.Equal(string.Empty, config.ExternalProgramSettings.Nir2ProgramPath);
        Assert.Equal(string.Empty, config.ExternalProgramSettings.Nir2FilterMonitorPath);
        Assert.Equal(string.Empty, config.ExternalProgramSettings.Nir2FilterDestinationPath);
        
        // Workflow Settings
        Assert.Equal(string.Empty, config.WorkflowSettings.DeleteQuarantinePath);
    }

    [Fact]
    public void GetDefault_PreservesStandardDefaults()
    {
        // Act
        var config = DefaultConfiguration.GetDefault();

        // Assert
        // Check some standard defaults are not lost during initialization
        Assert.True(config.MatchingSettings.EnableAbnormalDetection);
        Assert.Equal(30, config.WorkflowSettings.LogRetentionDays);
    }
}
