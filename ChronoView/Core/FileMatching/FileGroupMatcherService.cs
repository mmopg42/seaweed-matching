using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<FileGroupMatcherService>? _logger;

        public MatchingConfiguration Configuration { get; set; }

        public FileGroupMatcherService(ILogger<FileGroupMatcherService>? logger = null)
        {
            _consumedNirKeys = new HashSet<string>();
            _groupCounter = 0;
            Configuration = new MatchingConfiguration();
            _logger = logger;
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
            _logger?.LogDebug("Built {Count} groups for Line1", groupsLine1.Count);
            groups.AddRange(groupsLine1);

            // Process Line 2
            var groupsLine2 = BuildLineGroups(unmatchedFiles, lineNumber: 2);
            foreach (var g in groupsLine2)
            {
                g.LineNumber = 2;
            }
            _logger?.LogDebug("Built {Count} groups for Line2", groupsLine2.Count);
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
                normalKey = "normal1";
                nirKey = "nir1";
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
                .OrderBy(x => x.Timestamp.GetValueOrDefault())
                .ToList();

            // Get and sort NIR files
            var nirFiles = unmatchedFiles.NirFiles.ContainsKey(nirKey)
                ? unmatchedFiles.NirFiles[nirKey]
                : new Dictionary<string, string>();

            var availableNirs = new List<(string Key, string Path, DateTime? Timestamp)>();

            foreach (var kvp in nirFiles)
            {
                if (_consumedNirKeys.Contains(kvp.Key)) continue;

                var timestamp = ExtractTimestampFromNirKey(kvp.Key);
                
                // Fallback to LastWriteTime if name extraction fails (matching Python behavior)
                if (!timestamp.HasValue && File.Exists(kvp.Value))
                {
                    try
                    {
                        timestamp = File.GetLastWriteTime(kvp.Value);
                    }
                    catch { /* Ignore error */ }
                }

                if (timestamp.HasValue)
                {
                    availableNirs.Add((kvp.Key, kvp.Value, timestamp));
                }
            }
            
            // Sort by timestamp
            availableNirs.Sort((a, b) => a.Timestamp!.Value.CompareTo(b.Timestamp!.Value));

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
                    CreatedAt = normal.Timestamp.GetValueOrDefault(),
                    Status = GroupStatus.Complete,
                    HasNir = false,
                    LineNumber = lineNumber,
                    CameraFiles = new Dictionary<string, string>()
                };
                _logger?.LogDebug("Created normal-based group (Line {Line}) normal={Normal} ts={Ts}", lineNumber, normal.Key, normal.Timestamp);

                // Attach camera files
                DateTime? cam1Timestamp = null;

                // Cam1 (or Cam4) - match based on normal timestamp
                var pickedCam1 = FindMatchingCamFile(normal.Timestamp!.Value, camQueues[0]);
                if (pickedCam1 != null)
                {
                    group.CameraFiles[camKeys[0]] = pickedCam1.Path;
                    cam1Timestamp = pickedCam1.Timestamp;
                    _logger?.LogDebug("Matched {Cam} to normal {Normal} diff={Diff}s file={File}", camKeys[0], normal.Key, (cam1Timestamp.Value - normal.Timestamp!.Value).TotalSeconds, pickedCam1.Path);
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
                            _logger?.LogDebug("Matched {Cam} to cam1 ref {RefTs} diff={Diff}s file={File}", camKeys[i], cam1Timestamp.Value, (pickedCam.Timestamp - cam1Timestamp.Value).TotalSeconds, pickedCam.Path);
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
                            _logger?.LogDebug("Matched {Cam} to normal {Normal} diff={Diff}s file={File}", camKeys[i], normal.Key, (pickedCam.Timestamp - normal.Timestamp!.Value).TotalSeconds, pickedCam.Path);
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

                    var timeDiff = (nir.Timestamp!.Value - groups[i].CreatedAt).TotalSeconds;
                    var absDiff = Math.Abs(timeDiff);

                    // Must be within configured time difference (allow 1s jitter)
                    if (absDiff <= Configuration.NirMatchTimeDiff)
                    {
                        if (!minDiff.HasValue || absDiff < minDiff.Value)
                        {
                            minDiff = absDiff;
                            targetIdx = i;
                        }
                    }
                }

                if (targetIdx.HasValue)
                {
                    // Attach to existing group
                    groups[targetIdx.Value].NirKey = nir.Key;
                    groups[targetIdx.Value].NirFilePath = nir.Path; // Newly added property
                    groups[targetIdx.Value].HasNir = true;
                    _logger?.LogDebug("Matched NIR {NirKey} to group idx={Idx} diff={Diff}s path={Path}", nir.Key, targetIdx.Value, minDiff, nir.Path);
                }
                else
                {
                    // Create NIR-only group
                    var nirOnlyGroup = new FileGroup
                    {
                        GroupId = "",
                        NirKey = nir.Key,
                        NirFilePath = nir.Path, // Newly added property
                        CreatedAt = nir.Timestamp!.Value,
                        Status = GroupStatus.Complete,
                        HasNir = true,
                        LineNumber = lineNumber,
                        CameraFiles = new Dictionary<string, string>()
                    };
                    groups.Add(nirOnlyGroup);
                    _logger?.LogDebug("Created NIR-only group for {NirKey} ts={Ts} path={Path}", nir.Key, nir.Timestamp, nir.Path);
                }
            }

            // Final sort by time for this line
            groups = groups.OrderBy(g => g.CreatedAt).ToList();

            return groups;
        }

        // ... FlattenCamFiles and other existing methods ...

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

            // Pattern 1: C + 6 digits (date) + T + 6 digits (time)
            // Example: C251204T111028
            var match = System.Text.RegularExpressions.Regex.Match(folderName, @"C(\d{6}T\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value, 
                    "yyMMddTHHmmss", 
                    null, 
                    System.Globalization.DateTimeStyles.None, 
                    out var dt))
                {
                    return dt;
                }
            }

            // Pattern 2: C + 8 digits (date) + _ + 6 digits (time)
            // Example: C20240115_143022
            match = System.Text.RegularExpressions.Regex.Match(folderName, @"C(\d{8}_\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value, 
                    "yyyyMMdd_HHmmss", 
                    null, 
                    System.Globalization.DateTimeStyles.None, 
                    out var dt))
                {
                    return dt;
                }
            }

            return null;
        }

        /// <summary>
        /// Extract timestamp from NIR key
        /// </summary>
        private DateTime? ExtractTimestampFromNirKey(string nirKey)
        {
            if (string.IsNullOrEmpty(nirKey))
                return null;

            // Pattern: 8 digits (date) + T + 6 digits (time)
            // Example: 20250926T103033
            var match = System.Text.RegularExpressions.Regex.Match(nirKey, @"(\d{8}T\d{6})");
            if (match.Success)
            {
                if (DateTime.TryParseExact(
                    match.Groups[1].Value, 
                    "yyyyMMddTHHmmss", 
                    null, 
                    System.Globalization.DateTimeStyles.None, 
                    out var dt))
                {
                    return dt;
                }
            }
            
            // Legacy/Unusual fallback logic if needed can be added here
            // But strict regex is safer for now based on python 'utils.py'
            
            return null;
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
