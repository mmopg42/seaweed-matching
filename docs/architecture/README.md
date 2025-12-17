# Architecture Documentation Index

> **Purpose**: Entry point for any AI modifying this C# codebase.  
> **Read relevant docs BEFORE making changes.**  
> **Run verification commands BEFORE trusting documented dependents.**  
> **Check glossary.md for official naming BEFORE writing any document.**

> Last Updated: 2024-12-16

## Quick Start
> New to this codebase? Start here.

1. [Glossary](glossary.md) - **READ FIRST** - Official naming definitions
2. Core Modules (below) - Understand key components

## Core Modules

### File Watching & Monitoring
- [module_monitoring_orchestrator.md](module_monitoring_orchestrator.md) - Orchestrates file monitoring, matching, and group creation
  - **Last Updated**: 2024-12-14
  - **Key Change**: Fixed real-time matching to use FileGroupMatcher consistently
  - **High Risk**: Changes affect real-time file grouping behavior

- [module_file_watcher_service.md](module_file_watcher_service.md) - Hybrid file monitoring (Watcher + Polling)
  - **Last Updated**: 2025-12-15
  - **Key Change**: Added polling for network drive support
  - **Critical**: Reliability of file detection depends on this

### File Matching
- [module_file_group_matcher.md](module_file_group_matcher.md) - Core file grouping algorithm
  - **Last Updated**: 2024-12-14
  - **Key Insight**: GroupId is regenerated on every call; use NormalFolder/NirKey as stable identifiers
  - **High Risk**: Matching logic affects all file grouping operations

### Configuration & Settings
- [impact_deprecated_matching_properties.md](impact_deprecated_matching_properties.md) - Deprecated matching properties migration
  - **Last Updated**: 2024-12-16
  - **Key Change**: NirTimeWindowSeconds, CameraTimeWindowSeconds, NormalFolderTimeWindowSeconds marked obsolete
  - **Migration**: Use DataSequenceSettings instead

### UI/UX Design
- [design_guidelines.md](design_guidelines.md) - ChronoView UI/UX design system
  - **Last Updated**: 2025-12-15
  - **Purpose**: Maintain visual consistency across all windows and dialogs
  - **Required**: Read before creating any new XAML views or controls

## Document Registry
| Document | Type | Last Updated | Verified |
|----------|------|--------------|----------|
| glossary.md | Reference | 2024-12-16 | 2024-12-16 |
| design_guidelines.md | UI/UX | 2025-12-15 | 2025-12-15 |
| module_monitoring_orchestrator.md | Module | 2024-12-14 | 2024-12-14 |
| module_file_group_matcher.md | Module | 2024-12-14 | 2024-12-14 |
| module_file_watcher_service.md | Module | 2025-12-15 | 2025-12-15 |
| impact_deprecated_matching_properties.md | Impact | 2024-12-16 | 2024-12-16 |
| module_nir_file_resolver.md | Module | - | - |
| feature_ui_group_display.md | Feature | - | - |


## How to Update Documentation

When modifying code:
1. **Check glossary** for official term names
2. **Update affected module docs** (contracts, dependencies, touchpoints)
3. **Add verification commands** for new dependents
4. **Update this README** if adding new documents
5. **Run verification commands** to ensure docs match code

## Verification Best Practices

All module documents include `<!-- VERIFY: ... -->` commands. Run these before trusting the documented information:

```bash
# Example: Verify MonitoringOrchestrator dependents
grep -rn "MonitoringOrchestrator" ChronoView/
grep -rn "IMonitoringOrchestrator" ChronoView/
```

If results differ from documentation, **UPDATE THE DOCS FIRST** before making changes.
