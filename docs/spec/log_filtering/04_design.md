---
Task: log_filtering
Created: 2026-01-08
Status: Draft
Depends On: 03_plan.md
---

# Log Filtering - Detailed Design

## 1. Component Designs

### 1.1 LogMessage

#### Interface

```csharp
public class LogMessage
{
    // ... Existing properties ...
    public int? LineNumber { get; set; } // New property for filtering
}
```

### 1.2 MainWindowViewModel

#### Detailed Logic

```pseudo
// Central Inference Logic
function AddLogMessage(severity, source, message):
    // 1. Infer Line Number
    int? lineNumber = InferLineNumber(source, message)
    
    // 2. Format Message
    string displayMessage = message
    if lineNumber != null:
        displayMessage = $"[Line {lineNumber}] {message}"
        
    // 3. Create & Add
    logMsg = new LogMessage(severity, source, displayMessage)
    logMsg.LineNumber = lineNumber
    
    LogMessages.Add(logMsg)

function InferLineNumber(source, message):
    text = (source + " " + message).ToLower()
    
    // Line 2 Patterns
    if Regex.IsMatch(text, @"(nir2|normal2|cam[456]|line\s?2|normal2path|camera[456]path)"):
        return 2
        
    // Line 1 Patterns
    if Regex.IsMatch(text, @"(nir1|normal1|cam[123]|line\s?1|normal1path|camera[123]path)"):
        return 1
        
    return null // System/Common
```

### 1.3 LogPanel.xaml.cs

#### Interface

```csharp
public static readonly DependencyProperty ActiveTabIndexProperty = ...
public int ActiveTabIndex { get; set; } // 0, 1, 2
```

#### Detailed Logic

```pseudo
function FilterLogMessage(object obj):
    msg = obj as LogMessage
    if msg is null: return false
    
    // ... Existing Search/Level Filter ...
    
    // Line Filtering
    // ActiveTabIndex: 0=Line1, 1=Line2, 2=Combined
    
    if ActiveTabIndex == 0: // Line 1 Tab
        // Show if Line 1 OR System (null)
        if msg.LineNumber == 2: return false
        
    if ActiveTabIndex == 1: // Line 2 Tab
        // Show if Line 2 OR System (null)
        if msg.LineNumber == 1: return false
        
    // Combined (2) -> Show everything
    
    return true

// Event Handler
OnActiveTabIndexChanged():
    _filteredView.Refresh()
```

---

## 2. Integration Points

### 2.1 MainWindow.xaml Binding

```xml
<local:LogPanel 
    LogMessages="{Binding LogMessages}" 
    ActiveTabIndex="{Binding ActiveTabIndex, Mode=OneWay}" />
```

---

## 4. Testing Strategy

### 4.1 Unit Test Cases

| Test Name | Input (Source, Msg) | Expected LineNumber |
|-----------|---------------------|---------------------|
| `Infer_Nir1` | "NirSpectrumMonitor", "Values..." | 1 (implied if logic matches component names, currently specific regex "nir1") |
| `Infer_Line2_Explicit` | "System", "Moving Line 2" | 2 |
| `Infer_System` | "System", "Ready" | null |

> **Note**: The Regex patterns need to be robust enough to catch the component names commonly used.
> - Line 1: `nir1`, `normal1`, `cam1`, `cam2`, `cam3`
> - Line 2: `nir2`, `normal2`, `cam4`, `cam5`, `cam6`

---
