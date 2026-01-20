using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChronoView.Core.GroupIdGeneration;
using ChronoView.Core.Localization;
using ChronoView.Core.Nir;
using ChronoView.Helpers;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    public class GroupManager : IGroupManager
    {
        private readonly ILogger<GroupManager> _logger;
        private readonly INirFileResolver _nirFileResolver;
        private readonly IGroupIdGenerator _idGenerator;
        private readonly ConcurrentDictionary<string, FileGroup> _activeGroups = new();
        private readonly List<(string Path, DateTime Timestamp, DateTime AddedAt)> _pendingNirFiles = new();
        private readonly object _lockObject = new();
        private readonly ITimestampCache _folderTimestamps;
        private readonly Dictionary<string, bool> _processedFiles = new(StringComparer.OrdinalIgnoreCase);
        // 각 DataType별 + LineNumber별 마지막 배정 그룹 추적 (라인별 컬럼 내 순서성 보장)
        private readonly Dictionary<(DataType Type, int LineNumber), string> _lastAssignedGroup = new();

        public event EventHandler<FileGroup>? GroupCreated;
        public event EventHandler<FileGroup>? GroupUpdated;
        public event EventHandler<string>? GroupRemoved;
        public event Action<LogSeverity, string>? Log; // UI Logging Event

        public IEnumerable<FileGroup> ActiveGroups => _activeGroups.Values;

        public GroupManager(ILogger<GroupManager> logger, INirFileResolver nirFileResolver, ITimestampCache folderTimestamps, IGroupIdGenerator idGenerator)
        {
            _logger = logger;
            _nirFileResolver = nirFileResolver;
            _folderTimestamps = folderTimestamps;
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        }

        private void RaiseLog(string resourceKey, int? lineNumber = null, LogSeverity severity = LogSeverity.Info, params object[] args)
        {
            // NOTE: [Line X] prefix is added by MainWindowViewModel.AddLogMessage
            // based on InferLineNumber() parsing. Do NOT add prefix here to avoid duplication.
            var message = LocalizationManager.GetString(resourceKey, args);
            Log?.Invoke(severity, message);
        }

        /// <summary>
        /// Filters groups by line number to ensure strict line separation.
        /// </summary>
        private IEnumerable<FileGroup> FilterByLine(IEnumerable<FileGroup> groups, int lineNumber)
            => groups.Where(g => g.LineNumber == lineNumber);

        public FileGroup? FindGroupById(string groupId)
        {
            return _activeGroups.TryGetValue(groupId, out var group) ? group : null;
        }

        public int GetActiveGroupsCount() => _activeGroups.Count;

        public async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config, bool captureSuccess = true)
        {
            try
            {
                string processPath = filePath;
                
                // Flag-based duplicate tracking: false = incomplete (no image), true = complete
                lock (_lockObject)
                {
                    if (_processedFiles.TryGetValue(filePath, out bool isComplete))
                    {
                        if (isComplete)
                        {
                            _logger.LogDebug("Common: File already processed completely, skipping: {Path}", filePath);
                            return null;
                        }
                        
                        // Check if image now exists for Normal folders
                        if (fileType == FileType.Normal)
                        {
                            var checkPath = Directory.Exists(filePath) ? filePath : Path.GetDirectoryName(filePath);
                            var stitchedPath = Path.Combine(checkPath ?? filePath, "stitched_original.png");
                            bool hasImage = File.Exists(stitchedPath);
                            
                            // Determine line number for logging
                            int lineNumber = DetermineLineNumber(filePath, fileType, config);
                            
                            if (!hasImage)
                            {
                                _logger.LogDebug("Re-process skip: 이미지 없음 - {Path}", filePath);
                                RaiseLog("Log_Debug_ImageCheck_NoImage", lineNumber, LogSeverity.Debug, Path.GetFileName(filePath));
                                return null; // Still no image, skip re-processing
                            }
                            _logger.LogInformation("Re-process: 이미지 발견! - {Path}", filePath);
                            RaiseLog("Log_Debug_ImageCheck_Found", lineNumber, LogSeverity.Debug, Path.GetFileName(filePath));
                        }
                    }
                    else
                    {
                        _processedFiles[filePath] = false; // Initially incomplete
                    }
                }

                if (fileType == FileType.Normal && !string.IsNullOrEmpty(Path.GetExtension(filePath)))
                {
                    var parentFolder = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(parentFolder))
                    {
                        processPath = parentFolder;
                    }
                }

                var newGroupTemplate = CreateGroupFromSingleFile(processPath, fileType, config);
                if (newGroupTemplate == null) return null;

                // Handle Normal folder timestamp caching
                if (fileType == FileType.Normal)
                {
                    if (_folderTimestamps.TryGet(processPath, out DateTime cachedTimestamp))
                    {
                        newGroupTemplate.Timestamp = cachedTimestamp;
                    }
                }

                FileGroup? targetGroup;
                FileGroup? evictedGroupForEvent = null; // For eviction event outside lock
                bool isNew = false;
                bool isDataChanged = false;

                lock (_lockObject)
                {
                    var newGroupType = DetermineDataTypeForGroup(newGroupTemplate);
                    string colName = GetFriendlyColumnName(newGroupType, newGroupTemplate.LineNumber);
                    string fileName = Path.GetFileName(filePath);

                    FileGroup? existingGroup = FindMatchingExistingGroup(newGroupTemplate, config);

                    if (existingGroup != null)
                    {
                        lock (existingGroup)
                        {
                            isDataChanged = MergeGroups(existingGroup, newGroupTemplate);
                        }
                        targetGroup = existingGroup;
                    }
                    else
                    {
                        // ========== NEW: EVICTION CHECK ==========
                        // Before creating a new group, check if this file should evict an existing group
                        var evictionResult = CheckEvictionNeeded(newGroupTemplate, config);
                        
                        if (evictionResult.ShouldEvict && evictionResult.VictimGroup != null)
                        {
                            var victimGroup = evictionResult.VictimGroup;
                            
                            // Step 1: Clone victim group to new ID
                            string evictedGroupId = _idGenerator.GenerateNextId(victimGroup.LineNumber);
                            evictedGroupForEvent = victimGroup.CloneWithNewId(evictedGroupId);
                            
                            // Step 2: Reset victim group and merge new data into it
                            lock (victimGroup)
                            {
                                victimGroup.ResetData();
                                MergeGroups(victimGroup, newGroupTemplate);
                            }
                            
                            RaiseLog("Log_Info_Eviction", newGroupTemplate.LineNumber, LogSeverity.Info, colName, fileName, victimGroup.GroupId, evictedGroupForEvent.GroupId);
                            
                            // Step 3: Register evicted group
                            _activeGroups[evictedGroupId] = evictedGroupForEvent;
                            
                            // Update last assigned group for the evicted data type
                            var evictedDataType = DetermineDataTypeForGroup(evictedGroupForEvent);
                            _lastAssignedGroup[(evictedDataType, evictedGroupForEvent.LineNumber)] = evictedGroupForEvent.GroupId;
                            
                            targetGroup = victimGroup;
                            // Mark both as needing UI update
                            isDataChanged = true;
                        }
                        else
                        {
                            // Normal path: Generate new Group ID
                            string newGroupId = _idGenerator.GenerateNextId(newGroupTemplate.LineNumber);
                            
                            // ID 중복 가능성은 낮으나 방어적 코드로 유지
                            if (!_activeGroups.TryAdd(newGroupId, newGroupTemplate))
                            {
                                newGroupId = _idGenerator.GenerateNextId(newGroupTemplate.LineNumber);
                            }
                            
                            newGroupTemplate.GroupId = newGroupId;
                            _activeGroups[newGroupId] = newGroupTemplate;
                            
                            // ★ 새 그룹 생성 시 마지막 배정 그룹 업데이트 (라인별 분리)
                            _lastAssignedGroup[(newGroupType, newGroupTemplate.LineNumber)] = newGroupTemplate.GroupId;
                            
                            RaiseLog("Log_Info_NewGroup", newGroupTemplate.LineNumber, LogSeverity.Info, colName, fileName, newGroupTemplate.GroupId);
                            targetGroup = newGroupTemplate;
                            isNew = true;
                        }
                        
                    }
                }

                if (isNew)
                {
                    GroupCreated?.Invoke(this, targetGroup);
                }
                else if (isDataChanged)
                {
                    GroupUpdated?.Invoke(this, targetGroup);
                }
                
                // Fire GroupCreated for evicted group (moved outside lock)
                if (evictedGroupForEvent != null)
                {
                    GroupCreated?.Invoke(this, evictedGroupForEvent);
                }

                // Match pending NIR
                if (!targetGroup.HasNir && fileType != FileType.Nir)
                {
                    if (TryMatchPendingNirToGroup(targetGroup, config))
                    {
                        GroupUpdated?.Invoke(this, targetGroup);
                    }
                }

                // ★ Mark as complete ONLY if captureSuccess AND image exists for Normal folders
                if (fileType == FileType.Normal && !string.IsNullOrEmpty(processPath) && captureSuccess)
                {
                    var stitchedPath = Path.Combine(processPath, "stitched_original.png");
                    if (File.Exists(stitchedPath))
                    {
                        lock (_lockObject)
                        {
                            _processedFiles[filePath] = true; // Mark complete
                        }
                    }
                }

                return targetGroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateOrUpdateGroupAsync for {Path}", filePath);
                return null;
            }
        }

        public bool RemoveGroup(string groupId)
        {
            if (_activeGroups.TryRemove(groupId, out _))
            {
                GroupRemoved?.Invoke(this, groupId);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _activeGroups.Clear();
            lock (_lockObject)
            {
                _pendingNirFiles.Clear();
                _processedFiles.Clear();
            }
        }

        public void ResetState()
        {
            Clear();
            _lastAssignedGroup.Clear();
            _idGenerator.Reset();
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAllProcessedFilePaths()
        {
            lock (_lockObject)
            {
                return _processedFiles.Keys.ToList(); // Return a safe snapshot
            }
        }

        private FileGroup? CreateGroupFromSingleFile(string filePath, FileType fileType, ApplicationConfiguration config)
        {
            var lineNumber = DetermineLineNumber(filePath, fileType, config);
            var group = new FileGroup
            {
                GroupId = "temp",
                LineNumber = lineNumber,
                CreatedAt = DateTime.UtcNow,
                Status = GroupStatus.Pending,
                CameraFiles = new Dictionary<string, string>()
            };

            var timestamp = FileNamingHelper.ExtractTimestamp(filePath, Enum.GetName(typeof(FileType), fileType) ?? "");
            if (!timestamp.HasValue) return null;

            switch (fileType)
            {
                case FileType.Nir:
                    bool allowNirLeader = config.DataSequenceSettings?.GetOrderedTypes().FirstOrDefault() == DataType.NIR;
                    if (!allowNirLeader)
                    {
                        lock (_lockObject)
                        {
                            _pendingNirFiles.Add((filePath, timestamp.Value, DateTime.UtcNow));
                        }
                        return null;
                    }
                    group.NirFilePath = filePath;
                    group.NirKey = _nirFileResolver.GetNirKey(filePath);
                    group.HasNir = true;
                    group.Timestamp = timestamp.Value;
                    break;

                case FileType.Normal:
                    group.NormalFolder = filePath;
                    var mainImagePath = Path.Combine(filePath, "stitched_original.png");
                    
                    // If direct path doesn't exist, search recursively in the folder
                    if (!File.Exists(mainImagePath))
                    {
                        try
                        {
                            var found = Directory.GetFiles(filePath, "stitched_original.png", SearchOption.AllDirectories).FirstOrDefault();
                            if (!string.IsNullOrEmpty(found))
                            {
                                mainImagePath = found;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore search errors, keep original path
                        }
                    }
                    
                    group.MainImagePath = mainImagePath;
                    group.Timestamp = timestamp.Value;
                    group.Status = GroupStatus.Complete;
                    break;

                case FileType.Camera:
                    for (int i = 1; i <= 6; i++)
                    {
                        var camPath = config.MatchingSettings.GetCameraPath(i);
                        if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                        {
                            group.CameraFiles[$"cam{i}"] = filePath;
                            group.Timestamp = timestamp.Value;
                            break;
                        }
                    }
                    if (group.CameraFiles.Count == 0) return null;
                    break;

                default:
                    return null;
            }

            return group;
        }

        private int DetermineLineNumber(string filePath, FileType fileType, ApplicationConfiguration config)
        {
            // For Normal folders, use centralized helper with UseFolderSuffix support
            if (fileType == FileType.Normal)
            {
                int line = NormalFolderHelper.DetermineLineNumber(
                    filePath,
                    config.MatchingSettings.UseFolderSuffix,
                    config.MatchingSettings.Normal1Path,
                    config.MatchingSettings.Normal2Path);
                
                RaiseLog("Log_Debug_LineCheck_Normal", line, LogSeverity.Debug, Path.GetFileName(filePath));
                return line;
            }

            // NIR path check
            if (!string.IsNullOrEmpty(config.MatchingSettings.Nir2Path))
            {
                RaiseLog("Log_Debug_LineCheck_Nir2Path", 2, LogSeverity.Debug, config.MatchingSettings.Nir2Path);
                if (filePath.StartsWith(config.MatchingSettings.Nir2Path, StringComparison.OrdinalIgnoreCase))
                {
                    RaiseLog("Log_Debug_LineMatches_Nir2", 2, LogSeverity.Debug);
                    return 2;
                }
            }

            // Camera path check (Line 2 = Cameras 4-6)
            for (int i = 4; i <= 6; i++)
            {
                var camPath = config.MatchingSettings.GetCameraPath(i);
                if (!string.IsNullOrEmpty(camPath))
                {
                     RaiseLog("Log_Debug_LineCheck_CamPath", 2, LogSeverity.Debug, i, camPath);
                     if (filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase))
                     {
                         RaiseLog("Log_Debug_LineMatches_Cam", 2, LogSeverity.Debug, i);
                         return 2;
                     }
                }
            }

            // Fallback Check: If file looks like Line 2 but failed detection
            string fileName = Path.GetFileName(filePath);
            if (fileName.Contains("nir2", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("cam4", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("cam5", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("cam6", StringComparison.OrdinalIgnoreCase))
            {
                 RaiseLog("Log_Warn_LineMismatch_Fallback", 1, LogSeverity.Warning, fileName);
            }
            else
            {
                 // LineDecision 로그는 영문 리소스로 제공하지 않고 Debug 로그만 사용
                 // RaiseLog 호출 제거 (영문 메시지가 필요 없음)
            }

            return 1;
        }

        /// <summary>
        /// Checks if a new file should evict an existing group based on delay settings.
        /// This happens when the new file's expected predecessor range indicates it should
        /// precede an existing group in the sequence order.
        /// </summary>
        private (bool ShouldEvict, FileGroup? VictimGroup) CheckEvictionNeeded(
            FileGroup newGroup, 
            ApplicationConfiguration config)
        {
            if (config.DataSequenceSettings == null || newGroup.Timestamp == DateTime.MinValue)
                return (false, null);

            var snapshot = _activeGroups.Values.ToArray();
            var orderedTypes = config.DataSequenceSettings.GetOrderedTypes();
            if (orderedTypes.Count == 0)
                return (false, null);

            var newGroupType = DetermineDataTypeForGroup(newGroup);
            var normalizedNewType = NormalizeForSequence(newGroupType);
            var newGroupOrder = GetPriority(normalizedNewType, config);

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

            // Find groups with lower-order types (earlier in sequence) that are "too late"
            foreach (var candidate in FilterByLine(snapshot, newGroup.LineNumber)
                .OrderBy(g => ExtractNumericSuffix(g.GroupId)))
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
                        "Eviction triggered: {NewType} (T={NewTime}) should precede {CandidateType} (T={CandidateTime}) in {GroupId}. " +
                        "Expected predecessor before T={MaxTime}, but found T={ActualTime}",
                        newGroupType, newGroup.Timestamp.ToString("HHmmss"),
                        candidateType, candidateTimestamp.Value.ToString("HHmmss"),
                        candidate.GroupId, expectedPredMax.ToString("HHmmss"), candidateTimestamp.Value.ToString("HHmmss"));

                    return (true, candidate);
                }
            }

            return (false, null);
        }

        private FileGroup? FindMatchingExistingGroup(FileGroup newGroup, ApplicationConfiguration config)
        {
            var snapshot = _activeGroups.Values.ToArray();

            // Match 1: By NormalFolder (Line-filtered to prevent cross-contamination)
            if (!string.IsNullOrEmpty(newGroup.NormalFolder))
            {
                var match = FilterByLine(snapshot, newGroup.LineNumber)
                    .FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder);
                if (match != null) return match;
            }

            // Match 2: By NirKey (CRITICAL: Line-filtered to prevent NirKey collision across lines)
            if (!string.IsNullOrEmpty(newGroup.NirKey))
            {
                var match = FilterByLine(snapshot, newGroup.LineNumber)
                    .FirstOrDefault(g => g.NirKey == newGroup.NirKey);
                if (match != null) return match;
            }

            // Match 3: By Timestamp + LineNumber + Priority
            // Match 3: By Timestamp + LineNumber + Priority
            if (newGroup.Timestamp != DateTime.MinValue && config.DataSequenceSettings != null)
            {
                var orderedTypes = config.DataSequenceSettings.GetOrderedTypes();
                if (orderedTypes.Count > 0)
                {
                    var newGroupType = DetermineDataTypeForGroup(newGroup);
                    var normalizedType = NormalizeForSequence(newGroupType); // For settings lookup
                    // IMPORTANT: predecessor/successor must be derived from the configured sequence order,
                    // not from hardcoded camera assumptions.
                    // Use list index (orderedTypes is already sorted by Order in DataSequenceSettings.GetOrderedTypes()).
                    int currentIndex = orderedTypes.FindIndex(t => NormalizeForSequence(t) == normalizedType);
                    if (currentIndex == -1)
                    {
                        // This data type is not enabled/represented in the sequence; do not attempt sequence matching.
                        return null;
                    }

                    DataType? predecessorSeqType = currentIndex > 0 ? orderedTypes[currentIndex - 1] : null;
                    DataType? successorSeqType = currentIndex < orderedTypes.Count - 1 ? orderedTypes[currentIndex + 1] : null;

                    // Reference camera option:
                    // If enabled, Cam2/Cam3 use Cam1 (and Line2 Cam5/Cam6 use Cam4) as the timestamp reference
                    // instead of the immediately preceding sequence item.
                    if (config.DataSequenceSettings.CompareToReferenceCamera &&
                        (normalizedType == DataType.Cam2 || normalizedType == DataType.Cam3))
                    {
                        int cam1Index = orderedTypes.FindIndex(t => NormalizeForSequence(t) == DataType.Cam1);
                        if (cam1Index != -1 && cam1Index < currentIndex)
                        {
                            predecessorSeqType = DataType.Cam1;
                        }
                    }

                    // Map sequence camera types (Cam1-3) to actual per-line camera types for group contents checks.
                    DataType? predecessorType = predecessorSeqType.HasValue
                        ? MapCameraTypeForLine(predecessorSeqType.Value, newGroup.LineNumber)
                        : null;
                    DataType? successorType = successorSeqType.HasValue
                        ? MapCameraTypeForLine(successorSeqType.Value, newGroup.LineNumber)
                        : null;

                    var candidates = new List<(FileGroup Group, double AbsDiff, bool HasPredecessor, string TargetName)>();
                    string colName = GetFriendlyColumnName(newGroupType, newGroup.LineNumber);
                    string fileName = GetFileNameForGroup(newGroup, newGroupType);

                    // Forward Matching (Checking against Successor)
                    if (predecessorType == null && successorType != null)
                    {
                        var succGroupType = successorType.Value;
                        var succNormalized = NormalizeForSequence(succGroupType);
                        var minDelay = config.DataSequenceSettings.GetMinDelay(succNormalized);
                        var maxDelay = config.DataSequenceSettings.GetMaxDelay(succNormalized);

                        foreach (var candidate in FilterByLine(snapshot, newGroup.LineNumber)
                            .OrderBy(g => ExtractNumericSuffix(g.GroupId)))
                        {
                            if (HasDataType(candidate, newGroupType)) continue;
                            // Sequence principle: candidate must already contain the successor type we're matching against.
                            if (!HasDataType(candidate, succGroupType)) continue;

                            var succTs = GetTimestampForDataType(candidate, succGroupType) ?? candidate.Timestamp;
                            var timeDiff = (succTs - newGroup.Timestamp).TotalSeconds;
                             
                            _logger.LogTrace("Checking Fwd Candidate {GroupId}: TimeDiff={Diff}s (Range: {Min}-{Max})", candidate.GroupId, timeDiff, minDelay, maxDelay);

                            if (timeDiff >= minDelay && timeDiff <= maxDelay)
                            {
                                candidates.Add((candidate, Math.Abs(timeDiff), false, succNormalized.ToString()));
                            }
                        }
                    }
                    // Backward Matching (Checking against Predecessor)
                    else if (predecessorType != null)
                    {
                        var predType = predecessorType.Value;
                        var minDelay = config.DataSequenceSettings.GetMinDelay(normalizedType);
                        var maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedType);

                        _logger.LogTrace("Matching {Type} (Index {Index}) - Looking for Predecessor {PredType} in stored groups. Time Window: {Min}-{Max}s",
                            newGroupType, currentIndex, predType, minDelay, maxDelay);

                        // ★ DEBUG: Log sorted group order for diagnosis
                        var sortedGroups = FilterByLine(snapshot, newGroup.LineNumber)
                            .OrderBy(g => ExtractNumericSuffix(g.GroupId))
                            .ToList();
                        _logger.LogInformation("DEBUG {Type} Line{Line}: Group iteration order = {Order}",
                            newGroupType, newGroup.LineNumber,
                            string.Join(", ", sortedGroups.Select(g => $"{g.GroupId}({ExtractNumericSuffix(g.GroupId)})")));

                        // Track last "AlreadyHasType" candidate for summary logging
                        (string groupId, string anchorFile, string anchorTime)? lastAlreadyHasType = null;

                        foreach (var candidate in sortedGroups)
                        {
                            if (HasDataType(candidate, newGroupType))
                            {
                                var candAnchorFile = GetAnchorFileName(candidate);
                                var candAnchorTime = candidate.Timestamp.ToString("HHmmss");
                                // Store for later logging (only log the last one)
                                lastAlreadyHasType = (candidate.GroupId, candAnchorFile, candAnchorTime);
                                continue;
                            }

                            // ★ 컬럼 내 순서성 체크: 마지막 배정 그룹보다 이후 그룹만 허용 (라인별 분리)
                            var sequenceKey = (newGroupType, newGroup.LineNumber);
                            if (_lastAssignedGroup.TryGetValue(sequenceKey, out var lastGroupId))
                            {
                                if (CompareGroupId(candidate.GroupId, lastGroupId) <= 0)
                                {
                                    _logger.LogTrace("Skipping {GroupId} for {Type} Line{Line}: must be after {LastGroup}",
                                        candidate.GroupId, newGroupType, newGroup.LineNumber, lastGroupId);
                                    // UI 로그 추가 - 컬럼 내 순서성 위반
                                    var candAnchorFile = GetAnchorFileName(candidate);
                                    var candAnchorTime = FormatHms(candidate.Timestamp);
                                    RaiseLog("Log_Info_MatchExcluded_ColumnOrder", newGroup.LineNumber, LogSeverity.Info,
                                        colName, fileName, candidate.GroupId, lastGroupId, candAnchorFile, candAnchorTime);
                                    continue;
                                }
                            }
                            
                            DateTime comparisonTimestamp = candidate.Timestamp; // Default to group anchor
                            string comparisonSourceType = "GroupAnchor";
                            string comparisonSourceFile = GetAnchorFileName(candidate);
                            
                            bool hasPred;
                            if (predType == DataType.NIR)
                            {
                                hasPred = true; // User feedback: Relax NIR check
                                if (candidate.HasNir)
                                {
                                    // Note: candidate.Timestamp is the group's anchor; keep behavior as-is but make logs explicit.
                                    comparisonTimestamp = candidate.Timestamp;
                                    comparisonSourceType = "NIR";
                                    comparisonSourceFile = GetFileNameForDataTypeInGroup(candidate, DataType.NIR);
                                }
                            }
                            else
                            {
                                hasPred = HasDataType(candidate, predType);
                                if (hasPred)
                                {
                                    var predTimestamp = GetTimestampForDataType(candidate, predType);
                                    if (predTimestamp.HasValue)
                                    {
                                        comparisonTimestamp = predTimestamp.Value;
                                        comparisonSourceType = predType.ToString();
                                        comparisonSourceFile = GetFileNameForDataTypeInGroup(candidate, predType);
                                    }
                                }
                            }

                            // Enforce strict predecessor presence for non-NIR predecessors.
                            // This aligns GroupManager matching with the sequence principle (predecessor-based matching only),
                            // and prevents "anchor-based" accidental matches (especially for camera columns).
                            if (!hasPred && predType != DataType.NIR)
                            {
                                var candAnchorFile = GetAnchorFileName(candidate);
                                var candAnchorTime = candidate.Timestamp.ToString("HHmmss");
                                RaiseLog("Log_Info_MatchExcluded_MissingPredecessor", newGroup.LineNumber, LogSeverity.Info,
                                    colName, fileName, candidate.GroupId, predType.ToString(), candAnchorFile, candAnchorTime);
                                _logger.LogTrace("Skipping Candidate {GroupId}: missing predecessor {PredType}", candidate.GroupId, predType);
                                continue;
                            }

                            var timeDiff = (newGroup.Timestamp - comparisonTimestamp).TotalSeconds;
                            bool skipOrdering = (predType == DataType.NIR && !HasDataType(candidate, DataType.NIR));

                            if (timeDiff < 0 && !skipOrdering)
                            {
                                var candAnchorFile = GetAnchorFileName(candidate);
                                var candAnchorTime = comparisonTimestamp.ToString("HHmmss");
                                var newGroupTime = newGroup.Timestamp.ToString("HHmmss");
                                RaiseLog("Log_Info_MatchExcluded_TimeOrder", newGroup.LineNumber, LogSeverity.Info,
                                    colName, fileName, candidate.GroupId, newGroupTime, candAnchorTime);
                                continue;
                            }

                            var absDiff = Math.Abs(timeDiff);
                            
                            _logger.LogTrace("Candidate {GroupId}: Diff={Diff:F2}s, HasPred={HasPred} ({PredType})", candidate.GroupId, timeDiff, hasPred, predType);

                            // Strict tolerance check against predecessor
                            if (absDiff >= minDelay && absDiff <= maxDelay) 
                            {
                                // ★ NEW: Successor Validation
                                // Check that newFile's timestamp is within valid range of all existing successors
                                bool successorValidationPassed = true;
                                string successorRejectionReason = "";
                                
                                // Validate against all configured successors (types after currentIndex in orderedTypes).
                                for (int si = currentIndex + 1; si < orderedTypes.Count; si++)
                                {
                                    var succSeq = orderedTypes[si];
                                    var succGroupType = MapCameraTypeForLine(succSeq, newGroup.LineNumber);
                                    var normalizedSucc = NormalizeForSequence(succSeq);

                                    // Reference camera option compatibility:
                                    // When CompareToReferenceCamera is enabled, Cam2/Cam3 (and Line2 Cam5/Cam6 via normalization)
                                    // are compared to the reference camera (Cam1/Cam4) instead of to each other.
                                    // Therefore, do NOT enforce an additional ordering/time-window constraint between sibling cameras.
                                    if (config.DataSequenceSettings.CompareToReferenceCamera
                                        && (normalizedType == DataType.Cam2 || normalizedType == DataType.Cam3)
                                        && (normalizedSucc == DataType.Cam2 || normalizedSucc == DataType.Cam3))
                                    {
                                        continue;
                                    }

                                    if (HasDataType(candidate, succGroupType))
                                    {
                                        var succTimestamp = GetTimestampForDataType(candidate, succGroupType);
                                        if (succTimestamp.HasValue)
                                        {
                                            // Time difference: successor should be AFTER newFile
                                            var succTimeDiff = (succTimestamp.Value - newGroup.Timestamp).TotalSeconds;
                                            
                                            // Get successor's configured delay range
                                            var succMinDelay = config.DataSequenceSettings.GetMinDelay(normalizedSucc);
                                            var succMaxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedSucc);
                                            
                                            if (succTimeDiff < succMinDelay || succTimeDiff > succMaxDelay)
                                            {
                                                successorValidationPassed = false;
                                                var succFile = GetFileNameForDataTypeInGroup(candidate, succGroupType);
                                                successorRejectionReason =
                                                    $"{normalizedSucc}({succFile}) 시차 범위 밖 (diff={succTimeDiff:F1}s, 허용={succMinDelay}-{succMaxDelay}s, 비교: 신규@{FormatHms(newGroup.Timestamp)} vs {succFile}@{FormatHms(succTimestamp.Value)})";
                                                _logger.LogTrace("Successor validation failed: {Reason}", successorRejectionReason);
                                                break;
                                            }
                                        }
                                    }
                                }
                                
                                if (successorValidationPassed)
                                {
                                    string targetName = hasPred ? predType.ToString() : "Group Anchor";
                                    candidates.Add((candidate, absDiff, hasPred, targetName));
                                }
                                else
                                {
                                    RaiseLog("Log_Info_MatchExcluded_TimeWindow", newGroup.LineNumber, LogSeverity.Info,
                                        colName, fileName, candidate.GroupId, successorRejectionReason);
                                }
                            }
                            else 
                            { 
                                // Logging rejection cause for better traceability
                                string reason = timeDiff < 0
                                    ? "시간 순서 역전"
                                    : BuildTimeWindowRejectionReason(absDiff, minDelay, maxDelay);
                                string compareDetail =
                                    $"비교대상: {candidate.GroupId}/{comparisonSourceType}/{comparisonSourceFile}@{FormatHms(comparisonTimestamp)} (신규@{FormatHms(newGroup.Timestamp)})";
                                _logger.LogTrace("Rejected Candidate {GroupId}: {Reason}", candidate.GroupId, reason);
                                // UI 로그 추가 - 후보 탈락 사유 표시
                                RaiseLog("Log_Info_MatchExcluded_TimeWindow", newGroup.LineNumber, LogSeverity.Info,
                                    colName, fileName, candidate.GroupId, reason, compareDetail);
                            }
                        }

                        // Log only the last "AlreadyHasType" candidate (if any)
                        if (lastAlreadyHasType.HasValue)
                        {
                            var (groupId, anchorFile, anchorTime) = lastAlreadyHasType.Value;
                            RaiseLog("Log_Info_MatchExcluded_AlreadyHasType", newGroup.LineNumber, LogSeverity.Info,
                                colName, fileName, groupId, anchorFile, anchorTime);
                        }
                    }

                    // Select Best Match
                    var bestMatch = candidates
                        .OrderByDescending(c => c.HasPredecessor) // Priority 1
                        .ThenBy(c => ExtractNumericSuffix(c.Group.GroupId))  // Priority 2: FIFO (Numeric)
                        .FirstOrDefault();


                    if (bestMatch.Group != null) 
                    {
                        // ★ 마지막 배정 그룹 업데이트 (라인별 분리)
                        _lastAssignedGroup[(newGroupType, newGroup.LineNumber)] = bestMatch.Group.GroupId;
                        
                        string reason = bestMatch.HasPredecessor ? "순서일치" : "FIFO(우선배정)";
                        RaiseLog("Log_Info_MatchSuccess", newGroup.LineNumber, LogSeverity.Info,
                            colName, fileName, bestMatch.Group.GroupId, reason, bestMatch.TargetName, bestMatch.AbsDiff.ToString("F1"));
                        return bestMatch.Group;
                    }
                    else
                    {
                         double minDelay;
                         double maxDelay;
                         string basis;
                         
                         // Forward matching uses successor type's delay window; backward matching uses new type's delay window.
                         if (predecessorType == null && successorType != null)
                         {
                             var succType = successorType.Value;
                             minDelay = config.DataSequenceSettings.GetMinDelay(succType);
                             maxDelay = config.DataSequenceSettings.GetMaxDelay(succType);
                             basis = $"기준: 후행={succType} / 허용={minDelay}-{maxDelay}s";
                         }
                         else if (predecessorType != null)
                         {
                             minDelay = config.DataSequenceSettings.GetMinDelay(normalizedType);
                             maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedType);
                             basis = $"기준: 선행={predecessorType} / 허용={minDelay}-{maxDelay}s";
                         }
                         else
                         {
                             minDelay = config.DataSequenceSettings.GetMinDelay(normalizedType);
                             maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedType);
                             basis = $"기준: 허용={minDelay}-{maxDelay}s";
                         }
                         RaiseLog("Log_Info_MatchFailed_NoGroup", newGroup.LineNumber, LogSeverity.Info, colName, fileName, basis);
                    }
                }
            }

            return null;
        }

        private static string BuildTimeWindowRejectionReason(double absDiffSeconds, double minDelaySeconds, double maxDelaySeconds)
        {
            if (absDiffSeconds < minDelaySeconds)
                return $"시차 허용 범위 미달 (diff={absDiffSeconds:F1}s < min={minDelaySeconds}s)";
            if (absDiffSeconds > maxDelaySeconds)
                return $"시차 허용 범위 초과 (diff={absDiffSeconds:F1}s > max={maxDelaySeconds}s)";
            return $"시차 허용 범위 밖 (diff={absDiffSeconds:F1}s, 허용={minDelaySeconds}-{maxDelaySeconds}s)";
        }

        private static string FormatHms(DateTime timestampUtcOrLocal)
        {
            if (timestampUtcOrLocal == DateTime.MinValue) return "-";
            return timestampUtcOrLocal.ToString("HHmmss.fff");
        }

        private static string GetAnchorFileName(FileGroup group)
        {
            if (!string.IsNullOrEmpty(group.NormalFolder))
                return Path.GetFileName(group.NormalFolder);
            if (group.HasNir && !string.IsNullOrEmpty(group.NirFilePath))
                return Path.GetFileName(group.NirFilePath);
            if (group.CameraFiles != null && group.CameraFiles.Count > 0)
            {
                // Stable-ish choice for logging: smallest key
                var first = group.CameraFiles.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase).First();
                return Path.GetFileName(first.Value);
            }
            return group.GroupId;
        }

        private static string GetFileNameForDataTypeInGroup(FileGroup group, DataType type)
        {
            try
            {
                return type switch
                {
                    DataType.NIR => Path.GetFileName(group.NirFilePath),
                    DataType.Normal => Path.GetFileName(group.NormalFolder),
                    DataType.Cam1 => group.CameraFiles.TryGetValue("cam1", out var p1) ? Path.GetFileName(p1) : "-",
                    DataType.Cam2 => group.CameraFiles.TryGetValue("cam2", out var p2) ? Path.GetFileName(p2) : "-",
                    DataType.Cam3 => group.CameraFiles.TryGetValue("cam3", out var p3) ? Path.GetFileName(p3) : "-",
                    DataType.Cam4 => group.CameraFiles.TryGetValue("cam4", out var p4) ? Path.GetFileName(p4) : "-",
                    DataType.Cam5 => group.CameraFiles.TryGetValue("cam5", out var p5) ? Path.GetFileName(p5) : "-",
                    DataType.Cam6 => group.CameraFiles.TryGetValue("cam6", out var p6) ? Path.GetFileName(p6) : "-",
                    _ => "-"
                };
            }
            catch
            {
                return "-";
            }
        }

        private bool TryMatchPendingNirToGroup(FileGroup group, ApplicationConfiguration config)
        {
            if (config.DataSequenceSettings == null) return false;
            
            var groupType = DetermineDataTypeForGroup(group);
            var normalizedGroupType = NormalizeForSequence(groupType);
            var minDelay = config.DataSequenceSettings.GetMinDelay(normalizedGroupType);
            var maxDelay = config.DataSequenceSettings.GetMaxDelay(normalizedGroupType);

            lock (_lockObject)
            {
                for (int i = _pendingNirFiles.Count - 1; i >= 0; i--)
                {
                    var pending = _pendingNirFiles[i];
                    if (DetermineLineNumber(pending.Path, FileType.Nir, config) != group.LineNumber) continue;

                    var timeDiff = (group.Timestamp - pending.Timestamp).TotalSeconds;
                    if (timeDiff >= minDelay && timeDiff <= maxDelay)
                    {
                        group.NirFilePath = pending.Path;
                        group.NirKey = _nirFileResolver.GetNirKey(pending.Path);
                        group.HasNir = true;
                        _pendingNirFiles.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        private DataType DetermineDataTypeForGroup(FileGroup group)
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
        private DataType NormalizeForSequence(DataType type)
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
        /// Maps normalized camera types (Cam1-3) to the actual per-line camera types.
        /// Line1: Cam1-3 그대로
        /// Line2: Cam1->Cam4, Cam2->Cam5, Cam3->Cam6
        /// </summary>
        private static DataType MapCameraTypeForLine(DataType normalizedCameraType, int lineNumber)
        {
            if (lineNumber != 2) return normalizedCameraType;

            return normalizedCameraType switch
            {
                DataType.Cam1 => DataType.Cam4,
                DataType.Cam2 => DataType.Cam5,
                DataType.Cam3 => DataType.Cam6,
                _ => normalizedCameraType
            };
        }

        private int GetPriority(DataType type, ApplicationConfiguration config)
        {
            return config.DataSequenceSettings?.GetOrder(type) ?? 0;
        }

        public async Task RemoveFileAsync(string filePath)
        {
            var affectedGroups = _activeGroups.Values
                .Where(g => ContainsFile(g, filePath))
                .ToList();

            foreach (var group in affectedGroups)
            {
                _logger.LogInformation("Removing file from group {GroupId}: {Path}", group.GroupId, filePath);
                RemoveFileFromGroup(group, filePath);

                if (IsGroupEmpty(group))
                {
                    _activeGroups.TryRemove(group.GroupId, out _);
                    GroupRemoved?.Invoke(this, group.GroupId);
                }
                else
                {
                    GroupUpdated?.Invoke(this, group);
                }
            }
            await Task.CompletedTask;
        }

        private bool MergeGroups(FileGroup target, FileGroup source)
        {
            bool changed = false;

            if (source.HasNir && !target.HasNir)
            {
                target.NirFilePath = source.NirFilePath;
                target.NirKey = source.NirKey;
                target.HasNir = true;
                changed = true;
            }

            if (!string.IsNullOrEmpty(source.NormalFolder) && string.IsNullOrEmpty(target.NormalFolder))
            {
                target.NormalFolder = source.NormalFolder;
                target.MainImagePath = source.MainImagePath;
                changed = true;
            }

            if (source.CameraFiles != null)
            {
                foreach (var cam in source.CameraFiles)
                {
                    if (!target.CameraFiles.ContainsKey(cam.Key))
                    {
                        target.CameraFiles[cam.Key] = cam.Value;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private bool ContainsFile(FileGroup group, string filePath)
        {
            if (string.Equals(group.NirFilePath, filePath, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(group.NormalFolder, filePath, StringComparison.OrdinalIgnoreCase)) return true;
            if (group.CameraFiles.Values.Any(v => string.Equals(v, filePath, StringComparison.OrdinalIgnoreCase))) return true;
            
            // Check if it's an image inside the normal folder
            if (!string.IsNullOrEmpty(group.NormalFolder) && filePath.StartsWith(group.NormalFolder, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private void RemoveFileFromGroup(FileGroup group, string filePath)
        {
            if (string.Equals(group.NirFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                group.NirFilePath = string.Empty;
                group.HasNir = false;
            }
            else if (string.Equals(group.NormalFolder, filePath, StringComparison.OrdinalIgnoreCase) || 
                     string.Equals(group.MainImagePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                // If the folder itself or the stitched image is deleted, we treat as normal data removed
                group.NormalFolder = string.Empty;
                group.MainImagePath = string.Empty;
            }
            else
            {
                var camKey = group.CameraFiles.FirstOrDefault(kvp => string.Equals(kvp.Value, filePath, StringComparison.OrdinalIgnoreCase)).Key;
                if (!string.IsNullOrEmpty(camKey))
                {
                    group.CameraFiles.Remove(camKey);
                }
            }
        }

        private bool IsGroupEmpty(FileGroup group)
        {
            return !group.HasNir && 
                   string.IsNullOrEmpty(group.NormalFolder) && 
                   (group.CameraFiles == null || group.CameraFiles.Count == 0);
        }

        private DateTime? GetTimestampForDataType(FileGroup group, DataType type)
        {
            if (type == DataType.NIR && group.HasNir)
            {
               // This requires NirTimestamp to be stored. 
               // Currently FileGroup only has one main Timestamp (Group Timestamp).
               // We might need to resolve it essentially from path or if we store it.
               // Wait, the group structure might not store individual timestamps except implicit via paths.
               // We can re-extract from path using FileNamingHelper.
               return FileNamingHelper.ExtractTimestamp(group.NirFilePath, "Nir");
            }
            if (type == DataType.Normal && !string.IsNullOrEmpty(group.NormalFolder))
            {
                return FileNamingHelper.ExtractTimestamp(group.NormalFolder, "Normal");
            }
            if (type == DataType.Camera)
            {
                // This is ambiguous. Which camera? 
                // DataType enum usually specific like Cam1, Cam2.
                // Let's assume input is specific Cam type.
                return null; 
            }
            
            // Handle specific cams (Cam1-6)
            if (type == DataType.Cam1 || type == DataType.Cam2 || type == DataType.Cam3 ||
                type == DataType.Cam4 || type == DataType.Cam5 || type == DataType.Cam6)
            {
                string key = type.ToString().ToLower(); // cam1, cam2, ..., cam6
                if (group.CameraFiles.TryGetValue(key, out var path))
                {
                    return FileNamingHelper.ExtractTimestamp(path, "Camera");
                }
            }

            return null;
        }

        private string GetFileNameForGroup(FileGroup group, DataType type)
        {
            try
            {
                if (type == DataType.NIR) return Path.GetFileName(group.NirFilePath);
                if (type == DataType.Normal) return Path.GetFileName(group.NormalFolder); // Folder name
                if (group.CameraFiles != null && group.CameraFiles.Count > 0)
                {
                    // Return the first camera file name found
                    using var enumerator = group.CameraFiles.Values.GetEnumerator();
                    if (enumerator.MoveNext())
                        return Path.GetFileName(enumerator.Current);
                }
                return type.ToString();
            }
            catch
            {
                return type.ToString();
            }
        }

        private bool HasDataType(FileGroup group, DataType type)
        {
            return type switch
            {
                DataType.NIR => group.HasNir,
                DataType.Normal => !string.IsNullOrEmpty(group.NormalFolder),
                DataType.Cam1 => group.CameraFiles.ContainsKey("cam1"),
                DataType.Cam2 => group.CameraFiles.ContainsKey("cam2"),
                DataType.Cam3 => group.CameraFiles.ContainsKey("cam3"),
                DataType.Cam4 => group.CameraFiles.ContainsKey("cam4"),
                DataType.Cam5 => group.CameraFiles.ContainsKey("cam5"),
                DataType.Cam6 => group.CameraFiles.ContainsKey("cam6"),
                DataType.Camera => group.CameraFiles.Count > 0,
                _ => false
            };
        }

        private string GetFriendlyColumnName(DataType type, int lineNumber)
        {
            return type switch
            {
                DataType.Normal => $"normal{lineNumber}",
                DataType.NIR => $"nir{lineNumber}",
                DataType.Cam1 => "Cam1",
                DataType.Cam2 => "Cam2",
                DataType.Cam3 => "Cam3",
                DataType.Cam4 => "Cam4",
                DataType.Cam5 => "Cam5",
                DataType.Cam6 => "Cam6",
                DataType.Camera => "Camera",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// GroupId 비교 (숫자 기준)
        /// group_023 vs group_024 → -1 (023이 작음)
        /// line1_023 vs line1_024 → -1 (023이 작음)
        /// </summary>
        private static int CompareGroupId(string a, string b)
        {
            // Extract numeric suffix from group IDs (supports: group_XXX, line1_XXX, line2_XXX)
            int numA = ExtractNumericSuffix(a);
            int numB = ExtractNumericSuffix(b);
            return numA.CompareTo(numB);
        }

        /// <summary>
        /// Extract the numeric suffix from a group ID
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
    }
}
