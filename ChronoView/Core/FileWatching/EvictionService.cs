using ChronoView.Helpers;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Service responsible for determining when file groups should be evicted
    /// based on time sequence constraints. Implements cascading eviction where
    /// all affected groups shift by one position (array shift behavior).
    /// </summary>
    public class EvictionService : IEvictionService
    {
        private readonly ILogger<EvictionService> _logger;

        public EvictionService(ILogger<EvictionService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public EvictionResult CheckEvictionNeeded(
            FileGroup newGroup,
            ApplicationConfiguration config,
            IEnumerable<FileGroup> activeGroups,
            int lineNumber)
        {
            if (config.DataSequenceSettings == null || newGroup.Timestamp == DateTime.MinValue)
                return EvictionResult.NoEviction;

            var orderedTypes = config.DataSequenceSettings.GetAllOrderedTypes();
            if (orderedTypes.Count == 0)
                return EvictionResult.NoEviction;

            var newGroupType = DetermineDataTypeForGroup(newGroup);
            var normalizedNewType = NormalizeForSequence(newGroupType);
            var newGroupOrder = GetPriority(normalizedNewType, config);

            // Check if new group type uses sequence matching or timestamp-only matching
            bool useSequenceConstraints = config.DataSequenceSettings.IsSequenceMatching(normalizedNewType);
            if (!useSequenceConstraints)
            {
                _logger.LogTrace("Eviction Check: Skipped for {Type} (timestamp-only mode)", newGroupType);
                return EvictionResult.NoEviction;
            }

            // Get delay settings for the new file's type
            var minDelay = config.DataSequenceSettings.GetMinDelay(normalizedNewType);
            var maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedNewType);

            // Calculate expected predecessor timestamp range
            // If minDelay=5, maxDelay=9: newGroup expects predecessor at [T-9, T-5]
            var expectedPredMin = newGroup.Timestamp.AddSeconds(-maxDelay);
            var expectedPredMax = newGroup.Timestamp.AddSeconds(-minDelay);

            _logger.LogTrace("Eviction Check: {Type} (T={Time}) expects predecessor in range [{Min}, {Max}]",
                newGroupType, newGroup.Timestamp.ToString("HHmmss"),
                expectedPredMin.ToString("HHmmss"), expectedPredMax.ToString("HHmmss"));

            // Collect ALL victims (cascading eviction)
            // Process in descending order (newest groups first)
            var victims = new List<FileGroup>();

            foreach (var candidate in FilterByLine(activeGroups, lineNumber)
                .OrderByDescending(g => ExtractNumericSuffix(g.GroupId)))
            {
                var candidateType = DetermineDataTypeForGroup(candidate);
                var normalizedCandidateType = NormalizeForSequence(candidateType);
                var candidateOrder = GetPriority(normalizedCandidateType, config);

                // Only consider eviction if candidate has lower-order type (earlier in sequence)
                if (candidateOrder >= newGroupOrder)
                    continue;

                // Skip if candidate already has this type of data
                if (HasDataType(candidate, newGroupType))
                    continue;

                var candidateTimestamp = GetTimestampForDataType(candidate, candidateType);
                if (!candidateTimestamp.HasValue)
                    continue;

                // Check if candidate's timestamp is AFTER our expected predecessor range
                // This means the candidate is "too late" to be our predecessor
                if (candidateTimestamp.Value > expectedPredMax)
                {
                    _logger.LogInformation(
                        "Eviction candidate found: {NewType} (T={NewTime}) should precede {CandidateType} (T={CandidateTime}) in {GroupId}. " +
                        "Expected predecessor before T={MaxTime}, but found T={ActualTime}",
                        newGroupType, newGroup.Timestamp.ToString("HHmmss"),
                        candidateType, candidateTimestamp.Value.ToString("HHmmss"),
                        candidate.GroupId, expectedPredMax.ToString("HHmmss"), candidateTimestamp.Value.ToString("HHmmss"));

                    victims.Add(candidate);
                }
            }

            if (victims.Count > 0)
            {
                return EvictionResult.CascadingEviction(victims);
            }

            return EvictionResult.NoEviction;
        }

        #region Helper Methods

        /// <summary>
        /// Filters groups by line number to ensure strict line separation.
        /// </summary>
        private static IEnumerable<FileGroup> FilterByLine(IEnumerable<FileGroup> groups, int lineNumber)
            => groups.Where(g => g.LineNumber == lineNumber);

        /// <summary>
        /// Determines the data type (Normal, NIR, Cam1-6) for a file group.
        /// </summary>
        private static DataType DetermineDataTypeForGroup(FileGroup group)
        {
            if (group.HasNir) return DataType.NIR;
            if (!string.IsNullOrEmpty(group.NormalFolder)) return DataType.Normal;

            if (group.CameraFiles != null)
            {
                if (group.CameraFiles.ContainsKey("cam1")) return DataType.Cam1;
                if (group.CameraFiles.ContainsKey("cam2")) return DataType.Cam2;
                if (group.CameraFiles.ContainsKey("cam3")) return DataType.Cam3;
                if (group.CameraFiles.ContainsKey("cam4")) return DataType.Cam4;
                if (group.CameraFiles.ContainsKey("cam5")) return DataType.Cam5;
                if (group.CameraFiles.ContainsKey("cam6")) return DataType.Cam6;
            }
            return DataType.Camera;
        }

        /// <summary>
        /// Normalize Line2 camera types (Cam4-6) to Line1 equivalents (Cam1-3) for settings lookup.
        /// Line2 cameras share settings with their Line1 counterparts.
        /// </summary>
        private static DataType NormalizeForSequence(DataType type)
        {
            return type switch
            {
                DataType.Cam4 => DataType.Cam1,
                DataType.Cam5 => DataType.Cam2,
                DataType.Cam6 => DataType.Cam3,
                _ => type
            };
        }

        /// <summary>
        /// Gets the priority (order) of a data type from configuration.
        /// </summary>
        private static int GetPriority(DataType type, ApplicationConfiguration config)
        {
            return config.DataSequenceSettings?.GetOrder(type) ?? 0;
        }

        /// <summary>
        /// Checks if a file group contains data of the specified type.
        /// </summary>
        private static bool HasDataType(FileGroup group, DataType type)
        {
            return type switch
            {
                DataType.NIR => group.HasNir,
                DataType.Normal => !string.IsNullOrEmpty(group.NormalFolder),
                DataType.Cam1 => group.CameraFiles?.ContainsKey("cam1") == true,
                DataType.Cam2 => group.CameraFiles?.ContainsKey("cam2") == true,
                DataType.Cam3 => group.CameraFiles?.ContainsKey("cam3") == true,
                DataType.Cam4 => group.CameraFiles?.ContainsKey("cam4") == true,
                DataType.Cam5 => group.CameraFiles?.ContainsKey("cam5") == true,
                DataType.Cam6 => group.CameraFiles?.ContainsKey("cam6") == true,
                DataType.Camera => group.CameraFiles != null && group.CameraFiles.Count > 0,
                _ => false
            };
        }

        /// <summary>
        /// Extracts the timestamp for a specific data type from a file group.
        /// </summary>
        private static DateTime? GetTimestampForDataType(FileGroup group, DataType type)
        {
            if (type == DataType.NIR && group.HasNir)
            {
                return FileNamingHelper.ExtractTimestamp(group.NirFilePath, "Nir");
            }
            if (type == DataType.Normal && !string.IsNullOrEmpty(group.NormalFolder))
            {
                return FileNamingHelper.ExtractTimestamp(group.NormalFolder, "Normal");
            }

            // Handle specific cams (Cam1-6) with null check for CameraFiles
            if (type == DataType.Cam1 || type == DataType.Cam2 || type == DataType.Cam3 ||
                type == DataType.Cam4 || type == DataType.Cam5 || type == DataType.Cam6)
            {
                string key = type.ToString().ToLower();
                if (group.CameraFiles != null && group.CameraFiles.TryGetValue(key, out var path))
                {
                    return FileNamingHelper.ExtractTimestamp(path, "Camera");
                }
            }

            return null;
        }

        /// <summary>
        /// Extract the numeric suffix from a group ID.
        /// Examples: "group_001" → 1, "line1_023" → 23, "line2_005" → 5
        /// </summary>
        private static int ExtractNumericSuffix(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return 0;

            // Find the last underscore and parse the number after it
            int lastUnderscore = groupId.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore < groupId.Length - 1)
            {
                if (int.TryParse(groupId.Substring(lastUnderscore + 1), out int num))
                {
                    return num;
                }
            }
            return 0;
        }

        #endregion
    }
}
