using System;
using System.IO;
using Xunit;
using Moq;
using ChronoView.Core.Analytics;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Tests.Core.Analytics
{
    public class AbnormalDetectorServiceTests : IDisposable
    {
        private readonly string _tempHistoryPath;
        private readonly AbnormalHistoryManager _historyManager;
        private readonly Mock<IConfigurationManager> _mockConfig;
        private readonly AbnormalDetectorService _service;

        public AbnormalDetectorServiceTests()
        {
            _tempHistoryPath = Path.GetTempFileName();
            _historyManager = new AbnormalHistoryManager(null, null, _tempHistoryPath);
            _mockConfig = new Mock<IConfigurationManager>();

            var config = new ApplicationConfiguration();
            // Using default window size 10
            config.MatchingSettings.AbnormalDetectionWindowSize = 10;
            // Ratio diff threshold (e.g., |currentRatio - medianRatio| > threshold)
            config.MatchingSettings.AbnormalRatioThreshold = 0.12;

            _mockConfig.Setup(c => c.LoadConfiguration<ApplicationConfiguration>()).Returns(config);

            _service = new AbnormalDetectorService(_mockConfig.Object, _historyManager, null);
        }

        public void Dispose()
        {
            if (File.Exists(_tempHistoryPath))
            {
                try { File.Delete(_tempHistoryPath); } catch { }
            }
        }

        [Fact]
        public void AddAndCheckImage_BaselineStability_ShouldDetectDeviation_WhenLogicIsFixed()
        {
            string context = "ContextA";
            double threshold = 0.12;

            // 1. Fill history with 5 items (MinSamples)
            for (int i = 0; i < 5; i++)
            {
                _service.AddAndCheckImage(1896, 2112, context);
            }

            // 2. Add an outlier (Width 1528 is ~19% deviation from 1896)
            var resAbnormal = _service.AddAndCheckImage(1528, 2112, context);
            Assert.True(resAbnormal.IsAbnormal);
            Assert.True(resAbnormal.RatioDiff.HasValue);
            Assert.True(resAbnormal.RatioDiff.Value > threshold);

            // 3. IMPORTANT: Verify next check still compares against 1896 (Clean Baseline)
            // If it was polluted, 1616 might be considered normal. 
            var resNext = _service.AddAndCheckImage(1616, 2112, context);
            Assert.True(resNext.IsAbnormal, "Subsequent outlier should be detected because baseline was NOT polluted");
        }

        // NOTE: 과거 레거시 테스트에는 "연속 이상치 후 자동 리셋" 정책이 있었지만,
        // 현재 AbnormalDetectorService는 "clean baseline" 정책(이상치는 히스토리에 추가하지 않음)을 사용합니다.
    }
}
