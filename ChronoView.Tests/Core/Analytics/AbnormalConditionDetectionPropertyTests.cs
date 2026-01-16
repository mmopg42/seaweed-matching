using System;
using System.Collections.Generic;
using ChronoView.Core.Analytics;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Core.Analytics
{
    /// <summary>
    /// 레거시(z-score/percent) 기반 테스트를 최신 ratio-diff 기반 AbnormalDetectorService에 맞춰 업데이트.
    /// 메인 코드는 변경하지 않고 테스트만 최신 동작을 검증한다.
    /// </summary>
    public class AbnormalConditionDetectionPropertyTests
    {
        [Fact]
        public void RatioDiffDetection_ShouldBeConsistent_ForSameInputSequence()
        {
            var detector1 = new AbnormalDetectorService(windowSize: 50, minSamples: 5, threshold: 0.2);
            var detector2 = new AbnormalDetectorService(windowSize: 50, minSamples: 5, threshold: 0.2);
            const string context = "Line1_Cam1";

            var testData = new List<(int w, int h)>
            {
                // Warm-up (baseline ratio ~ 1.777...)
                (1920, 1080), (1920, 1080), (1920, 1080), (1920, 1080), (1920, 1080),
                // Now detection enabled
                (1920, 1080),
                // Outlier (ratio 1.5) -> diff ~ 0.277...
                (3000, 2000),
            };

            (bool a1, double? d1) = (false, null);
            (bool a2, double? d2) = (false, null);

            foreach (var (w, h) in testData)
            {
                (a1, d1) = detector1.AddAndCheckImage(w, h, context);
                (a2, d2) = detector2.AddAndCheckImage(w, h, context);
            }

            Assert.Equal(a1, a2);
            Assert.Equal(d1, d2);
        }

        [Fact]
        public void AbnormalDetection_ShouldTrigger_WhenRatioDiffExceedsThreshold()
        {
            var detector = new AbnormalDetectorService(windowSize: 50, minSamples: 5, threshold: 0.2);
            const string context = "Line2_Cam6";

            // Build baseline
            for (int i = 0; i < 5; i++)
            {
                var warm = detector.AddAndCheckImage(1920, 1080, context);
                Assert.False(warm.IsAbnormal);
            }

            // Outlier
            var result = detector.AddAndCheckImage(3000, 2000, context);
            Assert.True(result.IsAbnormal);
            Assert.True(result.RatioDiff.HasValue);
            Assert.True(result.RatioDiff.Value > 0.2);
        }

        [Fact]
        public void NormalVariations_ShouldNotTriggerAbnormalDetection()
        {
            var detector = new AbnormalDetectorService(windowSize: 50, minSamples: 5, threshold: 0.3);
            const string context = "Line1_Cam2";

            // Slight ratio variations around baseline
            var baselineData = new List<(int w, int h)>
            {
                (1920, 1080),
                (1919, 1080),
                (1921, 1080),
                (1920, 1079),
                (1920, 1081),
                (1920, 1080),
                (1921, 1081),
                (1919, 1079),
            };

            bool anyAbnormal = false;
            foreach (var (w, h) in baselineData)
            {
                var r = detector.AddAndCheckImage(w, h, context);
                anyAbnormal |= r.IsAbnormal;
            }

            Assert.False(anyAbnormal);
        }

        [Fact]
        public void DetectorShouldDeferJudgmentUntilMinSamples()
        {
            var detector = new AbnormalDetectorService(windowSize: 50, minSamples: 5, threshold: 0.2);
            const string context = "Global";

            for (int i = 0; i < 4; i++)
            {
                var result = detector.AddAndCheckImage(1920, 1080, context);
                Assert.False(result.IsAbnormal);
                Assert.Equal(0.0, result.RatioDiff);
            }
        }

        [Fact]
        public void ResetShouldClearAllState()
        {
            var detector = new AbnormalDetectorService(windowSize: 50, minSamples: 5, threshold: 0.2);
            const string context = "Line1_Cam3";

            for (int i = 0; i < 10; i++)
            {
                detector.AddAndCheckImage(1920, 1080, context);
            }

            detector.Reset();

            var afterReset = detector.AddAndCheckImage(1920, 1080, context);
            Assert.False(afterReset.IsAbnormal);
            Assert.Equal(0.0, afterReset.RatioDiff);
        }

        [Fact]
        public void NirOnlyGroupsShouldBeAbnormal()
        {
            var detector = new AbnormalDetectorService();
            var nirOnlyGroup = new FileGroup
            {
                GroupId = "group_001",
                NirKey = "20240115_143022",
                HasNir = true,
                NormalFolder = "",
                CameraFiles = new Dictionary<string, string>(),
                LineNumber = 1,
                CreatedAt = DateTime.Now
            };

            Assert.True(detector.IsGroupAbnormal(nirOnlyGroup));
        }

        [Fact]
        public void GroupsWithCameraDataShouldNotBeAbnormal()
        {
            var detector = new AbnormalDetectorService();
            var normalGroup = new FileGroup
            {
                GroupId = "group_001",
                NirKey = "20240115_143022",
                HasNir = true,
                NormalFolder = "C20240115_143022",
                CameraFiles = new Dictionary<string, string> { { "cam1", @"C:\test\cam1\image.jpg" } },
                LineNumber = 1,
                CreatedAt = DateTime.Now
            };

            Assert.False(detector.IsGroupAbnormal(normalGroup));
        }
    }
}
