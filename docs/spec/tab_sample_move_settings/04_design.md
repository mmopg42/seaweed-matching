---
Task: tab_sample_move_settings
Created: 2026-01-08
Status: Draft
Depends On: 03_plan.md
---

# Tab-Specific Sample Move Settings - Detailed Design

## 1. Component Designs

### 1.1 MainWindowViewModel

#### Interface (Modified)

```csharp
// Layout properties
public bool IsLine1Tab { get; }
public bool IsLine2Tab { get; }
public bool IsCombinedTab { get; }
public string SampleMoveSettingsHeader { get; }

// Line 1 Settings (Two-way binding)
public string Line1SampleName { get; set; }
public string Line1MoveNir { get; set; }
public string Line1MoveAllData { get; set; }

// Line 2 Settings (Two-way binding)
public string Line2SampleName { get; set; }
public string Line2MoveNir { get; set; }
public string Line2MoveAllData { get; set; }
```

#### Detailed Logic: SaveLineSettingsAsync

```pseudo
function SaveLineSettingsAsync(lineNumber):
    lock (_saveLock):
        _saveDebounceTimer.Stop()
        _saveDebounceTimer = new Timer(500ms)
        _saveDebounceTimer.Tick += async (s, e) => 
            _saveDebounceTimer.Stop()
            
            // Load fresh config
            config = await _configManager.Load()
            
            // Map ViewModel -> Config (Line 1)
            config.MatchingSettings.Line1Settings.SampleName = Line1SampleName
            config.MatchingSettings.Line1Settings.MoveNir = ParseInt(Line1MoveNir)
            config.MatchingSettings.Line1Settings.MoveAllData = ParseInt(Line1MoveAllData)
            
            // Map ViewModel -> Config (Line 2)
            config.MatchingSettings.Line2Settings = ... (same pattern)
            
            await _configManager.Save(config)
        
        _saveDebounceTimer.Start()
```

#### Detailed Logic: ExecuteMoveWithConfirmation

```pseudo
function ExecuteMoveWithConfirmation():
    if IsMonitoring:
        // ... warning logic ...
    
    if IsLine1Tab:
        ExecuteMoveForLine(1, Line1SampleName, Line1MoveNir, Line1MoveAllData, Dashboard.Line1Groups)
        
    else if IsLine2Tab:
        ExecuteMoveForLine(2, ... same pattern ...)
        
    else if IsCombinedTab:
        // 1. Build summary message
        msg = "Line 1: " + Line1SampleName + ...
        msg += "Line 2: " + Line2SampleName + ...
        
        // 2. Confirm
        if Confirm(msg):
            await ExecuteMoveForLine(1, ...)
            await ExecuteMoveForLine(2, ...)
            
            // Refresh
            RefreshData()

#### Detailed Logic: ExecuteDeleteWithConfirmation

```pseudo
function ExecuteDeleteWithConfirmation():
    // Standard monitoring checks...
    
    if IsLine1Tab:
        // Use Line 1 settings and groups
        ExecuteDelete(Dashboard.Line1Groups, Line1SampleName)
        
    else if IsLine2Tab:
        // Use Line 2 settings and groups
        ExecuteDelete(Dashboard.Line2Groups, Line2SampleName)
        
    else if IsCombinedTab:
        // Combined sequential execution
        msg = "Delete selected files from BOTH lines?"
        if Confirm(msg):
             // Important: Must use correct subject for each batch to ensure quarantine path is correct
            await ExecuteDelete(Dashboard.Line1Groups, Line1SampleName)
            await ExecuteDelete(Dashboard.Line2Groups, Line2SampleName)
            
            RefreshData()
```

```

---

### 1.2 SampleMoveSettingsTemplateSelector

#### Interface

```csharp
public class SampleMoveSettingsTemplateSelector : DataTemplateSelector
{
    public DataTemplate Line1Template { get; set; }
    public DataTemplate Line2Template { get; set; }
    public DataTemplate CombinedTemplate { get; set; }
    
    public override DataTemplate SelectTemplate(object item, DependencyObject container);
}
```

#### Detailed Logic

```pseudo
function SelectTemplate(item, container):
    if item is MainWindowViewModel vm:
        switch vm.ActiveTabIndex:
            case 0: return Line1Template
            case 1: return Line2Template
            case 2: return CombinedTemplate
            default: return Line1Template
            
    return base.SelectTemplate(item, container)
```

---

## 2. Integration Points

### 2.1 ViewModel → Configuration

- **Data**: `LineMoveSettings`
- **Flow**: ViewModel properties (string) -> Parse -> Config Model (int?) -> JSON

---

## 3. Edge Cases

| Case | Input | Behavior | Implementation |
|------|-------|----------|----------------|
| Non-numeric input | "abc" in MoveNir | Parsed as null (None/All) | `int.TryParse` in Save logic |
| Empty input | "" | Parsed as null (None/All) | `string.IsNullOrWhiteSpace` check |
| Tab Switch | User clicks Tab 2 | `ActiveTabIndex` changes -> Selector updates UI | WPF Binding |

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Verification |
|-----------|--------------|
| `Save_Debounces_Calls` | Trigger setter 5 times quickly, verify Save only called once |
| `Combined_Move_Sequential` | Verify Line 1 completes before Line 2 starts |

### 4.2 Manual Verification

1. **Independent Settings**:
   - Set Line 1 "SampleName" to "A".
   - Set Line 2 "SampleName" to "B".
   - Switch tabs. Verify values persist and are distinct.
   - Restart app. Verify values loaded.

2. **Move Execution**:
   - Line 1 Tab: Click Move. Verify log shows "Subject: A".
   - Line 2 Tab: Click Move. Verify log shows "Subject: B".

---

## 5. Open Questions

- [x] All design questions resolved

---

## Approval

- [ ] Detailed pseudo-code provided
- [ ] Requirements covered
- [ ] No open questions

**Next Step**: 05_tasks.md
