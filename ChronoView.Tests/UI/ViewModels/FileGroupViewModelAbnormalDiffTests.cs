using System;
using System.Reflection;
using Xunit;
using Moq;
using ChronoView.UI.ViewModels;
using ChronoView.Models;
using ChronoView.Core.ImageProcessing;
using ChronoView.Core.Analytics;
using Microsoft.Extensions.Logging;

namespace ChronoView.Tests.UI.ViewModels
{
    public class FileGroupViewModelAbnormalDiffTests
    {
        private readonly Mock<IImageProcessor> _mockImageProcessor;
        private readonly Mock<IAbnormalDetector> _mockAbnormalDetector;
        private readonly Mock<ILogger<FileGroupViewModel>> _mockLogger;

        public FileGroupViewModelAbnormalDiffTests()
        {
            _mockImageProcessor = new Mock<IImageProcessor>();
            _mockAbnormalDetector = new Mock<IAbnormalDetector>();
            _mockLogger = new Mock<ILogger<FileGroupViewModel>>();
        }

        private FileGroup CreateTestGroup()
        {
            return new FileGroup
            {
                GroupId = "TestGroup",
                NormalFolder = "C:\\Test\\Normal",
                MainImagePath = "C:\\Test\\Normal\\main.jpg",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public void Constructor_ShouldNotRunRatioCheck_Initially()
        {
            var group = CreateTestGroup();
            // Ensure IsGroupAbnormal does not return true for NIR check so that IsAbnormal starts false
            _mockAbnormalDetector.Setup(x => x.IsGroupAbnormal(It.IsAny<FileGroup>())).Returns(false);

            using var vm = new FileGroupViewModel(group, _mockImageProcessor.Object, null, _mockAbnormalDetector.Object, null, _mockLogger.Object, null);

            // Verify GetImageDimensions was NOT called (old logic removed)
            _mockImageProcessor.Verify(x => x.GetImageDimensions(It.IsAny<string>()), Times.Never);
            Assert.False(vm.IsAbnormal);
        }

        [Fact]
        public void OnMainImageLoadedInfoChanged_ShouldCalculateDiff_And_UpdateLabel()
        {
            var group = CreateTestGroup();
            _mockAbnormalDetector.Setup(x => x.IsGroupAbnormal(It.IsAny<FileGroup>())).Returns(false);
            
            // Setup Detector
            _mockAbnormalDetector.Setup(x => x.AddAndCheckImage(1920, 1080, It.IsAny<string>()))
                .Returns((false, 0.05));

            using var vm = new FileGroupViewModel(group, _mockImageProcessor.Object, null, _mockAbnormalDetector.Object, null, _mockLogger.Object, null);

            // Access private _mediaLoader via reflection
            var mediaLoaderField = typeof(FileGroupViewModel).GetField("_mediaLoader", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(mediaLoaderField);
            var mediaLoader = mediaLoaderField.GetValue(vm) as FileGroupMediaLoader;
            Assert.NotNull(mediaLoader);

            // Create Info
            var info = new MainImageLoadedInfo(
                "TestGroup",
                DateTime.UtcNow.Ticks,
                1920,
                1080,
                null, 
                group.MainImagePath,
                DateTime.UtcNow
            );

            // Trigger PropertyChange by setting MainImageLoadedInfo on loader
            mediaLoader.MainImageLoadedInfo = info;

            // Verify calculation happened
            _mockAbnormalDetector.Verify(x => x.AddAndCheckImage(1920, 1080, It.IsAny<string>()), Times.Once);
            
            // Verify Label Updated
            Assert.Contains("1920x1080", vm.NormalImageSizeLabel);
            Assert.Contains("Diff: 0.05", vm.NormalImageSizeLabel);
        }
        
        [Fact]
        public void OnMainImageLoadedInfoChanged_ShouldMarkAbnormal_IfDetectorSaysSo()
        {
            var group = CreateTestGroup();
            _mockAbnormalDetector.Setup(x => x.IsGroupAbnormal(It.IsAny<FileGroup>())).Returns(false);
            
            // Setup Detector to return abnormal
            _mockAbnormalDetector.Setup(x => x.AddAndCheckImage(1920, 1080, It.IsAny<string>()))
                .Returns((true, 0.5));

            using var vm = new FileGroupViewModel(group, _mockImageProcessor.Object, null, _mockAbnormalDetector.Object, null, _mockLogger.Object, null);

            // Access private _mediaLoader via reflection
            var mediaLoaderField = typeof(FileGroupViewModel).GetField("_mediaLoader", BindingFlags.NonPublic | BindingFlags.Instance);
            var mediaLoader = mediaLoaderField.GetValue(vm) as FileGroupMediaLoader;

            var info = new MainImageLoadedInfo(
                "TestGroup",
                DateTime.UtcNow.Ticks,
                1920,
                1080,
                null,
                group.MainImagePath,
                DateTime.UtcNow
            );

            mediaLoader.MainImageLoadedInfo = info;

            // Verify IsAbnormal
            Assert.True(vm.IsAbnormal);
            // Verify status text update
            Assert.Equal("Abnormal", vm.StatusText);
        }

        [Fact]
        public void OnMainImageLoadedInfoChanged_ShouldGuardAgainstStaleGroup()
        {
            var group = CreateTestGroup();
            _mockAbnormalDetector.Setup(x => x.IsGroupAbnormal(It.IsAny<FileGroup>())).Returns(false);

            using var vm = new FileGroupViewModel(group, _mockImageProcessor.Object, null, _mockAbnormalDetector.Object, null, _mockLogger.Object, null);

            var mediaLoaderField = typeof(FileGroupViewModel).GetField("_mediaLoader", BindingFlags.NonPublic | BindingFlags.Instance);
            var mediaLoader = mediaLoaderField.GetValue(vm) as FileGroupMediaLoader;

            // Create Info with WRONG GroupId
            var info = new MainImageLoadedInfo(
                "DifferentGroup",
                DateTime.UtcNow.Ticks,
                1920,
                1080,
                null,
                group.MainImagePath,
                DateTime.UtcNow
            );

            mediaLoader.MainImageLoadedInfo = info;

            // Verify calculation did NOT happen
            _mockAbnormalDetector.Verify(x => x.AddAndCheckImage(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
}
