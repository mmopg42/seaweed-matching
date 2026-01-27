using System.Linq;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Models
{
    /// <summary>
    /// Unit tests for DataSequencePresets to validate preset configurations.
    /// Tests for bug fix: Duplicate DataType in CamerasFirst() preset.
    /// </summary>
    public class DataSequencePresetsTests
    {
        [Fact]
        public void CamerasFirst_NoDuplicateTypes()
        {
            // Arrange & Act
            var settings = DataSequencePresets.CamerasFirst();

            // Assert
            var validation = settings.Validate(out var errors);
            Assert.Empty(errors); // No duplicate type errors
            Assert.Equal(5, settings.Sequence.Count); // Should have 5 items (not 7 with duplicates)

            // Verify correct types exist
            var types = settings.Sequence.Select(x => x.Type).ToList();
            Assert.Contains(DataType.Cam1, types);
            Assert.Contains(DataType.Cam2, types);
            Assert.Contains(DataType.Cam3, types);
            Assert.Contains(DataType.Normal, types);
            Assert.Contains(DataType.NIR, types);

            // Verify Cam4 and Cam5 are NOT present (the duplicates were replaced)
            Assert.DoesNotContain(DataType.Cam4, types);
            Assert.DoesNotContain(DataType.Cam5, types);
        }

        [Fact]
        public void CamerasFirst_CorrectOrder()
        {
            // Arrange & Act
            var settings = DataSequencePresets.CamerasFirst();

            // Assert - Verify order is correct: Cam1 → Cam2 → Normal → NIR → Cam3
            Assert.Equal(5, settings.Sequence.Count);
            Assert.Equal(DataType.Cam1, settings.Sequence[0].Type);
            Assert.Equal(DataType.Cam2, settings.Sequence[1].Type);
            Assert.Equal(DataType.Normal, settings.Sequence[2].Type);
            Assert.Equal(DataType.NIR, settings.Sequence[3].Type);
            Assert.Equal(DataType.Cam3, settings.Sequence[4].Type);
        }

        [Fact]
        public void NormalFirst_ValidConfiguration()
        {
            // Arrange & Act
            var settings = DataSequencePresets.NormalFirst();

            // Assert
            Assert.True(settings.Validate(out var errors));
            Assert.Empty(errors);
            Assert.Equal(5, settings.Sequence.Count);

            // Verify order: Normal → NIR → Cam1 → Cam2 → Cam3
            Assert.Equal(DataType.Normal, settings.Sequence[0].Type);
            Assert.Equal(DataType.NIR, settings.Sequence[1].Type);
            Assert.Equal(DataType.Cam1, settings.Sequence[2].Type);
            Assert.Equal(DataType.Cam2, settings.Sequence[3].Type);
            Assert.Equal(DataType.Cam3, settings.Sequence[4].Type);
        }

        [Fact]
        public void NirFirst_ValidConfiguration()
        {
            // Arrange & Act
            var settings = DataSequencePresets.NirFirst();

            // Assert
            Assert.True(settings.Validate(out var errors));
            Assert.Empty(errors);
            Assert.Equal(5, settings.Sequence.Count);

            // Verify order: NIR → Normal → Cam1 → Cam2 → Cam3
            Assert.Equal(DataType.NIR, settings.Sequence[0].Type);
            Assert.Equal(DataType.Normal, settings.Sequence[1].Type);
            Assert.Equal(DataType.Cam1, settings.Sequence[2].Type);
            Assert.Equal(DataType.Cam2, settings.Sequence[3].Type);
            Assert.Equal(DataType.Cam3, settings.Sequence[4].Type);
        }

        [Fact]
        public void CamerasFirst_DelaysPreserved()
        {
            // Arrange & Act
            var settings = DataSequencePresets.CamerasFirst();

            // Assert - Verify delay values are preserved correctly
            Assert.Equal(0, settings.Sequence[0].MinDelaySeconds); // Cam1
            Assert.Equal(0, settings.Sequence[0].MaxDelaySeconds);
            Assert.Equal(1, settings.Sequence[1].MinDelaySeconds); // Cam2
            Assert.Equal(2, settings.Sequence[1].MaxDelaySeconds);
            Assert.Equal(4, settings.Sequence[2].MinDelaySeconds); // Normal
            Assert.Equal(9, settings.Sequence[2].MaxDelaySeconds);
            Assert.Equal(0, settings.Sequence[3].MinDelaySeconds); // NIR
            Assert.Equal(1, settings.Sequence[3].MaxDelaySeconds);
            Assert.Equal(0, settings.Sequence[4].MinDelaySeconds); // Cam3
            Assert.Equal(1, settings.Sequence[4].MaxDelaySeconds);
        }
    }
}
