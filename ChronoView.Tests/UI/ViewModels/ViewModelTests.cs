using ChronoView.Models;
using ChronoView.UI.ViewModels;
using ChronoView.Core.Configuration;
using ChronoView.Core.ImageProcessing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChronoView.Tests.UI.ViewModels;

/// <summary>
/// Unit tests for ViewModel logic including property notifications and command execution.
/// Tests Requirements: 1.3, 5.1
/// </summary>
public class ViewModelTests
{
    private static IImageProcessor CreateMockImageProcessor()
    {
        var mock = new Mock<IImageProcessor>();
        mock.Setup(x => x.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());
        mock.Setup(x => x.GetPlaceholderImage(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Array.Empty<byte>());
        return mock.Object;
    }

    #region ViewModelBase Tests

    [Fact]
    public void ViewModelBase_SetProperty_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(TestViewModel.TestProperty))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.TestProperty = "New Value";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("New Value", viewModel.TestProperty);
    }

    [Fact]
    public void ViewModelBase_SetProperty_DoesNotRaisePropertyChangedWhenValueUnchanged()
    {
        // Arrange
        var viewModel = new TestViewModel { TestProperty = "Initial" };
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (sender, args) => propertyChangedCount++;

        // Act
        viewModel.TestProperty = "Initial";

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    private class TestViewModel : ViewModelBase
    {
        private string _testProperty = string.Empty;

        public string TestProperty
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value);
        }
    }

    #endregion

    #region RelayCommand Tests

    [Fact]
    public void RelayCommand_Execute_CallsAction()
    {
        // Arrange
        var executed = false;
        var command = new RelayCommand(() => executed = true);

        // Act
        command.Execute(null);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void RelayCommand_CanExecute_ReturnsCorrectValue()
    {
        // Arrange
        var canExecute = false;
        var command = new RelayCommand(() => { }, () => canExecute);

        // Act & Assert
        Assert.False(command.CanExecute(null));

        canExecute = true;
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RelayCommand_WithParameter_ExecutesWithCorrectParameter()
    {
        // Arrange
        string? receivedParameter = null;
        var command = new RelayCommand<string>(param => receivedParameter = param);

        // Act
        command.Execute("test");

        // Assert
        Assert.Equal("test", receivedParameter);
    }

    #endregion

    #region FileGroupViewModel Tests

    [Fact]
    public void FileGroupViewModel_Constructor_InitializesPropertiesFromModel()
    {
        // Arrange
        var fileGroup = new FileGroup
        {
            GroupId = "G001",
            NirKey = "NIR001",
            NormalFolder = "/path/to/normal",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete,
            CameraFiles = new Dictionary<string, string>
            {
                { "cam1", "/path/to/cam1.jpg" },
                { "cam2", "/path/to/cam2.jpg" }
            }
        };

        // Act
        var viewModel = new FileGroupViewModel(fileGroup, CreateMockImageProcessor());

        // Assert
        Assert.Equal("G001", viewModel.GroupId);
        Assert.Equal("NIR001", viewModel.NirKey);
        Assert.Equal("/path/to/normal", viewModel.NormalFolder);
        Assert.Equal(1, viewModel.LineNumber);
        Assert.True(viewModel.HasNir);
        Assert.Equal(GroupStatus.Complete, viewModel.Status);
        Assert.Equal("/path/to/cam1.jpg", viewModel.Camera1ImagePath);
        Assert.Equal("/path/to/cam2.jpg", viewModel.Camera2ImagePath);
    }

    [Fact]
    public void FileGroupViewModel_IsSelected_RaisesPropertyChanged()
    {
        // Arrange
        var fileGroup = new FileGroup { GroupId = "G001" };
        var viewModel = new FileGroupViewModel(fileGroup, CreateMockImageProcessor());
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(FileGroupViewModel.IsSelected))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.IsSelected = true;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.True(viewModel.IsSelected);
    }

    [Fact]
    public void FileGroupViewModel_StatusText_ReturnsCorrectDisplayText()
    {
        // Arrange & Act & Assert
        var completeGroup = new FileGroup { GroupId = "G001", Status = GroupStatus.Complete };
        var completeViewModel = new FileGroupViewModel(completeGroup, CreateMockImageProcessor());
        Assert.Equal("✓ Complete", completeViewModel.StatusText);

        var errorGroup = new FileGroup { GroupId = "G002", Status = GroupStatus.Error };
        var errorViewModel = new FileGroupViewModel(errorGroup, CreateMockImageProcessor());
        Assert.Equal("✗ Error", errorViewModel.StatusText);

        var abnormalGroup = new FileGroup { GroupId = "G003", Status = GroupStatus.Abnormal };
        var abnormalViewModel = new FileGroupViewModel(abnormalGroup, CreateMockImageProcessor());
        Assert.Equal("⚠ Abnormal", abnormalViewModel.StatusText);
    }

    [Fact]
    public void FileGroupViewModel_SetCameraImagePath_UpdatesCorrectCamera()
    {
        // Arrange
        var fileGroup = new FileGroup { GroupId = "G001" };
        var viewModel = new FileGroupViewModel(fileGroup, CreateMockImageProcessor());

        // Act
        viewModel.SetCameraImagePath(3, "/path/to/cam3.jpg");

        // Assert
        Assert.Equal("/path/to/cam3.jpg", viewModel.GetCameraImagePath(3));
        Assert.Equal("/path/to/cam3.jpg", viewModel.Camera3ImagePath);
    }

    [Fact]
    public void FileGroupViewModel_Refresh_UpdatesAllProperties()
    {
        // Arrange
        var fileGroup = new FileGroup
        {
            GroupId = "G001",
            Status = GroupStatus.Pending
        };
        var viewModel = new FileGroupViewModel(fileGroup, CreateMockImageProcessor());

        // Modify the underlying model
        fileGroup.Status = GroupStatus.Complete;
        fileGroup.HasNir = true;

        var propertyChangedEvents = new List<string>();
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
                propertyChangedEvents.Add(args.PropertyName);
        };

        // Act
        viewModel.Refresh();

        // Assert
        Assert.Contains(nameof(FileGroupViewModel.Status), propertyChangedEvents);
        Assert.Contains(nameof(FileGroupViewModel.StatusText), propertyChangedEvents);
        Assert.Contains(nameof(FileGroupViewModel.HasNir), propertyChangedEvents);
    }

    #endregion

    #region MainWindowViewModel Tests

    private MainWindowViewModel CreateMainWindowViewModel()
    {
        var mockOrchestrator = new Mock<ChronoView.Core.FileWatching.IMonitoringOrchestrator>();
        var mockStatisticsService = new Mock<ChronoView.Core.Analytics.IStatisticsService>();
        var mockConfigManager = new Mock<ChronoView.Core.Configuration.IConfigurationManager>();
        var mockFileOperationService = new Mock<ChronoView.Core.FileOperations.IFileOperationService>();
        var mockPathManagementService = new Mock<ChronoView.Core.FileOperations.IPathManagementService>();
        var mockImageProcessor = new Mock<IImageProcessor>();
        var mockAbnormalDetector = new Mock<ChronoView.Core.Analytics.IAbnormalDetector>();
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MainWindowViewModel>>();
        var mockFileGroupLogger = new Mock<Microsoft.Extensions.Logging.ILogger<FileGroupViewModel>>();

        return new MainWindowViewModel(
            mockOrchestrator.Object,
            mockStatisticsService.Object,
            mockConfigManager.Object,
            mockFileOperationService.Object,
            mockPathManagementService.Object,
            mockImageProcessor.Object,
            mockAbnormalDetector.Object,
            mockLogger.Object,
            mockFileGroupLogger.Object);
    }

    [Fact]
    public void MainWindowViewModel_Constructor_InitializesCollections()
    {
        // Arrange & Act
        var viewModel = CreateMainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.FileGroups);
        Assert.NotNull(viewModel.Line1Groups);
        Assert.NotNull(viewModel.Line2Groups);
        Assert.NotNull(viewModel.LogMessages);
        Assert.Empty(viewModel.FileGroups);
        Assert.Empty(viewModel.Line1Groups);
        Assert.Empty(viewModel.Line2Groups);
    }

    [Fact]
    public void MainWindowViewModel_IsMonitoring_UpdatesCommandStates()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        // Act & Assert - Initially not monitoring
        Assert.False(viewModel.IsMonitoring);
        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));

        // Start monitoring
        viewModel.IsMonitoring = true;
        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
    }

    [Fact]
    public void MainWindowViewModel_StartCommand_SetsIsMonitoringTrue()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        // Act
        viewModel.StartCommand.Execute(null);

        // Assert
        Assert.True(viewModel.IsMonitoring);
        Assert.Equal("Monitoring...", viewModel.StatusMessage);
    }

    [Fact]
    public void MainWindowViewModel_StopCommand_SetsIsMonitoringFalse()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        viewModel.IsMonitoring = true;

        // Act
        viewModel.StopCommand.Execute(null);

        // Assert
        Assert.False(viewModel.IsMonitoring);
        Assert.Equal("Ready", viewModel.StatusMessage);
    }

    [Fact]
    public void MainWindowViewModel_SelectedGroup_EnablesMoveAndDeleteCommands()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        var fileGroup = new FileGroup { GroupId = "G001" };
        var groupViewModel = new FileGroupViewModel(fileGroup, CreateMockImageProcessor());

        // Act & Assert - No selection
        Assert.Null(viewModel.SelectedGroup);
        Assert.False(viewModel.MoveCommand.CanExecute(null));
        Assert.False(viewModel.DeleteCommand.CanExecute(null));

        // Select a group (use SelectedLine1Group instead of SelectedGroup)
        viewModel.SelectedLine1Group = groupViewModel;
        Assert.True(viewModel.MoveCommand.CanExecute(null));
        Assert.True(viewModel.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public void MainWindowViewModel_AddFileGroup_AddsToCorrectCollections()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        var line1Group = new FileGroup { GroupId = "G001", LineNumber = 1 };
        var line2Group = new FileGroup { GroupId = "G002", LineNumber = 2 };

        // Act
        viewModel.AddFileGroup(line1Group);
        viewModel.AddFileGroup(line2Group);

        // Assert
        Assert.Equal(2, viewModel.FileGroups.Count);
        Assert.Single(viewModel.Line1Groups);
        Assert.Single(viewModel.Line2Groups);
        Assert.Equal("G001", viewModel.Line1Groups[0].GroupId);
        Assert.Equal("G002", viewModel.Line2Groups[0].GroupId);
    }

    [Fact]
    public void MainWindowViewModel_RemoveFileGroup_RemovesFromAllCollections()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        var fileGroup = new FileGroup { GroupId = "G001", LineNumber = 1 };
        viewModel.AddFileGroup(fileGroup);

        // Act
        viewModel.RemoveFileGroup("G001");

        // Assert
        Assert.Empty(viewModel.FileGroups);
        Assert.Empty(viewModel.Line1Groups);
    }

    [Fact]
    public void MainWindowViewModel_ClearFileGroups_RemovesAllGroups()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        viewModel.AddFileGroup(new FileGroup { GroupId = "G001", LineNumber = 1 });
        viewModel.AddFileGroup(new FileGroup { GroupId = "G002", LineNumber = 2 });

        // Act
        viewModel.ClearFileGroups();

        // Assert
        Assert.Empty(viewModel.FileGroups);
        Assert.Empty(viewModel.Line1Groups);
        Assert.Empty(viewModel.Line2Groups);
    }

    [Fact]
    public void MainWindowViewModel_AddLogMessage_AddsToCollection()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        var initialCount = viewModel.LogMessages.Count;

        // Act
        viewModel.AddLogMessage(LogSeverity.Info, "Test", "Test message");

        // Assert
        Assert.Equal(initialCount + 1, viewModel.LogMessages.Count);
        var lastMessage = viewModel.LogMessages.Last();
        Assert.Equal(LogSeverity.Info, lastMessage.Severity);
        Assert.Equal("Test", lastMessage.Source);
        Assert.Equal("Test message", lastMessage.Message);
    }

    [Fact]
    public void MainWindowViewModel_AddLogMessage_LimitsCollectionSize()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        // Act - Add more than 1000 messages
        for (int i = 0; i < 1100; i++)
        {
            viewModel.AddLogMessage(LogSeverity.Info, "Test", $"Message {i}");
        }

        // Assert - Should keep only last 1000
        Assert.Equal(1000, viewModel.LogMessages.Count);
    }

    [Fact]
    public void MainWindowViewModel_UpdateFileCountStatistics_UpdatesAllProperties()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        // Act
        viewModel.UpdateFileCountStatistics(10, 20, 30, 40, 50, 60, 70, 80, 90, 100);

        // Assert
        Assert.Equal(10, viewModel.NirCount);
        Assert.Equal(20, viewModel.Nir2Count);
        Assert.Equal(30, viewModel.NormalCount);
        Assert.Equal(40, viewModel.Normal2Count);
        Assert.Equal(50, viewModel.Cam1Count);
        Assert.Equal(60, viewModel.Cam2Count);
        Assert.Equal(70, viewModel.Cam3Count);
        Assert.Equal(80, viewModel.Cam4Count);
        Assert.Equal(90, viewModel.Cam5Count);
        Assert.Equal(100, viewModel.Cam6Count);
    }

    [Fact]
    public void MainWindowViewModel_UpdateUnifiedMatchingStatistics_UpdatesProperties()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        // Act
        viewModel.UpdateUnifiedMatchingStatistics(100, 75, 20, 5);

        // Assert
        Assert.Equal(100, viewModel.UnifiedTotalGroups);
        Assert.Equal(75, viewModel.UnifiedWithNir);
        Assert.Equal(20, viewModel.UnifiedWithoutNir);
        Assert.Equal(5, viewModel.UnifiedFailed);
    }

    [Fact]
    public void MainWindowViewModel_UpdateSeparatedMatchingStatistics_UpdatesProperties()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        // Act
        viewModel.UpdateSeparatedMatchingStatistics(50, 40, 8, 2, 60, 45, 12, 3);

        // Assert
        Assert.Equal(50, viewModel.Line1TotalGroups);
        Assert.Equal(40, viewModel.Line1WithNir);
        Assert.Equal(8, viewModel.Line1WithoutNir);
        Assert.Equal(2, viewModel.Line1Failed);
        Assert.Equal(60, viewModel.Line2TotalGroups);
        Assert.Equal(45, viewModel.Line2WithNir);
        Assert.Equal(12, viewModel.Line2WithoutNir);
        Assert.Equal(3, viewModel.Line2Failed);
    }

    [Fact]
    public void MainWindowViewModel_AddFileGroup_UpdatesStatistics()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();
        var groupWithNir = new FileGroup
        {
            GroupId = "G001",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete
        };
        var groupWithoutNir = new FileGroup
        {
            GroupId = "G002",
            LineNumber = 1,
            HasNir = false,
            Status = GroupStatus.Complete
        };

        // Act
        viewModel.AddFileGroup(groupWithNir);
        viewModel.AddFileGroup(groupWithoutNir);

        // Assert
        Assert.Equal(2, viewModel.TotalGroups);
        Assert.Equal(50.0, viewModel.MatchRate); // 1 out of 2 has NIR = 50%
        Assert.Equal(0, viewModel.Failures);
    }

    #endregion

    #region MainWindowViewModel - Duplicate Group Detection Tests

    /// <summary>
    /// Property 2: Event Handler Idempotence
    /// Validates: Requirements 2.4, 4.1, 4.2
    /// Test that calling AddFileGroup (which OnGroupCreated uses internally) twice with same GroupId only adds once
    /// </summary>
    [Fact]
    public void MainWindowViewModel_AddFileGroup_SkipsDuplicateGroups()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        var fileGroup = new FileGroup
        {
            GroupId = "group_028",
            NirKey = "run_120251204T111140",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete
        };

        // Act - Add the same group twice
        viewModel.AddFileGroup(fileGroup);
        viewModel.AddFileGroup(fileGroup);

        // Assert - Group should only be added once
        Assert.Single(viewModel.FileGroups);
        Assert.Single(viewModel.Line1Groups);
        Assert.Equal("group_028", viewModel.FileGroups[0].GroupId);
    }

    /// <summary>
    /// Property 2: Event Handler Idempotence - Multiple different groups
    /// Validates: Requirements 2.4, 4.1, 4.2
    /// Test that multiple different groups can be added, but duplicates are skipped
    /// </summary>
    [Fact]
    public void MainWindowViewModel_AddFileGroup_AllowsMultipleDifferentGroups()
    {
        // Arrange
        var viewModel = CreateMainWindowViewModel();

        var group1 = new FileGroup
        {
            GroupId = "group_028",
            NirKey = "run_120251204T111140",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete
        };

        var group2 = new FileGroup
        {
            GroupId = "group_029",
            NirKey = "run_120251204T111140A",
            LineNumber = 1,
            HasNir = true,
            Status = GroupStatus.Complete
        };

        // Act - Add two different groups, then try to add group1 again
        viewModel.AddFileGroup(group1);
        viewModel.AddFileGroup(group2);
        viewModel.AddFileGroup(group1); // Try to add duplicate

        // Assert - Should have exactly 2 groups (group1 and group2)
        Assert.Equal(2, viewModel.FileGroups.Count);
        Assert.Equal(2, viewModel.Line1Groups.Count);
        Assert.Contains(viewModel.FileGroups, g => g.GroupId == "group_028");
        Assert.Contains(viewModel.FileGroups, g => g.GroupId == "group_029");
    }

    #endregion

    #region SettingsDialogViewModel Tests

    [Fact]
    public void SettingsDialogViewModel_Constructor_LoadsFromConfiguration()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            FolderPaths = new Dictionary<string, string>
            {
                { "nir", "/path/to/nir" },
                { "normal", "/path/to/normal" }
            },
            MatchingSettings = new MatchingSettings
            {
                Camera1Path = "/path/to/cam1",
                NirTimeWindowSeconds = 300,
                LineMode = "separated"
            },
            ImageSettings = new ImageSettings
            {
                ThumbnailWidth = 200,
                ThumbnailHeight = 150,
                EnableCaching = true
            }
        };

        // Arrange mocks
        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(config);
        var mockLogger = new Mock<ILogger<SettingsDialogViewModel>>();

        // Act
        var viewModel = new SettingsDialogViewModel(mockConfigManager.Object, mockLogger.Object);

        // Assert
        Assert.Equal("/path/to/nir", viewModel.NirPath);
        Assert.Equal("/path/to/normal", viewModel.NormalPath);
        Assert.Equal("/path/to/cam1", viewModel.Camera1Path);
        Assert.Equal(300, viewModel.NirTimeWindowSeconds);
        Assert.True(viewModel.IsSeparatedMode);
        Assert.Equal(200, viewModel.ThumbnailWidth);
        Assert.Equal(150, viewModel.ThumbnailHeight);
        Assert.True(viewModel.UseDiskCache);
    }

    [Fact]
    public void SettingsDialogViewModel_SaveCommand_RaisesCloseRequestedWithTrue()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(config);
        var mockLogger = new Mock<ILogger<SettingsDialogViewModel>>();
        var viewModel = new SettingsDialogViewModel(mockConfigManager.Object, mockLogger.Object);
        var closeRequestedRaised = false;
        var dialogResult = false;
        viewModel.CloseRequested += (sender, result) =>
        {
            closeRequestedRaised = true;
            dialogResult = result;
        };

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.True(closeRequestedRaised);
        Assert.True(dialogResult);
    }

    [Fact]
    public void SettingsDialogViewModel_CancelCommand_RaisesCloseRequestedWithFalse()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(config);
        var mockLogger = new Mock<ILogger<SettingsDialogViewModel>>();
        var viewModel = new SettingsDialogViewModel(mockConfigManager.Object, mockLogger.Object);
        var closeRequestedRaised = false;
        var dialogResult = true;
        viewModel.CloseRequested += (sender, result) =>
        {
            closeRequestedRaised = true;
            dialogResult = result;
        };

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.True(closeRequestedRaised);
        Assert.False(dialogResult);
    }

    [Fact]
    public void SettingsDialogViewModel_PropertyChanges_RaisePropertyChanged()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(config);
        var mockLogger = new Mock<ILogger<SettingsDialogViewModel>>();
        var viewModel = new SettingsDialogViewModel(mockConfigManager.Object, mockLogger.Object);
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(SettingsDialogViewModel.NirPath))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.NirPath = "/new/path";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("/new/path", viewModel.NirPath);
    }

    [Fact]
    public void SettingsDialogViewModel_GetConfiguration_ReturnsUpdatedConfiguration()
    {
        // Arrange
        var config = new ApplicationConfiguration();
        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(m => m.LoadConfigurationAsync<ApplicationConfiguration>())
            .ReturnsAsync(config);
        var mockLogger = new Mock<ILogger<SettingsDialogViewModel>>();
        var viewModel = new SettingsDialogViewModel(mockConfigManager.Object, mockLogger.Object);
        viewModel.NirPath = "/updated/nir";
        viewModel.Camera1Path = "/updated/cam1";
        viewModel.IsSeparatedMode = true;

        // Act
        viewModel.SaveCommand.Execute(null);
        var updatedConfig = viewModel.GetConfiguration();

        // Assert
        Assert.Equal("/updated/nir", updatedConfig.FolderPaths["nir"]);
        Assert.Equal("/updated/cam1", updatedConfig.MatchingSettings.Camera1Path);
        Assert.Equal("separated", updatedConfig.MatchingSettings.LineMode);
    }

    #endregion
}
