---
Task: fix_logging_redundancy
Created: 2026-01-13
Status: Approved
Depends On: 01_requirements.md, 02_research.md
---

# fix_logging_redundancy - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| Logs written to session file | Simplification of `FileLogger.Log` | Check log file in `%APPDATA%\ChronoView\Logs\{YYYYMMDD}\` |
| No double-dated files | Removal of redundant path calculation | Verify only one `.log` file is created per session |
| Session header preserved | `App.xaml.cs` logic remains intact | Open log file and check for "Session Started" header |
| Standard log entry format | `FileLogger.Log` formatting logic | Verify logs follow `[Timestamp] [Level] [Category] Message` format |

## 1. Architecture Overview

### 1. Sistema Context
The logging system uses `FileLoggerProvider` to sink `Microsoft.Extensions.Logging` calls to the local file system. The initialization happens in `App.xaml.cs` where a session-specific filename is generated.

### 1.2 Data Flow
```
App.xaml.cs (Generates Path: HHmmss.log)
    │
    ▼
FileLoggerProvider (Receives Path)
    │
    ▼
FileLogger.Log(message)
    │
    ▼
[Modified Logic: Use Received Path Directly]
    │
    ▼
File.AppendAllText(HHmmss.log, message)
```

## 2. Components

### 2.1 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `FileLogger` | `ChronoView/Infrastructure/Logging/FileLoggerProvider.cs` | Remove redundant filename reconstruction; use `_logFilePath` directly. | No |

## 3. Interface Definitions

### 3.1 FileLogger
```csharp
internal class FileLogger : ILogger
{
    private readonly string _logFilePath;
    
    // Responsibility: Append formatted log entries to _logFilePath.
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
}
```

## 4. Key Design Decisions

### 4.1 Use Initialization Path vs. Dynamic Path
**Context**: The current implementation tries to dynamically change the log file if the date changes.
**Decision**: Use the initialized path directly.
**Rationale**: This aligns with the requirement for session-specific logs (`HHmmss`). `App.xaml.cs` is the single source of truth for the session's log file path.

## 7. Glossary Updates

### Existing Terms Check
| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `FileLoggerProvider` | Main logging provider class. |

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Multi-day session logging | Low | Medium | The log file will stay in the folder corresponding to the session start date. This is acceptable for a capture application. |

## 10. Open Questions

- [x] Should we still create the directory if it's missing? → Yes, as a safety measure.

---

## Approval

- [x] All requirements traced to components
- [x] Component interfaces defined
- [x] Design decisions documented with rationale
- [x] Glossary terms identified
- [x] All open questions resolved

**Next Step**: 04_design.md
