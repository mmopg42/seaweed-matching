---
Task: Unified Config Paths
Created: 2026-01-14
Status: Complete
Depends On: 05_tasks.md
---

# Unified Config Paths - Completion Report

## 1. Summary

Successfully centralized application path management into `IConfigurationManager`. All hardcoded path references in business logic and UI components have been removed and replaced with centralized properties.

## 2. Changes Implemented

### 2.1 Core Infrastructure
- **`IConfigurationManager`**: Added `LogsDirectory` and `HistoryFilePath` properties.
- **`ConfigurationManager`**: Implemented these properties using `%LOCALAPPDATA%\prische\ChronoView` as the base.
- **`PathHelper`**: Created a static helper for UserControls (like `LogPanel`) to access paths via DI service location or safe fallback.
- **`Glossary`**: Updated with new terms (`LogsDirectory`, `HistoryFilePath`, `PathHelper`).

### 2.2 Refactored Components
| Component | Change |
|-----------|--------|
| `MainWindowViewModel` | Replaced hardcoded log path with `_configManager.LogsDirectory`. |
| `LogPanel.xaml.cs` | Replaced 3 hardcoded paths with `PathHelper.LogsDirectory`. |
| `LogCleanupService` | Removed local `GetLogDirectory()` helper, used `_configManager.LogsDirectory`. |
| `AbnormalHistoryManager` | Updated constructor to accept `IConfigurationManager` (with backward compatibility) and use `HistoryFilePath`. |
| `App.xaml.cs` | Verified consistency (Development logging remains independent but matches SSoT path structure). |

## 3. Verification Results

### 3.1 Build Verification
- **Command**: `dotnet build ChronoView/ChronoView.csproj`
- **Result**: **SUCCESS** (0 Errors).

### 3.2 Path Consistency Check
- **Command**: Powershell script searching for `SpecialFolder` usage.
- **Result**: **CLEAN**. Only found usages in:
    - `ConfigurationManager.cs` (The Source of Truth)
    - `PathHelper.cs` (The Fallback)
    - `App.xaml.cs` (Bootstrap logging - Allowed Exception)
    - All other files are free of hardcoded paths.

## 4. Unresolved Issues / Tech Debt
- `App.xaml.cs` contains a duplicate path definition for bootstrap logging which runs before DI. This is intentional to ensure logging works if DI fails, but technically violates DRY.
- `AbnormalDetectorService` relies on a secondary constructor for testing that necessitates a fallback logic in `AbnormalHistoryManager`. This was handled gracefully but indicates a potential area for future test refactoring.

## 5. Conclusion
The system now adheres to the Single Source of Truth principle for file paths. Future changes to the directory structure (e.g., changing Author name or App name) need only be made in `ConfigurationManager.cs` (and `PathHelper.cs` fallback).
