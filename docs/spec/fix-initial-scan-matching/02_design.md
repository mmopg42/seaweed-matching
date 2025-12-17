# Fix Initial Scan & Real-time Scan Matching - Design

---
Task: Fix Initial Scan & Real-time Scan Matching
Created: 2025-12-15
Status: Draft
Depends On: 01_requirements.md
---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ MonitoringOrchestrator.StartAsync()                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ PHASE 1: Initial Scan (Existing Files)              │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ 1. Get DataSequenceSettings.GetOrderedTypes()       │   │
│  │ 2. For each dataType in order:                      │   │
│  │    - Scan files of that type from disk             │   │
│  │    - If FIRST priority: CreateNewGroupAsync()      │   │
│  │    - If LATER priority: CreateOrUpdateGroupAsync() │   │
│  │      → FindMatchingExistingGroup()                  │   │
│  │      → Match 3 logic ✓                              │   │
│  └─────────────────────────────────────────────────────┘   │
│                          ↓                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ PHASE 2: Real-time Scan (New Files)                 │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ 1. FileWatcher.StartAsync()                         │   │
│  │ 2. Subscribe to FileChanged events                  │   │
│  │ 3. New file detected:                               │   │
│  │    → CreateOrUpdateGroupAsync(filePath)             │   │
│  │    → FindMatchingExistingGroup()                    │   │
│  │    → Match 3 logic ✓                                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 2. Component Design

### 2.1 PerformInitialScanAsync (Refactored)

**Goal**: Process existing files sequentially using Match 3 logic.

**Pseudo-code**:
```csharp
public async Task<OrchestrationResult> PerformInitialScanAsync(CancellationToken ct)
{
    _logger.LogInformation("Starting initial scan with sequential matching");
    
    // Step 1: Get ordered types
    var orderedTypes = _currentConfig?.DataSequenceSettings?.GetOrderedTypes();
    if (orderedTypes == null || orderedTypes.Count == 0)
    {
        _logger.LogWarning("No ordered types configured, using legacy matching");
        return await PerformLegacyInitialScan(ct); // Fallback
    }
    
    // Step 2: Process each type in sequence
    foreach (var dataType in orderedTypes)
    {
        ct.ThrowIfCancellationRequested();
        
        _logger.LogDebug("Processing {DataType} files (Priority {Order})", 
            dataType, GetPriority(dataType));
        
        // Step 3: Scan files
        var files = await ScanFilesForDataType(dataType, ct);
        
        // Step 4: Process each file
        foreach (var file in files)
        {
            // First priority type: Create new groups
            if (IsFirstPriorityType(dataType, orderedTypes))
            {
                await CreateNewGroupAsync(file, dataType, ct);
            }
            // Later types: Try to match with existing groups
            else
            {
                await CreateOrUpdateGroupAsync(file, dataType, ct);
                // ↑ This calls FindMatchingExistingGroup() → Match 3
            }
        }
    }
    
    _logger.LogInformation("Initial scan complete: {GroupCount} groups, {FileCount} files",
        _activeGroups.Count, _processedFiles.Count);
    
    return new OrchestrationResult { Success = true };
}
```

**Helper: ScanFilesForDataType**:
```csharp
private async Task<List<string>> ScanFilesForDataType(DataType dataType, CancellationToken ct)
{
    var config = _currentConfig?.MatchingSettings;
    var files = new List<string>();
    
    switch (dataType)
    {
        case DataType.NIR:
            if (!string.IsNullOrEmpty(config.Nir1Path))
                files.AddRange(Directory.GetFiles(config.Nir1Path, "*.txt", SearchOption.AllDirectories));
            if (!string.IsNullOrEmpty(config.Nir2Path))
                files.AddRange(Directory.GetFiles(config.Nir2Path, "*.txt", SearchOption.AllDirectories));
            break;
            
        case DataType.Normal:
            if (!string.IsNullOrEmpty(config.Normal1Path))
                files.AddRange(Directory.GetDirectories(config.Normal1Path));
            if (!string.IsNullOrEmpty(config.Normal2Path))
                files.AddRange(Directory.GetDirectories(config.Normal2Path));
            break;
            
        case DataType.Cam1:
            if (!string.IsNullOrEmpty(config.Camera1Path))
                files.AddRange(Directory.GetFiles(config.Camera1Path, "*.*", SearchOption.TopDirectoryOnly));
            break;
            
        // ... Cam2-6 similar
    }
    
    return files;
}
```

**Helper: CreateNewGroupAsync**:
```csharp
private async Task CreateNewGroupAsync(string filePath, DataType dataType, CancellationToken ct)
{
    var group = new FileGroup
    {
        GroupId = $"group_{_nextGroupId++:D3}",
        Timestamp = ExtractTimestamp(filePath, dataType),
        LineNumber = DetermineLineNumber(filePath, dataType)
    };
    
    // Add file to group based on type
    AddFileToGroup(group, filePath, dataType);
    
    // Add to active groups
    lock (_lockObject)
    {
        _activeGroups[group.GroupId] = group;
    }
    
    OnGroupCreated(group);
    
    _logger.LogDebug("Created new group {GroupId} for {DataType}", group.GroupId, dataType);
}
```

### 2.2 Real-time Scan Verification

**Ensure FileWatcher is Started**:
```csharp
public async Task StartAsync(ApplicationConfiguration config)
{
    _currentConfig = config;
    
    // Phase 1: Initial scan
    var result = await PerformInitialScanAsync();
    
    // Phase 2: Start real-time monitoring
    await _fileWatcher.StartAsync(config.MatchingSettings);
    
    // Phase 3: Subscribe to events (if not already subscribed)
    _fileWatcher.FileChanged += OnFileWatcherFileChanged;
    
    _isMonitoring = true;
    _logger.LogInformation("Monitoring started: Initial scan + Real-time watcher active");
}
```

**Event Handler**:
```csharp
private async void OnFileWatcherFileChanged(object? sender, FileSystemEventArgs e)
{
    _logger.LogInformation("File detected: {Path}", e.FullPath);
    
    try
    {
        // Determine data type
        var dataType = DetermineDataTypeFromPath(e.FullPath);
        
        // Call CreateOrUpdateGroupAsync → Uses Match 3
        await CreateOrUpdateGroupAsync(e.FullPath, dataType, CancellationToken.None);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing file change: {Path}", e.FullPath);
    }
}
```

### 2.3 Diagnostic Logging

**Add to FindMatchingExistingGroup** (already done in Phase 4):
```csharp
// Match 1
_logger.LogInformation("Found match by NormalFolder: {GroupId}", match.GroupId);

// Match 2
_logger.LogInformation("Found match by NirKey: {GroupId}", match.GroupId);

// Match 3
_logger.LogDebug("Match 3: Searching for timestamp match. NewGroup Type={Type}, Order={Order}",
    newGroupType, newGroupOrder);
_logger.LogInformation("Match 3: Found timestamp match! GroupId={GroupId}, Δ={Delta:F1}s",
    closest.GroupId, timeDelta);
```

**Add to PerformInitialScanAsync**:
```csharp
_logger.LogInformation("Starting initial scan with sequential matching");
_logger.LogDebug("Processing {DataType} files (Priority {Order})", dataType, priority);
_logger.LogInformation("Initial scan complete: {GroupCount} groups, {FileCount} files",
    _activeGroups.Count, _processedFiles.Count);
```

## 3. Verification Plan

### 3.1 Initial Scan Test

**Setup**:
```
Folder structure:
Z:\nir\run_120251201T140000A.txt  (NIR, T=14:00:00)
Z:\cam1\20251201_140001.jpg       (Cam1, T=14:00:01)
```

**Expected Logs**:
```
INFO: Starting initial scan with sequential matching
DEBUG: Processing NIR files (Priority 1)
DEBUG: Created new group group_001 for NIR
DEBUG: Processing Cam1 files (Priority 3)
DEBUG: FindMatchingExistingGroup: NormalFolder=null, NirKey=null
DEBUG: Match 3: Searching for timestamp match. NewGroup Type=Cam1, Order=3
INFO: Match 3: Found timestamp match! GroupId=group_001, Δ=1.0s
INFO: Initial scan complete: 1 groups, 2 files
```

### 3.2 Real-time Scan Test

**Setup**:
1. Start app with empty folders
2. Create NIR file
3. Wait 1 second
4. Create Cam1 file

**Expected Logs**:
```
INFO: Monitoring started: Initial scan + Real-time watcher active
INFO: File detected: Z:\nir\run_120251201T140000A.txt
DEBUG: Created new group group_001 for NIR
INFO: File detected: Z:\cam1\20251201_140001.jpg
DEBUG: FindMatchingExistingGroup: ...
INFO: Match 3: Found timestamp match! GroupId=group_001, Δ=1.0s
```

## 4. Breaking Changes

None - this is a fix that improves existing behavior.

---

**Status**: Ready for implementation
**Next Step**: Create tasks.md
