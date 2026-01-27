using System.Text.Json;
using ChronoView.Models;
using Xunit;

namespace ChronoView.Tests.Models
{
    /// <summary>
    /// Unit tests for ApplicationConfiguration and MatchingSettings to validate serialization behavior.
    /// Tests for bug fix: Malformed XML comment causing JSON parsing issues.
    /// </summary>
    public class ApplicationConfigurationTests
    {
        [Fact]
        public void MalformedCommentInMatchingSettings_DoesNotCauseJsonError()
        {
            // Arrange
            var matchingSettings = new MatchingSettings
            {
                UseCameraSubfolderNormal2 = true // Set a non-default value
            };

            // Act - Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(matchingSettings, options);

            // Assert - Verify JSON is valid
            Assert.NotNull(json);
            Assert.Contains("useCameraSubfolderNormal2", json);

            // Act - Deserialize back
            var deserialized = JsonSerializer.Deserialize<MatchingSettings>(json, options);

            // Assert - Verify value is preserved (comments should not affect serialization)
            Assert.NotNull(deserialized);
            Assert.True(deserialized!.UseCameraSubfolderNormal2);
        }

        [Fact]
        public void UseCameraSubfolderNormal_RoundTrip()
        {
            // Arrange
            var original = new MatchingSettings
            {
                UseCameraSubfolderNormal = true
            };

            // Act
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(original, options);
            var restored = JsonSerializer.Deserialize<MatchingSettings>(json, options);

            // Assert
            Assert.True(restored!.UseCameraSubfolderNormal);
        }

        [Fact]
        public void UseCameraSubfolderNormal2_RoundTrip()
        {
            // Arrange
            var original = new MatchingSettings
            {
                UseCameraSubfolderNormal2 = true
            };

            // Act
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(original, options);
            var restored = JsonSerializer.Deserialize<MatchingSettings>(json, options);

            // Assert
            Assert.True(restored!.UseCameraSubfolderNormal2);
        }

        [Fact]
        public void MultipleProperties_RoundTrip()
        {
            // Arrange
            var original = new MatchingSettings
            {
                UseCameraSubfolderNormal = true,
                UseCameraSubfolderNormal2 = false,
                UseFolderSuffix = true
            };

            // Act
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(original, options);
            var restored = JsonSerializer.Deserialize<MatchingSettings>(json, options);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(original.UseCameraSubfolderNormal, restored!.UseCameraSubfolderNormal);
            Assert.Equal(original.UseCameraSubfolderNormal2, restored!.UseCameraSubfolderNormal2);
            Assert.Equal(original.UseFolderSuffix, restored!.UseFolderSuffix);
        }
    }
}
