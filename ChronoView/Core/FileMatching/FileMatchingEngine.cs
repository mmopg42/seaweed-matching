using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileMatching
{
    /// <summary>
    /// Matching strategy enumeration
    /// </summary>
    public enum MatchingStrategy
    {
        /// <summary>
        /// Use DataSequenceSettings for time-based matching
        /// </summary>
        DataSequenceBased,

        /// <summary>
        /// Sequential matching without time constraints (fallback)
        /// </summary>
        Sequential
    }

    /// <summary>
    /// Standalone file matching engine with pure logic
    /// Framework-agnostic, stateless, and independently testable
    /// </summary>
    public static class FileMatchingEngine
    {
        /// <summary>
        /// Match files into groups based on timestamp correlation
        /// </summary>
        /// <param name="unmatchedFiles">Files to be matched</param>
        /// <param name="dataSequenceSettings">Configuration for time-based matching (optional)</param>
        /// <param name="consumedNirKeys">Set of NIR keys already consumed (for state management)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <param name="uiLog">Optional UI log action (Severity, Source, Message)</param>
        /// <returns>List of matched file groups</returns>
        public static List<FileGroup> MatchFiles(
            UnmatchedFiles unmatchedFiles,
            DataSequenceSettings? dataSequenceSettings,
            HashSet<string> consumedNirKeys,
            ILogger? logger = null,
            Action<LogSeverity, string, string>? uiLog = null)
        {
            if (unmatchedFiles == null)
                throw new ArgumentNullException(nameof(unmatchedFiles));

            if (consumedNirKeys == null)
                throw new ArgumentNullException(nameof(consumedNirKeys));

            // Determine matching strategy
            var strategy = DetermineStrategy(dataSequenceSettings);
            logger?.LogInformation("Using matching strategy: {Strategy}", strategy);

            var groups = new List<FileGroup>();

            // Process Line 1
            var groupsLine1 = BuildLineGroups(
                unmatchedFiles, 
                lineNumber: 1, 
                dataSequenceSettings, 
                consumedNirKeys,
                logger,
                uiLog);
            
            foreach (var g in groupsLine1)
            {
                g.LineNumber = 1;
            }
            logger?.LogDebug("Built {Count} groups for Line1", groupsLine1.Count);
            groups.AddRange(groupsLine1);

            // Process Line 2
            var groupsLine2 = BuildLineGroups(
                unmatchedFiles, 
                lineNumber: 2, 
                dataSequenceSettings, 
                consumedNirKeys,
                logger,
                uiLog);
            
            foreach (var g in groupsLine2)
            {
                g.LineNumber = 2;
            }
            logger?.LogDebug("Built {Count} groups for Line2", groupsLine2.Count);
            groups.AddRange(groupsLine2);

            // Final sort by time and assign names
            groups = groups.OrderBy(g => g.CreatedAt).ToList();
            
            logger?.LogInformation("[DIAGNOSTIC] Final group count: Line1={Line1}, Line2={Line2}, Total={Total}", 
                groupsLine1.Count, groupsLine2.Count, groups.Count);
            
            // Log group composition
            var normalOnlyCount = groups.Count(g => !string.IsNullOrEmpty(g.NormalFolder) && g.CameraFiles.Count == 0);
            var normalWithCamCount = groups.Count(g => !string.IsNullOrEmpty(g.NormalFolder) && g.CameraFiles.Count > 0);
            var camOnlyCount = groups.Count(g => string.IsNullOrEmpty(g.NormalFolder) && g.CameraFiles.Count > 0);
            var nirOnlyCount = groups.Count(g => string.IsNullOrEmpty(g.NormalFolder) && g.CameraFiles.Count == 0 && g.HasNir);
            
            logger?.LogInformation("[DIAGNOSTIC] Group composition: NormalOnly={NO}, NormalWithCam={NWC}, CamOnly={CO}, NirOnly={NIO}", 
                normalOnlyCount, normalWithCamCount, camOnlyCount, nirOnlyCount);
            
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].GroupId = $"group_{(i + 1):D3}";
            }

            return groups;
        }

        /// <summary>
        /// Determine which matching strategy to use
        /// </summary>
        private static MatchingStrategy DetermineStrategy(DataSequenceSettings? dataSequenceSettings)
        {
            if (dataSequenceSettings == null || 
                dataSequenceSettings.Sequence == null || 
                dataSequenceSettings.Sequence.Count == 0)
            {
                return MatchingStrategy.Sequential;
            }

            return MatchingStrategy.DataSequenceBased;
        }

        /// <summary>
        /// Build groups for a specific line using DataSequenceSettings Order
        /// </summary>
        private static List<FileGroup> BuildLineGroups(
            UnmatchedFiles unmatchedFiles,
            int lineNumber,
            DataSequenceSettings? dataSequenceSettings,
            HashSet<string> consumedNirKeys,
            ILogger? logger,
            Action<LogSeverity, string, string>? uiLog)
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

            // If no DataSequenceSettings, fall back to legacy logic
            if (dataSequenceSettings == null || dataSequenceSettings.Sequence == null || dataSequenceSettings.Sequence.Count == 0)
            {
                logger?.LogWarning("No DataSequenceSettings configured, using legacy hardcoded matching logic");
                return BuildLineGroupsLegacy(unmatchedFiles, lineNumber, normalKey, nirKey, camKeys, consumedNirKeys, logger, uiLog, dataSequenceSettings);
            }

            // NEW: Use DataSequenceSettings Order for dynamic matching
            return BuildLineGroupsWithOrder(unmatchedFiles, lineNumber, normalKey, nirKey, camKeys, dataSequenceSettings, consumedNirKeys, logger, uiLog);
        }

        /// <summary>
        /// Build groups using DataSequenceSettings Order (NEW IMPLEMENTATION)
        /// </summary>
        private static List<FileGroup> BuildLineGroupsWithOrder(
            UnmatchedFiles unmatchedFiles,
            int lineNumber,
            string normalKey,
            string nirKey,
            string[] camKeys,
            DataSequenceSettings dataSequenceSettings,
            HashSet<string> consumedNirKeys,
            ILogger? logger,
            Action<LogSeverity, string, string>? uiLog)
        {
            var groups = new List<FileGroup>();

            // Get ordered data types from DataSequenceSettings
            var orderedTypes = dataSequenceSettings.GetOrderedItems();
            logger?.LogInformation("[BUILD-GROUPS] Line {Line}: Using DataSequenceSettings Order: {Order}",
                lineNumber, string.Join(" → ", orderedTypes.Select(dt => $"{dt.Type}(Order={dt.Order})")));

            // Collect all files by DataType
            var filesByType = new Dictionary<DataType, List<(string Key, string Path, DateTime Timestamp)>>();

            // Collect Normal files
            var normalFiles = unmatchedFiles.NormalFolders.ContainsKey(normalKey)
                ? unmatchedFiles.NormalFolders[normalKey]
                : new Dictionary<string, string>();

            var normalList = new List<(string Key, string Path, DateTime Timestamp)>();
            foreach (var kvp in normalFiles)
            {
                var timestamp = ExtractTimestampFromFolderName(kvp.Key);
                if (timestamp.HasValue)
                {
                    normalList.Add((kvp.Key, kvp.Value, timestamp.Value));
                }
            }
            normalList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            filesByType[DataType.Normal] = normalList;

            // Collect NIR files
            var nirFiles = unmatchedFiles.NirFiles.ContainsKey(nirKey)
                ? unmatchedFiles.NirFiles[nirKey]
                : new Dictionary<string, string>();

            var nirList = new List<(string Key, string Path, DateTime Timestamp)>();
            foreach (var kvp in nirFiles)
            {
                if (consumedNirKeys.Contains(kvp.Key)) continue;

                var timestamp = ExtractTimestampFromNirKey(kvp.Key);
                if (!timestamp.HasValue && File.Exists(kvp.Value))
                {
                    try { timestamp = File.GetLastWriteTime(kvp.Value); } catch { }
                }
                if (timestamp.HasValue)
                {
                    nirList.Add((kvp.Key, kvp.Value, timestamp.Value));
                }
            }
            nirList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            filesByType[DataType.NIR] = nirList;

            // Collect Camera files
            for (int i = 0; i < camKeys.Length; i++)
            {
                var camKey = camKeys[i];
                var camDataType = i == 0 ? DataType.Cam1 : (i == 1 ? DataType.Cam2 : DataType.Cam3);

                var camFiles = unmatchedFiles.CameraFiles.ContainsKey(camKey)
                    ? unmatchedFiles.CameraFiles[camKey]
                    : new List<TimestampedFile>();

                var camList = camFiles
                    .Where(f => !(f.FileName.Contains("복사본") || f.FileName.ToLower().Contains("copy")))
                    .Select(f => (f.FileName, f.AbsolutePath, f.Timestamp))
                    .OrderBy(x => x.Timestamp)
                    .ToList();

                filesByType[camDataType] = camList;
            }

            // Process files in Order sequence
            foreach (var item in orderedTypes)
            {
                var dataType = item.Type;
                var order = item.Order;

                if (!filesByType.ContainsKey(dataType) || filesByType[dataType].Count == 0)
                {
                    logger?.LogDebug("[BUILD-GROUPS] {DataType}(Order={Order}): No files, skip", dataType, order);
                    continue;
                }

                var files = filesByType[dataType];
                logger?.LogInformation("[BUILD-GROUPS] {DataType}(Order={Order}): Processing {Count} files", dataType, order, files.Count);

                foreach (var file in files)
                {
                    // Try to find matching group based on previous Order types
                    FileGroup? matchedGroup = null;

                    if (order > 1)
                    {
                        // Find reference DataType (previous Order)
                        var prevItem = orderedTypes.FirstOrDefault(x => x.Order == order - 1);
                        if (prevItem != null)
                        {
                            var prevDataType = prevItem.Type;
                            var minDelay = dataSequenceSettings.GetMinDelay(dataType);
                            var maxDelay = dataSequenceSettings.GetMaxDelay(dataType);

                            logger?.LogDebug("[MATCH] {DataType} file {File} ts={Ts:HH:mm:ss}: Looking for {PrevType} within {Min}~{Max}s",
                                dataType, Path.GetFileName(file.Path), file.Timestamp, prevDataType, minDelay, maxDelay);

                            // Find closest group with prevDataType
                            foreach (var g in groups)
                            {
                                if (!HasDataTypeInGroup(g, prevDataType)) continue;
                                if (HasDataTypeInGroup(g, dataType)) continue; // Non-duplicate filter

                                var timeDiff = (file.Timestamp - g.Timestamp).TotalSeconds;
                                if (timeDiff >= minDelay && timeDiff <= maxDelay)
                                {
                                    if (matchedGroup == null || Math.Abs(timeDiff) < Math.Abs((file.Timestamp - matchedGroup.Timestamp).TotalSeconds))
                                    {
                                        matchedGroup = g;
                                    }
                                }
                            }
                        }
                    }

                    if (matchedGroup != null)
                    {
                        // Merge into existing group
                        MergeFileIntoGroup(matchedGroup, dataType, file.Key, file.Path, camKeys, logger);
                        logger?.LogDebug("[MATCH] ✓ Merged {DataType} {File} into Group {GroupId}",
                            dataType, Path.GetFileName(file.Path), matchedGroup.GroupId);
                    }
                    else
                    {
                        // Create new group
                        var newGroup = CreateGroupFromFile(dataType, file.Key, file.Path, file.Timestamp, lineNumber, camKeys, logger);
                        groups.Add(newGroup);
                        logger?.LogDebug("[MATCH] ✗ Created new group for {DataType} {File}",
                            dataType, Path.GetFileName(file.Path));
                    }
                }
            }

            // Final sort by timestamp
            groups = groups.OrderBy(g => g.Timestamp).ToList();
            return groups;
        }

        /// <summary>
        /// Legacy hardcoded matching logic (fallback)
        /// </summary>
        private static List<FileGroup> BuildLineGroupsLegacy(
            UnmatchedFiles unmatchedFiles,
            int lineNumber,
            string normalKey,
            string nirKey,
            string[] camKeys,
            HashSet<string> consumedNirKeys,
            ILogger? logger,
            Action<LogSeverity, string, string>? uiLog,
            DataSequenceSettings? dataSequenceSettings = null)
        {
            var groups = new List<FileGroup>();

            // Get and sort normal files
            var normalFiles = unmatchedFiles.NormalFolders.ContainsKey(normalKey)
                ? unmatchedFiles.NormalFolders[normalKey]
                : new Dictionary<string, string>();

            // Log normal folder parsing
            var normalParsingResults = normalFiles
                .Select(kvp => new
                {
                    Key = kvp.Key,
                    Path = kvp.Value,
                    Timestamp = ExtractTimestampFromFolderName(kvp.Key)
                })
                .ToList();
            
            var parsedCount = normalParsingResults.Count(x => x.Timestamp.HasValue);
            var failedCount = normalParsingResults.Count(x => !x.Timestamp.HasValue);
            
            logger?.LogInformation("[DIAGNOSTIC] Line {Line} {Key}: Total={Total}, Parsed={Parsed}, Failed={Failed}", 
                lineNumber, normalKey, normalFiles.Count, parsedCount, failedCount);
            
            if (failedCount > 0)
            {
                var failedSamples = normalParsingResults
                    .Where(x => !x.Timestamp.HasValue)
                    .Take(5)
                    .Select(x => x.Key);
                logger?.LogInformation("[DIAGNOSTIC] Failed parsing samples: {Samples}", string.Join(", ", failedSamples));
            }

            var availableNormals = normalParsingResults
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
                if (consumedNirKeys.Contains(kvp.Key)) continue;

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
            
            // Log initial camera queue sizes
            for (int i = 0; i < camKeys.Length; i++)
            {
                logger?.LogInformation("[DIAGNOSTIC] Line {Line} {CamKey}: Initial queue size = {Count}", 
                    lineNumber, camKeys[i], camQueues[i].Count);
            }

            // Create normal-based groups with camera attachments
            foreach (var normal in availableNormals)
            {
                var group = new FileGroup
                {
                    GroupId = "", // Will be assigned later
                    NormalFolder = normal.Key,
                    MainImagePath = Path.Combine(normal.Path, "stitched_original.png"),
                    CreatedAt = normal.Timestamp.GetValueOrDefault(),
                    Timestamp = normal.Timestamp.GetValueOrDefault(),
                    Status = GroupStatus.Complete,
                    HasNir = false,
                    LineNumber = lineNumber,
                    CameraFiles = new Dictionary<string, string>()
                };
                logger?.LogDebug("Created normal-based group (Line {Line}) normal={Normal} ts={Ts}", 
                    lineNumber, normal.Key, normal.Timestamp);

                // Attach camera files
                DateTime? cam1Timestamp = null;

                // Determine DataType for first camera based on line number
                // Line 1: Cam1, Line 2: Cam4 (which uses Cam1 settings via auto-mapping)
                var firstCamDataType = lineNumber == 1 ? DataType.Cam1 : DataType.Cam1;

                // Cam1 (or Cam4) - match based on normal timestamp
                var pickedCam1 = FindMatchingCamFile(
                    normal.Timestamp!.Value, 
                    camQueues[0], 
                    dataSequenceSettings,
                    firstCamDataType,
                    logger);
                
                if (pickedCam1 != null)
                {
                    group.CameraFiles[camKeys[0]] = pickedCam1.Path;
                    cam1Timestamp = pickedCam1.Timestamp;
                    logger?.LogDebug("Matched {Cam} to normal {Normal} diff={Diff}s file={File}", 
                        camKeys[0], normal.Key, 
                        (cam1Timestamp.Value - normal.Timestamp!.Value).TotalSeconds, 
                        pickedCam1.Path);
                }

                // Cam2/3 (or Cam5/6) - match based on cam1 timestamp if available
                if (cam1Timestamp.HasValue)
                {
                    for (int i = 1; i < 3; i++)
                    {
                        // Determine DataType: Line 1 uses Cam2/Cam3, Line 2 uses Cam2/Cam3 (auto-mapped)
                        var camDataType = i == 1 ? DataType.Cam2 : DataType.Cam3;
                        
                        var pickedCam = FindMatchingCamFileFromReference(
                            cam1Timestamp.Value, 
                            camQueues[i], 
                            dataSequenceSettings,
                            camDataType,
                            logger);
                        
                        if (pickedCam != null)
                        {
                            group.CameraFiles[camKeys[i]] = pickedCam.Path;
                            logger?.LogDebug("Matched {Cam} to cam1 ref {RefTs} diff={Diff}s file={File}", 
                                camKeys[i], cam1Timestamp.Value, 
                                (pickedCam.Timestamp - cam1Timestamp.Value).TotalSeconds, 
                                pickedCam.Path);
                        }
                    }
                }
                else
                {
                    // Fallback to normal-based matching
                    for (int i = 1; i < 3; i++)
                    {
                        // Determine DataType: Line 1 uses Cam2/Cam3, Line 2 uses Cam2/Cam3 (auto-mapped)
                        var camDataType = i == 1 ? DataType.Cam2 : DataType.Cam3;
                        
                        var pickedCam = FindMatchingCamFile(
                            normal.Timestamp!.Value, 
                            camQueues[i],
                            dataSequenceSettings,
                            camDataType,
                            logger);
                        
                        if (pickedCam != null)
                        {
                            group.CameraFiles[camKeys[i]] = pickedCam.Path;
                            logger?.LogDebug("Matched {Cam} to normal {Normal} diff={Diff}s file={File}", 
                                camKeys[i], normal.Key, 
                                (pickedCam.Timestamp - normal.Timestamp!.Value).TotalSeconds, 
                                pickedCam.Path);
                        }
                    }
                }

                groups.Add(group);
            }

            // Drain remaining camera files into cam-only groups
            for (int i = 0; i < camKeys.Length; i++)
            {
                var remainingCount = camQueues[i].Count;
                logger?.LogInformation("[DIAGNOSTIC] Line {Line} {CamKey}: Remaining queue size before drain = {Count}", 
                    lineNumber, camKeys[i], remainingCount);
                
                if (remainingCount > 0)
                {
                    var samples = camQueues[i].Take(3).Select(c => $"{c.FileName}@{c.Timestamp:HH:mm:ss}");
                    logger?.LogInformation("[DIAGNOSTIC] Remaining {CamKey} samples: {Samples}", 
                        camKeys[i], string.Join(", ", samples));
                }
                
                DrainCamToGroups(groups, camKeys[i], camQueues[i], lineNumber);
            }

            // Attach NIR files to closest groups
            foreach (var nir in availableNirs)
            {
                int? targetIdx = null;
                double? minDiff = null;

                // Get NIR matching time window from DataSequenceSettings
                double nirMaxDiff = dataSequenceSettings?.GetMaxDelay(DataType.NIR) ?? 50.0;

                logger?.LogDebug("[MATCH-NIR] NIR {NirKey} ts={NirTs:HH:mm:ss.fff}: Searching {GroupCount} groups (maxDiff={Max}s)", 
                    nir.Key, nir.Timestamp!.Value, groups.Count, nirMaxDiff);
                uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", 
                    $"NIR {nir.Key} ts={nir.Timestamp!.Value:HH:mm:ss.fff}: Searching {groups.Count} groups (maxDiff={nirMaxDiff}s)");

                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].HasNir)
                    {
                        logger?.LogDebug("[MATCH-NIR]   Group[{Index}]: Already has NIR, skip", i);
                        uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", $"  Group[{i}]: Already has NIR, skip");
                        continue; // Already has NIR
                    }

                    var timeDiff = (nir.Timestamp!.Value - groups[i].CreatedAt).TotalSeconds;
                    var absDiff = Math.Abs(timeDiff);

                    logger?.LogDebug("[MATCH-NIR]   Group[{Index}]: ts={GroupTs:HH:mm:ss.fff} diff={Diff:F3}s (abs={AbsDiff:F3}s)", 
                        i, groups[i].CreatedAt, timeDiff, absDiff);
                    uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", 
                        $"  Group[{i}]: ts={groups[i].CreatedAt:HH:mm:ss.fff} diff={timeDiff:F3}s (abs={absDiff:F3}s)");

                    // Must be within configured time difference from DataSequenceSettings
                    if (absDiff <= nirMaxDiff)
                    {
                        if (!minDiff.HasValue || absDiff < minDiff.Value)
                        {
                            minDiff = absDiff;
                            targetIdx = i;
                            logger?.LogDebug("[MATCH-NIR]   → New best match: Group[{Index}] absDiff={AbsDiff:F3}s", i, absDiff);
                            uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", $"  → New best match: Group[{i}] absDiff={absDiff:F3}s");
                        }
                    }
                    else
                    {
                        logger?.LogDebug("[MATCH-NIR]   ✗ REJECTED: absDiff={AbsDiff:F3}s > maxDiff={Max}s", absDiff, nirMaxDiff);
                        uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", $"  ✗ REJECTED: absDiff={absDiff:F3}s > maxDiff={nirMaxDiff}s");
                    }
                }

                if (targetIdx.HasValue)
                {
                    // Attach to existing group
                    groups[targetIdx.Value].NirKey = nir.Key;
                    groups[targetIdx.Value].NirFilePath = nir.Path;
                    groups[targetIdx.Value].HasNir = true;
                    logger?.LogDebug("[MATCH-NIR] ✓ MATCHED: NIR {NirKey} → Group[{Idx}] absDiff={Diff:F3}s file={File}", 
                        nir.Key, targetIdx.Value, minDiff, Path.GetFileName(nir.Path));
                    uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", 
                        $"✓ MATCHED: NIR {nir.Key} → Group[{targetIdx.Value}] absDiff={minDiff:F3}s file={Path.GetFileName(nir.Path)}");
                }
                else
                {
                    // Create NIR-only group
                    logger?.LogDebug("[MATCH-NIR] ✗ NO MATCH: Creating NIR-only group for {NirKey}", nir.Key);
                    uiLog?.Invoke(LogSeverity.Debug, "MATCH-NIR", $"✗ NO MATCH: Creating NIR-only group for {nir.Key}");
                    var nirOnlyGroup = new FileGroup
                    {
                        GroupId = "",
                        NirKey = nir.Key,
                        NirFilePath = nir.Path,
                        CreatedAt = nir.Timestamp!.Value,
                        Timestamp = nir.Timestamp.Value,
                        Status = GroupStatus.Complete,
                        HasNir = true,
                        LineNumber = lineNumber,
                        CameraFiles = new Dictionary<string, string>()
                    };
                    groups.Add(nirOnlyGroup);
                    logger?.LogDebug("Created NIR-only group for {NirKey} ts={Ts} path={Path}", 
                        nir.Key, nir.Timestamp, nir.Path);
                }
            }

            // Final sort by time for this line
            groups = groups.OrderBy(g => g.CreatedAt).ToList();

            return groups;
        }

        /// <summary>
        /// Flatten camera files into a sorted queue
        /// </summary>
        private static List<CamFileEntry> FlattenCamFiles(List<TimestampedFile> camFiles)
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
        private static CamFileEntry? FindMatchingCamFile(
            DateTime normalTimestamp, 
            List<CamFileEntry> queue,
            DataSequenceSettings? dataSequenceSettings,
            DataType cameraDataType,
            ILogger? logger)
        {
            if (queue == null || queue.Count == 0)
            {
                logger?.LogDebug("[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: Queue empty, no match", 
                    cameraDataType, normalTimestamp);
                return null;
            }

            // If no DataSequenceSettings, use sequential matching
            if (dataSequenceSettings == null)
            {
                var item = queue[0];
                queue.RemoveAt(0);
                logger?.LogDebug("[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: Sequential match -> {File}", 
                    cameraDataType, normalTimestamp, Path.GetFileName(item.Path));
                return item;
            }

            // Get camera matching time window from DataSequenceSettings
            // Use the specific camera's DataType settings
            double camMinDiff = dataSequenceSettings.GetMinDelay(cameraDataType);
            double camMaxDiff = dataSequenceSettings.GetMaxDelay(cameraDataType);

            logger?.LogDebug("[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: Searching queue (size={QueueSize}, window={Min}~{Max}s)", 
                cameraDataType, normalTimestamp, queue.Count, camMinDiff, camMaxDiff);

            // Time-based matching using DataSequenceSettings
            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];
                var diff = (item.Timestamp - normalTimestamp).TotalSeconds;

                logger?.LogDebug("[MATCH]   Candidate[{Index}]: {File} ts={CamTs:HH:mm:ss.fff} diff={Diff:F3}s", 
                    i, Path.GetFileName(item.Path), item.Timestamp, diff);

                // Check if within valid range from DataSequenceSettings
                if (diff >= camMinDiff && diff <= camMaxDiff)
                {
                    queue.RemoveAt(i);
                    logger?.LogDebug("[MATCH]   ✓ MATCHED: {File} (diff={Diff:F3}s within {Min}~{Max}s)", 
                        Path.GetFileName(item.Path), diff, camMinDiff, camMaxDiff);
                    return item;
                }
                else
                {
                    logger?.LogDebug("[MATCH]   ✗ REJECTED: diff={Diff:F3}s outside range {Min}~{Max}s", 
                        diff, camMinDiff, camMaxDiff);
                }
            }

            logger?.LogDebug("[MATCH] {CamType} vs Normal {NormalTs:HH:mm:ss.fff}: No match found in queue", 
                cameraDataType, normalTimestamp);
            return null; // No match found
        }

        /// <summary>
        /// Find matching camera file based on reference camera timestamp (cam1 or cam4)
        /// </summary>
        private static CamFileEntry? FindMatchingCamFileFromReference(
            DateTime referenceTimestamp, 
            List<CamFileEntry> queue,
            DataSequenceSettings? dataSequenceSettings,
            DataType cameraDataType,
            ILogger? logger = null)
        {
            if (queue == null || queue.Count == 0)
            {
                logger?.LogDebug("[MATCH-REF] {CamType} vs Ref {RefTs:HH:mm:ss.fff}: Queue empty, no match", 
                    cameraDataType, referenceTimestamp);
                return null;
            }

            // If no DataSequenceSettings, use sequential matching
            if (dataSequenceSettings == null)
            {
                var item = queue[0];
                queue.RemoveAt(0);
                logger?.LogDebug("[MATCH-REF] {CamType} vs Ref {RefTs:HH:mm:ss.fff}: Sequential match -> {File}", 
                    cameraDataType, referenceTimestamp, Path.GetFileName(item.Path));
                return item;
            }

            // Get time window from DataSequenceSettings for this specific camera type
            // For secondary cameras (Cam2/3 or Cam5/6), use their specific settings
            double maxDiff = dataSequenceSettings.GetMaxDelay(cameraDataType);

            logger?.LogDebug("[MATCH-REF] {CamType} vs Ref {RefTs:HH:mm:ss.fff}: Searching queue (size={QueueSize}, maxDiff={Max}s)", 
                cameraDataType, referenceTimestamp, queue.Count, maxDiff);

            // Time-based matching - must be same or later, within maxDiff
            for (int i = 0; i < queue.Count; i++)
            {
                var item = queue[i];
                var diff = (item.Timestamp - referenceTimestamp).TotalSeconds;

                logger?.LogDebug("[MATCH-REF]   Candidate[{Index}]: {File} ts={CamTs:HH:mm:ss.fff} diff={Diff:F3}s", 
                    i, Path.GetFileName(item.Path), item.Timestamp, diff);

                // 0 <= diff <= maxDiff (same time or up to maxDiff seconds later)
                if (diff >= 0 && diff <= maxDiff)
                {
                    queue.RemoveAt(i);
                    logger?.LogDebug("[MATCH-REF]   ✓ MATCHED: {File} (diff={Diff:F3}s within 0~{Max}s)", 
                        Path.GetFileName(item.Path), diff, maxDiff);
                    return item;
                }
                else
                {
                    logger?.LogDebug("[MATCH-REF]   ✗ REJECTED: diff={Diff:F3}s outside range 0~{Max}s", 
                        diff, maxDiff);
                }
            }

            logger?.LogDebug("[MATCH-REF] {CamType} vs Ref {RefTs:HH:mm:ss.fff}: No match found in queue", 
                cameraDataType, referenceTimestamp);
            return null;
        }

        /// <summary>
        /// Drain remaining camera files into cam-only groups
        /// </summary>
        private static void DrainCamToGroups(
            List<FileGroup> groups, 
            string camKey, 
            List<CamFileEntry> queue, 
            int lineNumber)
        {
            foreach (var item in queue)
            {
                var group = new FileGroup
                {
                    GroupId = "",
                    CreatedAt = item.Timestamp,
                    Timestamp = item.Timestamp,
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
        private static DateTime? ExtractTimestampFromFolderName(string folderName)
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
        /// Supports multiple formats: run_120251201T140542, 20251201T140542, etc.
        /// </summary>
        private static DateTime? ExtractTimestampFromNirKey(string nirKey)
        {
            if (string.IsNullOrEmpty(nirKey))
                return null;

            // Pattern 1: run_N prefix followed by 8 digits (YYYYMMDD) + T + 6 digits (time)
            // Example: run_120251201T140542 -> extract 20251201T140542
            // Match "run_" + single digit + timestamp (YYYYMMDDTHHMMSS)
            var match = System.Text.RegularExpressions.Regex.Match(nirKey, @"run_\d(\d{8}T\d{6})");
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

            // Pattern 2: 8 digits (YYYYMMDD) + T + 6 digits (time) without prefix
            // Example: 20250926T103033
            match = System.Text.RegularExpressions.Regex.Match(nirKey, @"(\d{8}T\d{6})");
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

            return null;
        }

        /// <summary>
        /// Check if group has a specific DataType
        /// </summary>
        private static bool HasDataTypeInGroup(FileGroup group, DataType dataType)
        {
            return dataType switch
            {
                DataType.NIR => group.HasNir && !string.IsNullOrEmpty(group.NirKey),
                DataType.Normal => !string.IsNullOrEmpty(group.NormalFolder),
                DataType.Cam1 => group.CameraFiles.ContainsKey("cam1") && !string.IsNullOrEmpty(group.CameraFiles["cam1"]),
                DataType.Cam2 => group.CameraFiles.ContainsKey("cam2") && !string.IsNullOrEmpty(group.CameraFiles["cam2"]),
                DataType.Cam3 => group.CameraFiles.ContainsKey("cam3") && !string.IsNullOrEmpty(group.CameraFiles["cam3"]),
                _ => false
            };
        }

        /// <summary>
        /// Merge file into existing group
        /// </summary>
        private static void MergeFileIntoGroup(FileGroup group, DataType dataType, string key, string path, string[] camKeys, ILogger? logger)
        {
            switch (dataType)
            {
                case DataType.NIR:
                    if (string.IsNullOrEmpty(group.NirKey))
                    {
                        group.NirKey = key;
                        group.NirFilePath = path;
                        group.HasNir = true;
                    }
                    break;

                case DataType.Normal:
                    if (string.IsNullOrEmpty(group.NormalFolder))
                    {
                        group.NormalFolder = key;
                        group.MainImagePath = Path.Combine(path, "stitched_original.png");
                    }
                    break;

                case DataType.Cam1:
                    if (!group.CameraFiles.ContainsKey(camKeys[0]) || string.IsNullOrEmpty(group.CameraFiles[camKeys[0]]))
                    {
                        group.CameraFiles[camKeys[0]] = path;
                    }
                    break;

                case DataType.Cam2:
                    if (!group.CameraFiles.ContainsKey(camKeys[1]) || string.IsNullOrEmpty(group.CameraFiles[camKeys[1]]))
                    {
                        group.CameraFiles[camKeys[1]] = path;
                    }
                    break;

                case DataType.Cam3:
                    if (!group.CameraFiles.ContainsKey(camKeys[2]) || string.IsNullOrEmpty(group.CameraFiles[camKeys[2]]))
                    {
                        group.CameraFiles[camKeys[2]] = path;
                    }
                    break;
            }
        }

        /// <summary>
        /// Create new group from file
        /// </summary>
        private static FileGroup CreateGroupFromFile(DataType dataType, string key, string path, DateTime timestamp, int lineNumber, string[] camKeys, ILogger? logger)
        {
            var group = new FileGroup
            {
                GroupId = "", // Will be assigned later
                CreatedAt = timestamp,
                Timestamp = timestamp,
                Status = GroupStatus.Complete,
                LineNumber = lineNumber,
                CameraFiles = new Dictionary<string, string>()
            };

            switch (dataType)
            {
                case DataType.NIR:
                    group.NirKey = key;
                    group.NirFilePath = path;
                    group.HasNir = true;
                    break;

                case DataType.Normal:
                    group.NormalFolder = key;
                    group.MainImagePath = Path.Combine(path, "stitched_original.png");
                    break;

                case DataType.Cam1:
                    group.CameraFiles[camKeys[0]] = path;
                    break;

                case DataType.Cam2:
                    group.CameraFiles[camKeys[1]] = path;
                    break;

                case DataType.Cam3:
                    group.CameraFiles[camKeys[2]] = path;
                    break;
            }

            return group;
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
