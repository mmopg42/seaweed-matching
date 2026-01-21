---
phase: 31-cli-schema-standardization
plan: 01
subsystem: cli
tags: [json-response-standardization, cli-schema, system-text-json, exit-codes]

# Dependency graph
requires:
  - phase: 28-skill-registry-definition
    provides: skill definitions expecting standardized response formats
  - phase: 30-executor-skill-translation
    provides: executor needing reliable JSON parsing
provides:
  - JsonResponseModels.cs with SuccessResponse<T>, ErrorResponse, Response<T>, EmptySuccess record types
  - JsonResponseHelper.cs with PrintSuccess/PrintError/PrintEmptySuccess/PrintLegacy methods
  - ExitCodes.cs with IsRetryable() method and GetErrorName() helper
affects: [31-02, 31-03, 31-04] # Subsequent plans that migrate command handlers

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Standardized JSON response format with success, data/error, errorCode, retryable, suggestion fields"
    - "Retryable status mapping from exit codes (ERROR/TIMEOUT=true, SUCCESS/NOT_FOUND/INVALID_ARGUMENT=false)"
    - "Context-aware error suggestions via PrintErrorWithAutoSuggestion()"
    - "ISO 8601 timestamps in 'o' format for all responses"
    - "JsonNamingPolicy.CamelCase for consistent property naming"

key-files:
  created:
    - skills_scripts/ui_automation/Commands/JsonResponseModels.cs
    - skills_scripts/ui_automation/Commands/JsonResponseHelper.cs
  modified:
    - skills_scripts/ui_automation/Commands/ExitCodes.cs

key-decisions:
  - "Use record types for immutability and concise syntax (no JsonPropertyName attributes needed)"
  - "Rely on JsonNamingPolicy.CamelCase instead of attributes for camelCase JSON output"
  - "PrintLegacy() temporary helper for gradual migration of existing command handlers"
  - "Auto-suggestion generation based on error code and command context string"

patterns-established:
  - "Pattern 1: All JSON responses use consistent format with success boolean first"
  - "Pattern 2: Error responses always include retryable boolean and optional suggestion string"
  - "Pattern 3: Timestamp in ISO 8601 format ('o') for debugging and audit trails"
  - "Pattern 4: JsonResponseHelper.PrintSuccess<T>() for type-safe success responses"
  - "Pattern 5: JsonResponseHelper.PrintError() with auto-retryable from ExitCodes.IsRetryable()"

issues-created: []

# Metrics
duration: 5min
completed: 2026-01-21
---

# Phase 31 Plan 1: JSON Response Infrastructure Summary

**Standardized JSON response infrastructure with SuccessResponse<T>, ErrorResponse, and retryable/suggestion fields via JsonResponseHelper**

## Performance

- **Duration:** 5 min
- **Started:** 2026-01-21T13:42:42Z
- **Completed:** 2026-01-21T13:47:46Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments

- Created JsonResponseModels.cs with SuccessResponse<T>, ErrorResponse, Response<T>, EmptySuccess record types
- Extended ExitCodes.cs with IsRetryable() method and GetErrorName() helper for error code introspection
- Created JsonResponseHelper.cs with PrintSuccess(), PrintError(), PrintErrorWithAutoSuggestion(), PrintEmptySuccess(), PrintLegacy() methods
- All JSON output uses JsonNamingPolicy.CamelCase for consistent property naming
- ISO 8601 timestamps included in all responses for debugging

## Task Commits

Each task was committed atomically:

1. **Task 1: Create JsonResponseModels.cs with Standardized Response Types** - `98612a2` (feat)
2. **Task 2: Update ExitCodes.cs with Retryable Mapping** - `bcaf446` (feat)
3. **Task 3: Create JsonResponseHelper.cs with Standardized Output Methods** - `0f70ab8` (feat)

**Auto-fix commit:** `bdf59af` (fix) - Rule 3 blocking issue

**Plan metadata:** Pending

## Files Created/Modified

- `skills_scripts/ui_automation/Commands/JsonResponseModels.cs` - Standardized record types for all JSON responses (SuccessResponse<T>, ErrorResponse, Response<T>, EmptySuccess)
- `skills_scripts/ui_automation/Commands/JsonResponseHelper.cs` - Centralized JSON output helper with PrintSuccess/PrintError/PrintEmptySuccess/PrintLegacy methods
- `skills_scripts/ui_automation/Commands/ExitCodes.cs` - Added IsRetryable() method and GetErrorName() helper

## Decisions Made

- Use record types for immutability and concise syntax without JsonPropertyName attributes
- Rely on JsonNamingPolicy.CamelCase in JsonSerializerOptions for camelCase JSON output
- PrintLegacy() temporary helper for gradual migration - to be removed after Phase 31 Wave 2 completion
- Auto-suggestion generation based on error code and command context (MainWindow, SetupWindow, SettingsDialog, DataGrid)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed JsonPropertyName attributes to fix build error**

- **Found during:** Task 3 (build verification)
- **Issue:** JsonPropertyNameAttribute caused build error in .NET 10.0 despite being in System.Text.Json
- **Fix:** Removed [property: JsonPropertyName] attributes from record types, rely on JsonNamingPolicy.CamelCase in JsonResponseHelper for camelCase output
- **Files modified:** skills_scripts/ui_automation/Commands/JsonResponseModels.cs
- **Verification:** dotnet build succeeds with no errors
- **Committed in:** bdf59af (auto-fix commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Auto-fix was necessary for build to succeed. No scope creep. The change actually simplifies the code and matches the existing codebase pattern.

## Issues Encountered

None - all tasks completed successfully after auto-fix.

## Next Phase Readiness

- JSON response infrastructure complete and ready for command handler migration
- JsonResponseHelper.PrintSuccess/PrintError can be adopted by AppLifecycleCommands, WindowsCommands, DataPanelCommands, etc.
- PrintLegacy() provides backward compatibility for gradual migration
- Subsequent plans (31-02, 31-03, 31-04) will migrate command handlers to use the new infrastructure

---
*Phase: 31-cli-schema-standardization*
*Completed: 2026-01-21*
