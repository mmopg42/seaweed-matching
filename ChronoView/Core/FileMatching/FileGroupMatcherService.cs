using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using ChronoView.Models;

namespace ChronoView.Core.FileMatching
{
    /// <summary>
    /// Service for matching files into groups based on timestamp correlation
    /// Implements the core grouping logic from Python's group_manager.py
    /// </summary>
    public class FileGroupMatcherService : IFileGroupMatcher
    {
        private readonly HashSet<string> _consumedNirKeys;
        private int _groupCounter;

        public MatchingConfiguration Configuration { get; set; }

        public FileGroupMatcherService()
        {
            _consumedNirKeys = new HashSet<string>();
            _groupCounter = 0;
            Configuration = new MatchingConfiguration();
        }

        public void ResetConsumedNirKeys()
        {
            _consumedNirKeys.Clear();
        }

        public void AddConsumedNirKey(string nirKey)
        {
            if (!string.IsNullOrEmpty(nirKey))
            {
                _consumedNirKeys.Add(nirKey);
            }
        }

        public async Task<IEnumerable<FileGroup>> MatchFilesAsync(UnmatchedFiles unmatchedFiles)
        {
            return await Task.Run(() => BuildAllGroups(unmatchedFiles));
        }

        public async Task<FileGroup> UpdateGroupAsync(FileGroup group, string filePath, FileType fileType)
        {
            return await Task.Run(() =>
            {
                // Implementation for real-time group updates
                // This would update the group with the new file based on file type
                return group;
            });
        }

        /// <summary>
        /// Build all groups from unmatched files
        /// Processes both Line 1 and Line 2 separately
        /// </summary>
        private List<FileGroup> BuildAllGroups(UnmatchedFiles unmatchedFiles)
        {
            var groups = new List<FileGroup>();

            // Process Line 1
            var groupsLine1 = BuildLineGroups(unmatchedFiles, lineNumber: 1);
            foreach (var g in groupsLine1)
            {
                g.LineNumber = 1;
            }
            groups.AddRange(groupsLine1);

            // Process Line 2
            var groupsLine2 = BuildLineGroups(unmatchedFiles, lineNumber: 2);
            foreach (var g in groupsLine2)
            {
                g.LineNumber = 2;
            }
            groups.AddRange(groupsLine2);

            // Final sort by time and assign names
            groups = groups.OrderBy(g => g.CreatedAt).ToList();
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].GroupId = $"group_{(i + 1):D3}";
            }

            _groupCounter = groups.Count;
            return groups;
        }

        /// <summary>
        /// Build groups for a specific line
        /// </summary>
        private List<FileGroup> BuildLineGroups(UnmatchedFiles unmatchedFiles, int lineNumber)
        {
            var groups = new List<FileGroup>();

            // Determine keys based on line number
            string normalKey, nirKey;
            string[] camKeys;

            if (lineNumber == 1)
            {
                normalKey = "normal";
                nirKey = "nir";
                camKeys = new[] { "cam1", "cam2", "cam3" };
            }
            else // lineNumber == 2
            {
                normalKey = "normal2";
                nirKey = "nir2";
                camKeys = new[] { "cam4", "cam5", "cam6" };
            }

            // Get and sort normal files
            var normalFiles = unmatchedFiles.NormalFolders.ContainsKey(normalKey)
                ? unmatchedFiles.NormalFolders[normalKey]
                : new Dictionary<string, string>();

            var availableNormals = normalFiles
                .Select(kvp => new
                {
                    Key = kvp.Key,
                    Path = kvp.Value,
                    Timestamp = ExtractTimestampFromFolderName(kvp.Key)
                })
                .Where(x => x.Timestamp.HasValue)
                .OrderBy(x => x.Timestamp.Value)
                .ToList();

            // Get and sort NIR files
            var nirFiles = unmatchedFiles.NirFiles.ContainsKey(nirKey)
                ? unmatchedFiles.NirFiles[nirKey]
                : new Dictionary<string, string>();

            var availableNirs = nirFiles
                .Where(kvp => !_consumedNirKeys.Contains(kvp.Key))
                .Select(kvp => new
                {
                    Key = kvp.Key,
                    Path = kvp.Value,
                    Timestamp = ExtractTimestampFromNirKey(kvp.Key)
                })
                .Where(x => x.Timestamp.HasValue)
                .OrderBy(x => x.Timestamp.Value)
                .ToList();

            // Flatten camera files into queues
            var camQueues = camKeys.Select(ck =>
                FlattenCamFiles(unmatchedFiles.CameraFiles.ContainsKey(ck)
                    ? unmatchedFiles.CameraFiles[ck]
                    : new List<TimestampedFile>())
            ).ToArray();

            // Create normal-based groups with camera attachments
            foreach (var normal in availableNormals)
            {
                var group = new FileGroup
                {
                    GroupId = "", // Will be assigned later
                    NormalFolder = normal.Key,
                    MainImagePath = Path.Combine(normal.Path, "stitched_original.png"),
                    CreatedAt = normal.Timestamp.Value,
                    Status = GroupStatus.Complete,
                    HasNir = false,
                    LineNumber = lineNumber,
                    CameraFiles = new Dictionary<string, string>()
                };

                // Attach camera files
                DateTime? cam1Timestamp = null;

                // Cam1 (or Cam4) - match based on normal timestamp
                var pickedCam1 = FindMatchingCamFile(normal.Timestamp!.Value, camQueues[0]);
                if (pickedCam1 != null)
                {
                    group.CameraFiles[camKeys[0]] = pickedCam1.Path;
                    cam1Timestamp = pickedCam1.Timestamp;
                }

                // Cam2/3 (or Cam5/6) - match based on cam1 timestamp if available
                if (cam1Timestamp.HasValue)
                {
                    for (int i = 1; i < 3; i++)
                    {
                        var pickedCam = FindMatchingCamFileFromReference(
                            cam1Timestamp.Value, camQueues[i], maxDiff: 1.0);
                        if (pickedCam != null)
                        {
                            group.CameraFiles[camKeys[i]] = pickedCam.Path;
                        }
                    }
                }
                else
                {
                    // Fallback to normal-based matching
                    for (int i = 1; i < 3; i++)
                    {
                        var pickedCam = FindMatchingCamFile(normal.Timestamp!.Value, camQueues[i]);
                        if (pickedCam != null)
                        {
                            group.CameraFiles[camKeys[i]] = pickedCam.Path;
                        }
                    }
                }

                groups.Add(group);
            }

            // Drain remaining camera files into cam-only groups
            for (int i = 0; i < camKeys.Length; i++)
            {
                DrainCamToGroups(groups, camKeys[i], camQueues[i], lineNumber);
            }

            // Attach NIR files to closest groups
            foreach (var nir in availableNirs)
            {
                int? targetIdx = null;
                double? minDiff = null;

                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].HasNir) continue; // Already has NIR

                    var timeDiff = (groups[i].CreatedAt - nir.Timestamp!.Value).TotalSeconds;

                    // Must be same or later time, within max diff
                    if (timeDiff >= 0 && timeDiff <= Configuration.NirMatchTimeDiff)
                    {
                        if (!minDiff.HasValue || timeDiff < minDiff.Value)
                        {
                            minDiff = timeDiff;
                            targetIdx = i;
                        }
                    }
                }

                if (targetIdx.HasValue)
                {
                    // Attach to existing group
                    groups[targetIdx.Value].NirKey = nir.Key;
                    groups[targetIdx.Value].HasNir = true;
                }
                else
                {
                    // Create NIR-only group
                    var nirOnlyGroup = new FileGroup
                    {
                        GroupId = "",
                        NirKey = nir.Key,
                        CreatedAt = nir.Timestamp!.Value,
                        Status = GroupStatus.Complete,
                        HasNir = true,
                        LineNumber = lineNumber,
                        CameraFiles = new Dictionary<string, string>()
                    };
                    groups.Add(nirOnlyGroup);
                }
            }

            // Final sort by time for this line
            groups = groups.OrderBy(g => g.CreatedAt).ToList();

            return groups;
        }

        /// <summary>
        /// Flatten camera files into a sorted queue
        /// </summary>
        private List<CamFileEntry> FlattenCamFiles(List<TimestampedFile> camFiles)
        {
            var result = new List<CamFileEntry>();

            foreach (var file in camFiles)
            {
                var isCopy = file.FileName.Contains("복사본") || 
                            file.FileName.ToLower().Contains("copy");

                result.Add(new CamFileEntry
                {
                    FileName = file.FileName,
                    Path = file.AbsolutePath,
                    Timestamp = file.Timestamp,
                    IsCopy = isCopy
                });
            }

            // Sort: timestamp ascending, non-copies first, then by filename
            return result.OrderBy(x => x.Timestamp)
                        .ThenBy(x => x.IsCopy)
                        .ThenBy(x => x.FileName)
                        .ToList();
        }

        /// <summary>
        /// Find matching camera file based on normal timestamp
        /// </summary>
        private CamFileEntry? FindMatchingCamFile(DateTime normalTimestamp, List<CamFileEntry> queue)
        {
            if (queue == null || queue.Count == 0) return null;

            if (!Configuration.UseCamTimeMatching)
            {
                // Sequential matching - just pop first
                var item = queue[0];
                queue.RemoveAt(0);
                return item;
            }

            // Time-based matching
            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];
                var diff = (item.Timestamp - normalTimestamp).TotalSeconds;

                // Check if within valid range
                if (diff >= Configuration.CamMatchMinDiff && diff <= Configuration.CamMatchMaxDiff)
                {
                    queue.RemoveAt(i);
                    return item;
                }
            }

            return null; // No match found
        }

        /// <summary>
        /// Find matching camera file based on reference camera timestamp (cam1 or cam4)
        /// </summary>
        private CamFileEntry? FindMatchingCamFileFromReference(
            DateTime referenceTimestamp, List<CamFileEntry> queue, double maxDiff = 1.0)
        {
            if (queue == null || queue.Count == 0) return null;

            if (!Configuration.UseCamTimeMatching)
            {
                // Sequential matching
                var item = queue[0];
                queue.RemoveAt(0);
                return item;
            }

            // Time-based matching - must be same or later, within maxDiff
            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];
                var diff = (item.Timestamp - referenceTimestamp).TotalSeconds;

                // 0 <= diff <= maxDiff (same time or up to maxDiff seconds later)
                if (diff >= 0 && diff <= maxDiff)
                {
                    queue.RemoveAt(i);
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Drain remaining camera files into cam-only groups
        /// </summary>
        private void DrainCamToGroups(List<FileGroup> groups, string camKey, 
            List<CamFileEntry> queue, int lineNumber)
        {
            foreach (var item in queue)
            {
                var group = new FileGroup
                {
                    GroupId = "",
                    CreatedAt = item.Timestamp,
                    Status = GroupStatus.Complete,
                    HasNir = false,
                    LineNumber = lineNumber,
                    CameraFiles = new Dictionary<string, string>
                    {
                        { camKey, item.Path }
                    }
                };
                groups.Add(group);
            }

            queue.Clear();
        }

        /// <summary>
        /// Extract timestamp from folder name (e.g., "C251204T111028_0" or "C20240115_143022")
        /// </summary>
        private DateTime? ExtractTimestampFromFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName) || !folderName.StartsWith("C"))
                return null;

            try
            {
                // Format 1: C251204T111028_0 (New Format)
                // C + YYMMDD + T + HHMMSS + _ + Index
                if (folderName.Contains('T') && folderName.Contains('_'))
                {
                    // Remove 'C' prefix
                    var namePart = folderName.Substring(1);
                    var parts = namePart.Split('T'); // ["251204", "111028_0"]
                    
                    if (parts.Length >= 2)
                    {
                        var dateStr = parts[0];
                        var timeStr = parts[1].Split('_')[0]; // "111028"

                        if (dateStr.Length == 6 && timeStr.Length == 6)
                        {
                            int year = int.Parse("20" + dateStr.Substring(0, 2));
                            int month = int.Parse(dateStr.Substring(2, 2));
                            int day = int.Parse(dateStr.Substring(4, 2));
                            int hour = int.Parse(timeStr.Substring(0, 2));
                            int minute = int.Parse(timeStr.Substring(2, 2));
                            int second = int.Parse(timeStr.Substring(4, 2));

                            return new DateTime(year, month, day, hour, minute, second);
                        }
                    }
                }

                // Format 2: C20240115_143022 (Legacy Format)
                var dateTimePart = folderName.Substring(1);
                var legacyParts = dateTimePart.Split('_');
                if (legacyParts.Length == 2)
                {
                    var datePart = legacyParts[0]; // 20240115
                    var timePart = legacyParts[1]; // 143022

                    if (datePart.Length == 8 && timePart.Length == 6)
                    {
                        int year = int.Parse(datePart.Substring(0, 4));
                        int month = int.Parse(datePart.Substring(4, 2));
                        int day = int.Parse(datePart.Substring(6, 2));
                        int hour = int.Parse(timePart.Substring(0, 2));
                        int minute = int.Parse(timePart.Substring(2, 2));
                        int second = int.Parse(timePart.Substring(4, 2));

                        return new DateTime(year, month, day, hour, minute, second);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract timestamp from NIR key
        /// </summary>
        private DateTime? ExtractTimestampFromNirKey(string nirKey)
        {
            if (string.IsNullOrEmpty(nirKey))
                return null;

            try
            {
                // NIR keys typically have format like "20240115_143022"
                var parts = nirKey.Split('_');
                if (parts.Length < 2) return null;

                var datePart = parts[0];
                var timePart = parts[1];

                if (datePart.Length != 8 || timePart.Length < 6)
                    return null;

                int year = int.Parse(datePart.Substring(0, 4));
                int month = int.Parse(datePart.Substring(4, 2));
                int day = int.Parse(datePart.Substring(6, 2));
                int hour = int.Parse(timePart.Substring(0, 2));
                int minute = int.Parse(timePart.Substring(2, 2));
                int second = int.Parse(timePart.Substring(4, 2));

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Internal class for camera file queue entries
        /// </summary>
        private class CamFileEntry
        {
            public string FileName { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public bool IsCopy { get; set; }
        }
    }
}
