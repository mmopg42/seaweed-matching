# Initial Scan Matching Debug - Requirements

## 1. Goal

### Primary Goal
Add comprehensive debugging logs to diagnose why Match 3 logic is not being executed during initial scan, causing files to not be grouped correctly.

### Success Criteria
- [ ] Can see in logs whether Sequential scan or Legacy batch scan is being used
- [ ] Can see extracted timestamps for each file (Normal, NIR, Camera)
- [ ] Can see time difference calculations during Match 3 logic
- [ ] Can see why files are/aren't matching (within range check results)
- [ ] Can identify root cause of DataSequenceSettings not being loaded

## 2. Current Problem

**Symptoms**:
- All files are in separate groups (NIR alone, each Normal alone, each Camera alone)
- No "Sequential scan" logs appearing
- No Match 3 range check logs appearing

**Expected Behavior**:
- NIR (14:05:42) + Normal (14:05:43) + Cameras should be in same group
- Sequential scan should execute with Match 3 logic
- Timestamps should be extracted and compared

## 3. Investigation Areas

### 3.1 Configuration Loading
- Is `_currentConfig.DataSequenceSettings` null?
- Is `appsettings.json` being loaded correctly?
- Are settings being saved to correct location?

### 3.2 Timestamp Extraction
- Are Normal folder timestamps extracted? (`C251201T140543_0` → `2025-12-01 14:05:43`)
- Are Camera file timestamps extracted? (`20251201_140549.bmp` → `2025-12-01 14:05:49`)
- Are NIR timestamps extracted from filename?

### 3.3 Match 3 Logic
- Is `FindMatchingExistingGroup` being called?
- Are MinDelay/MaxDelay values correct?
- Is temporal ordering check working correctly?

## 4. Required Debug Logging

### 4.1 MonitoringOrchestrator.PerformInitialScanAsync
```csharp
// Log which scan type is selected
_logger.LogInformation("DataSequenceSettings: {IsNull}", _currentConfig?.DataSequenceSettings == null ? "NULL" : "LOADED");
```

### 4.2 MonitoringOrchestrator.PerformSequentialInitialScanAsync
```csharp
// Log scan start with sequence order
_logger.LogInformation("Sequential scan - Order: {Order}", 
    string.Join(" → ", sequence.Select(x => $"{x.Type}(min={x.MinDelaySeconds},max={x.MaxDelaySeconds})")));

// Log each file processing
_logger.LogInformation("Processing {DataType} file: {Path}", dataType, filePath);
```

### 4.3 CreateNewGroupForInitialScanAsync
```csharp
// Log timestamp extraction
_logger.LogInformation("Extracted timestamp {Timestamp} from {DataType} file: {Path}", 
    timestamp, dataType, filePath);
```

### 4.4 FindMatchingExistingGroup (Match 3)
```csharp
// Log Match 3 attempt
_logger.LogInformation("Match 3: Checking {DataType} ts={Timestamp} against {Count} existing groups", 
    dataType, newGroup.Timestamp, candidateGroups.Count);

// Log each candidate check
_logger.LogDebug("  Candidate group_{Id}: ts={GroupTs}, diff={Diff}s, range=[{Min},{Max}], temporal={TemporalOk}, withinRange={RangeOk}",
    candidateGroup.GroupId, candidateGroup.Timestamp, timeDiff, minDelay, maxDelay, temporalOrderOk, withinRange);

// Log match result
if (match found)
    _logger.LogInformation("Match 3: MATCHED to group_{Id}", matchedGroup.GroupId);
else
    _logger.LogInformation("Match 3: NO MATCH - creating new group");
```

## 5. Implementation Steps

1. Add debug logs to configuration loading
2. Add debug logs to timestamp extraction
3. Add debug logs to Match 3 logic
4. Test with existing data
5. Analyze logs to find root cause

---
**Status**: [ ] Ready for implementation
**Next Step**: 02_design.md
