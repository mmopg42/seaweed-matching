using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChronoView.Core.Nir;
using ChronoView.Helpers;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    public class GroupManager : IGroupManager
    {
        private readonly ILogger<GroupManager> _logger;
        private readonly INirFileResolver _nirFileResolver;
        private readonly ConcurrentDictionary<string, FileGroup> _activeGroups = new();
        private int _nextGroupId = 1;
        private readonly List<(string Path, DateTime Timestamp, DateTime AddedAt)> _pendingNirFiles = new();
        private readonly object _lockObject = new();
        private readonly ITimestampCache _folderTimestamps;
        private readonly HashSet<string> _processedFiles = new(StringComparer.OrdinalIgnoreCase);
        // 각 DataType별 마지막 배정 그룹 추적 (컬럼 내 순서성 보장)
        private readonly Dictionary<DataType, string> _lastAssignedGroup = new();

        public event EventHandler<FileGroup>? GroupCreated;
        public event EventHandler<FileGroup>? GroupUpdated;
        public event EventHandler<string>? GroupRemoved;
        public event Action<string>? Log; // UI Logging Event

        public IEnumerable<FileGroup> ActiveGroups => _activeGroups.Values;

        public GroupManager(ILogger<GroupManager> logger, INirFileResolver nirFileResolver, ITimestampCache folderTimestamps)
        {
            _logger = logger;
            _nirFileResolver = nirFileResolver;
            _folderTimestamps = folderTimestamps;
        }

        private void RaiseLog(string message)
        {
            Log?.Invoke(message);
        }

        public FileGroup? FindGroupById(string groupId)
        {
            return _activeGroups.TryGetValue(groupId, out var group) ? group : null;
        }

        public int GetActiveGroupsCount() => _activeGroups.Count;

        public async Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config)
        {
            try
            {
                string processPath = filePath;
                
                // Duplicate fix: If file already processed, ignore.
                lock (_lockObject)
                {
                    if (_processedFiles.Contains(filePath))
                    {
                        _logger.LogDebug("Common: File already processed, skipping: {Path}", filePath);
                        return null;
                    }
                    else
                    {
                         _processedFiles.Add(filePath);
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
                        var currentId = Interlocked.Increment(ref _nextGroupId) - 1;
                        var newGroupId = $"group_{currentId:D3}";
                        newGroupTemplate.GroupId = newGroupId;

                        if (!_activeGroups.TryAdd(newGroupTemplate.GroupId, newGroupTemplate))
                        {
                            currentId = Interlocked.Increment(ref _nextGroupId) - 1;
                            newGroupId = $"group_{currentId:D3}";
                            newGroupTemplate.GroupId = newGroupId;
                            _activeGroups.TryAdd(newGroupTemplate.GroupId, newGroupTemplate);
                        }
                        
                        // ★ 새 그룹 생성 시 마지막 배정 그룹 업데이트
                        _lastAssignedGroup[newGroupType] = newGroupTemplate.GroupId;
                        
                        RaiseLog($"[새 그룹 생성] {colName}: {fileName} -> {newGroupTemplate.GroupId}");
                        targetGroup = newGroupTemplate;
                        isNew = true;
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

                // Match pending NIR
                if (!targetGroup.HasNir && fileType != FileType.Nir)
                {
                    if (TryMatchPendingNirToGroup(targetGroup, config))
                    {
                        GroupUpdated?.Invoke(this, targetGroup);
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
            Interlocked.Exchange(ref _nextGroupId, 1);
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
                    group.MainImagePath = Path.Combine(filePath, "stitched_original.png");
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
            if (fileType == FileType.Normal)
            {
                string folderName = Path.GetFileName(filePath);
                var suffixMatch = System.Text.RegularExpressions.Regex.Match(folderName, @"_(\d+)$");
                if (suffixMatch.Success)
                {
                    int suffix = int.Parse(suffixMatch.Groups[1].Value);
                    return suffix == 1 ? 2 : 1;
                }
            }

            if ((!string.IsNullOrEmpty(config.MatchingSettings.Nir2Path) && filePath.StartsWith(config.MatchingSettings.Nir2Path, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(config.MatchingSettings.Normal2Path) && filePath.StartsWith(config.MatchingSettings.Normal2Path, StringComparison.OrdinalIgnoreCase)))
            {
                return 2;
            }

            for (int i = 4; i <= 6; i++)
            {
                var camPath = config.MatchingSettings.GetCameraPath(i);
                if (!string.IsNullOrEmpty(camPath) && filePath.StartsWith(camPath, StringComparison.OrdinalIgnoreCase)) return 2;
            }

            return 1;
        }

        private FileGroup? FindMatchingExistingGroup(FileGroup newGroup, ApplicationConfiguration config)
        {
            var snapshot = _activeGroups.Values.ToArray();

            // Match 1: By NormalFolder
            if (!string.IsNullOrEmpty(newGroup.NormalFolder))
            {
                var match = snapshot.FirstOrDefault(g => g.NormalFolder == newGroup.NormalFolder);
                if (match != null) return match;
            }

            // Match 2: By NirKey
            if (!string.IsNullOrEmpty(newGroup.NirKey))
            {
                var match = snapshot.FirstOrDefault(g => g.NirKey == newGroup.NirKey);
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
                    var newGroupOrder = GetPriority(newGroupType, config);

                    DataType? predecessorType = null;
                    int predecessorOrder = -1;
                    DataType? successorType = null;
                    int successorOrder = -1;

                    foreach (var type in orderedTypes)
                    {
                        var order = GetPriority(type, config);
                        if (order < newGroupOrder && order > predecessorOrder)
                        {
                            predecessorType = type;
                            predecessorOrder = order;
                        }
                        if (order > newGroupOrder && (successorOrder == -1 || order < successorOrder))
                        {
                            successorType = type;
                            successorOrder = order;
                        }
                    }

                    var candidates = new List<(FileGroup Group, double AbsDiff, bool HasPredecessor, string TargetName)>();
                    string colName = GetFriendlyColumnName(newGroupType, newGroup.LineNumber);
                    string fileName = GetFileNameForGroup(newGroup, newGroupType);

                    // Forward Matching (Checking against Successor)
                    if (predecessorType == null && successorType != null)
                    {
                        var succType = successorType.Value;
                        var minDelay = config.DataSequenceSettings.GetMinDelay(succType);
                        var maxDelay = config.DataSequenceSettings.GetMaxDelay(succType);

                        foreach (var candidate in snapshot.Where(g => g.LineNumber == newGroup.LineNumber))
                        {
                            if (HasDataType(candidate, newGroupType)) continue;
                            var timeDiff = (candidate.Timestamp - newGroup.Timestamp).TotalSeconds;
                             
                            _logger.LogTrace("Checking Fwd Candidate {GroupId}: TimeDiff={Diff}s (Range: {Min}-{Max})", candidate.GroupId, timeDiff, minDelay, maxDelay);

                            if (timeDiff >= minDelay && timeDiff <= maxDelay)
                            {
                                candidates.Add((candidate, Math.Abs(timeDiff), false, succType.ToString()));
                            }
                        }
                    }
                    // Backward Matching (Checking against Predecessor)
                    else if (predecessorType != null)
                    {
                        var predType = predecessorType.Value;
                        var minDelay = config.DataSequenceSettings.GetMinDelay(newGroupType);
                        var maxDelay = config.DataSequenceSettings.GetMaxDelay(newGroupType);

                        _logger.LogTrace("Matching {Type} (Order {Order}) - Looking for Predecessor {PredType} in stored groups. Time Window: {Min}-{Max}s", 
                            newGroupType, newGroupOrder, predType, minDelay, maxDelay);

                        foreach (var candidate in snapshot.Where(g => g.LineNumber == newGroup.LineNumber))
                        {
                            if (HasDataType(candidate, newGroupType)) continue;
                            
                            // ★ 컬럼 내 순서성 체크: 마지막 배정 그룹보다 이후 그룹만 허용
                            if (_lastAssignedGroup.TryGetValue(newGroupType, out var lastGroupId))
                            {
                                if (CompareGroupId(candidate.GroupId, lastGroupId) <= 0)
                                {
                                    _logger.LogTrace("Skipping {GroupId} for {Type}: must be after {LastGroup}",
                                        candidate.GroupId, newGroupType, lastGroupId);
                                    // UI 로그 추가 - 컬럼 내 순서성 위반
                                    RaiseLog($"[매칭제외] {colName}: {fileName} -> {candidate.GroupId} 제외 (사유: 컬럼순서 위반, 마지막배정: {lastGroupId})");
                                    continue;
                                }
                            }
                            
                            DateTime comparisonTimestamp = candidate.Timestamp; // Default to group anchor
                            
                            bool hasPred;
                            if (predType == DataType.NIR)
                            {
                                hasPred = true; // User feedback: Relax NIR check
                                if (candidate.HasNir) comparisonTimestamp = candidate.Timestamp; 
                            }
                            else
                            {
                                hasPred = HasDataType(candidate, predType);
                                if (hasPred)
                                {
                                    var predTimestamp = GetTimestampForDataType(candidate, predType);
                                    if (predTimestamp.HasValue) comparisonTimestamp = predTimestamp.Value;
                                }
                            }

                            var timeDiff = (newGroup.Timestamp - comparisonTimestamp).TotalSeconds;
                            bool skipOrdering = (predType == DataType.NIR && !HasDataType(candidate, DataType.NIR));
                            
                            if (timeDiff < 0 && !skipOrdering) continue;

                            var absDiff = Math.Abs(timeDiff);
                            
                            _logger.LogTrace("Candidate {GroupId}: Diff={Diff:F2}s, HasPred={HasPred} ({PredType})", candidate.GroupId, timeDiff, hasPred, predType);

                            // Strict tolerance check
                            if (absDiff >= minDelay && absDiff <= maxDelay) 
                            {
                                string targetName = hasPred ? predType.ToString() : "Group Anchor";
                                candidates.Add((candidate, absDiff, hasPred, targetName));
                            }
                            else 
                            { 
                                // Logging rejection cause for better traceability
                                string reason = timeDiff < 0 ? "시간 순서 역전" : $"시차 허용 범위 초과 ({absDiff:F1}s > {maxDelay}s)";
                                _logger.LogTrace("Rejected Candidate {GroupId}: {Reason}", candidate.GroupId, reason);
                                // UI 로그 추가 - 후보 탈락 사유 표시
                                RaiseLog($"[매칭제외] {colName}: {fileName} -> {candidate.GroupId} 제외 (사유: {reason})");
                            }
                        }
                    }

                    // Select Best Match
                    var bestMatch = candidates
                        .OrderByDescending(c => c.HasPredecessor) // Priority 1
                        .ThenBy(c => c.Group.GroupId)             // Priority 2: FIFO (Earlier Group)
                        .FirstOrDefault();


                    if (bestMatch.Group != null) 
                    {
                        // ★ 마지막 배정 그룹 업데이트
                        _lastAssignedGroup[newGroupType] = bestMatch.Group.GroupId;
                        
                        string reason = bestMatch.HasPredecessor ? "순서일치" : "FIFO(우선배정)";
                        RaiseLog($"[매칭성공] {colName}: {fileName} -> {bestMatch.Group.GroupId} (사유: {reason}, 대상: {bestMatch.TargetName}, 시차: {bestMatch.AbsDiff:F1}초)");
                        return bestMatch.Group;
                    }
                    else
                    {
                         RaiseLog($"[매칭실패] {colName}: {fileName} -> 배정 실패 (사유: 조건에 맞는 그룹 없음, 시차제한: {config.DataSequenceSettings.GetMaxDelay(newGroupType)}s)");
                    }
                }
            }

            return null;
        }

        private bool TryMatchPendingNirToGroup(FileGroup group, ApplicationConfiguration config)
        {
            if (config.DataSequenceSettings == null) return false;
            
            var groupType = DetermineDataTypeForGroup(group);
            var minDelay = config.DataSequenceSettings.GetMinDelay(groupType);
            var maxDelay = config.DataSequenceSettings.GetMaxDelay(groupType);

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
            }
            return DataType.Camera;
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
            
            // Handle specific cams
            if (type == DataType.Cam1 || type == DataType.Cam2 || type == DataType.Cam3)
            {
                string key = type.ToString().ToLower(); // cam1, cam2...
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
                DataType.Camera => group.CameraFiles.Count > 0,
                _ => false
            };
        }

        private string GetFriendlyColumnName(DataType type, int lineNumber)
        {
            return type switch
            {
                DataType.Normal => "일반",
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
        /// </summary>
        private static int CompareGroupId(string a, string b)
        {
            int numA = int.Parse(a.Replace("group_", ""));
            int numB = int.Parse(b.Replace("group_", ""));
            return numA.CompareTo(numB);
        }
    }
}
