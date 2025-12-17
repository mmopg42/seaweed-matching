# Initial Scan Matching Debug - Design

## 1. Logging Strategy

### 1.1 Log Levels
- **Info**: High-level execution flow (which scan type, match results)
- **Debug**: Detailed matching logic (each candidate check, timestamp extraction)
- **Warning**: Configuration issues, missing data

### 1.2 Log Locations

#### Phase 1: Configuration Check
**File**: `MonitoringOrchestrator.cs` - `PerformInitialScanAsync`
**Purpose**: Determine why Sequential scan not executing

```csharp
_logger.LogInformation("=== INITIAL SCAN START ===");
_logger.LogInformation("DataSequenceSettings: {Status}", 
    _currentConfig?.DataSequenceSettings == null ? "NULL (using legacy batch)" : 
    $"LOADED ({_currentConfig.DataSequenceSettings.Sequence.Count} items in sequence)");

if (_currentConfig?.DataSequenceSettings != null)
{
    var seqStr = string.Join(" → ", _currentConfig.DataSequenceSettings.Sequence
        .OrderBy(x => x.Order)
        .Select(x => $"{x.Type}[{x.Order}](min={x.MinDelaySeconds}s,max={x.MaxDelaySeconds}s,enabled={x.Enabled})"));
    _logger.LogInformation("Sequence order: {Sequence}", seqStr);
}
```

#### Phase 2: Timestamp Extraction
**File**: `MonitoringOrchestrator.cs` - `CreateUnmatchedFilesForSingleFile`
**Purpose**: Verify timestamps are extracted correctly

```csharp
// After timestamp extraction
if (timestamp == null)
{
    _logger.LogWarning("TIMESTAMP FAILED: {FileType} file={Path}", fileType, filePath);
}
else
{
    _logger.LogDebug("TIMESTAMP OK: {FileType} file={FileName} → {Timestamp:yyyy-MM-dd HH:mm:ss}", 
        fileType, Path.GetFileName(filePath), timestamp.Value);
}
```

#### Phase 3: Match 3 Logic
**File**: `MonitoringOrchestrator.cs` - `FindMatchingExistingGroup`
**Purpose**: See exactly why matches fail/succeed

```csharp
// At method start
_logger.LogInformation("Match 3: Check {DataType} file ts={Timestamp:yyyy-MM-dd HH:mm:ss}", 
    dataType, newGroup.Timestamp);

// For each candidate
foreach (var candidate in candidateGroups)
{
    var timeDiff = (newGroup.Timestamp - candidate.Timestamp).TotalSeconds;
    var minDelay = _currentConfig.DataSequenceSettings.GetMinDelay(dataType);
    var maxDelay = _currentConfig.DataSequenceSettings.GetMaxDelay(dataType);
    
    var withinRange = timeDiff >= minDelay && timeDiff <= maxDelay;
    var temporalOk = newGroup.Timestamp >= candidate.Timestamp; // simplified check
    
    _logger.LogDebug("  vs group_{Id} ({Type}): candidateTs={CandidateTs:HH:mm:ss}, " +
                     "diff={Diff:F1}s, range=[{Min},{Max}], temporal={TOk}, withinRange={ROk}",
        candidate.GroupId, 
        GetPrimaryDataType(candidate),
        candidate.Timestamp,
        timeDiff,
        minDelay,
        maxDelay,
        temporalOk,
        withinRange);
}

// Match result
if (matchedGroup != null)
{
    _logger.LogInformation("Match 3: ✓ MATCHED to group_{Id}", matchedGroup.GroupId);
}
else
{
    _logger.LogInformation("Match 3: ✗ NO MATCH - creating new group");
}
```

#### Phase 4: Sequential Scan Progress
**File**: `MonitoringOrchestrator.cs` - `PerformSequentialInitialScanAsync`
**Purpose**: Track scan progress

```csharp
// Per data type
_logger.LogInformation("Processing {DataType}: Found {Count} files", dataType, files.Count);

// Per file
_logger.LogDebug("  [{Index}/{Total}] Processing: {FileName}", 
    currentIndex, files.Count, Path.GetFileName(filePath));
```

## 2. Helper Methods

### 2.1 GetPrimaryDataType
```csharp
private DataType GetPrimaryDataType(FileGroup group)
{
    if (group.HasNir) return DataType.NIR;
    if (!string.IsNullOrEmpty(group.NormalFolder)) return DataType.Normal;
    if (group.CameraFiles.Any()) return DataType.Cam1; // simplified
    return DataType.NIR; // fallback
}
```

## 3. Configuration Diagnosis

### 3.1 Check appsettings.json
**Location**: `bin/Debug/net10.0-windows/appsettings.json`
**Expected**:
```json
{
  "DataSequenceSettings": {
    "Sequence": [
      {
        "Type": "NIR",
        "Order": 1,
        "MinDelaySeconds": 0,
        "MaxDelaySeconds": 0,
        "Enabled": true
      },
      // ... more items
    ]
  }
}
```

### 3.2 Check Configuration Loading
**File**: `MonitoringOrchestrator.cs` - Constructor
```csharp
_logger.LogInformation("MonitoringOrchestrator created. Config status: {Status}",
    _currentConfig == null ? "NULL" : 
    _currentConfig.DataSequenceSettings == null ? "No DataSequenceSettings" : "OK");
```

## 4. Testing Plan

1. **Add all logs**
2. **Build and run**
3. **Check logs for**:
   - "DataSequenceSettings: NULL" → Configuration not loaded
   - "DataSequenceSettings: LOADED" but no Sequential scan → Bug in condition check
   - "TIMESTAMP FAILED" → Timestamp extraction broken
   - "Match 3: NO MATCH" with reasons → Range/temporal order issue

---
**Status**: [ ] Ready for implementation
**Next Step**: 03_tasks.md
