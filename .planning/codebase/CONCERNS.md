# Codebase Concerns

**Analysis Date:** 2026-01-16

## Tech Debt

**GroupManager.cs exceeds file size guideline:**
- Issue: File is ~735 lines, exceeds 600-line soft limit
- Files: `ChronoView/Core/FileWatching/GroupManager.cs`
- Why: Grew organically during line separation feature implementation
- Impact: Harder to maintain, approaching 800-line hard limit
- Fix approach: Extract matching logic into separate classes (see `docs/trouble/groupmanager_refactoring_needed.md`)

**NirSpectrumMonitor dead code:**
- Issue: Contains only empty TODO methods with no actual implementation
- Files: Referenced in `docs/spec/remove_nir_spectrum_monitor/01_requirements.md`
- Why: Feature stub that was never completed
- Impact: Code bloat, confusion about feature availability
- Fix approach: Remove NirSpectrumMonitor class and all references

## Known Bugs

**Abnormal detection not working for image size:**
- Symptoms: Images with abnormal dimensions (e.g., 1936x4272) are not flagged as abnormal
- Files: `ChronoView/Core/Analytics/AbnormalDetectorService.cs`, `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
- Trigger: Any image with unusual aspect ratio
- Workaround: None currently
- Root cause: `AddAndCheckImage()` method exists but is never called; `IsGroupAbnormal()` only checks NIR-only groups
- Documentation: `docs/trouble/abnormal_detection_issue.md`
- Blocked by: Integration of image size analysis into FileGroupViewModel

**Window activation issues:**
- Symptoms: Camera programs may not activate correctly on launch
- Files: `docs/trouble/window_activation_issue.md`
- Trigger: Launching camera programs from application
- Workaround: Manual activation
- Root cause: Windows API timing issues
- Documentation: `docs/spec/fix_launch_activation_bugs/`

**Line 2 normal detection issues:**
- Symptoms: Line 2 cameras not detecting normal folder files correctly when suffix disabled
- Files: `docs/trouble/line2_normal_detection_issue.md`
- Trigger: Using Line 2 cameras with certain configuration
- Workaround: Enable suffix setting
- Root cause: Conditional logic in file matching
- Documentation: `docs/spec/fix_line2_detection_when_suffix_disabled/`

## Security Considerations

**External program execution:**
- Risk: Camera programs launched via Process.Start() with user-configurable paths
- Files: `ChronoView/Core/ProgramLaunching/GeneralCameraLauncher.cs`, `NirCameraLauncher.cs`, `Nir2CameraLauncher.cs`
- Current mitigation: Path configured in application settings (user-trusted)
- Recommendations: Add executable validation, whitelist allowed executables

**File operations on user data:**
- Risk: Move/delete operations modify user files
- Files: `ChronoView/Core/FileOperations/MoveService.cs`, `DeleteService.cs`
- Current mitigation: Operations require explicit user action
- Recommendations: Add undo/redo functionality for file operations

## Performance Bottlenecks

**Initial file scanning:**
- Problem: Initial scan can be slow for large directories
- Files: `ChronoView/Core/FileWatching/InitialScanner.cs`
- Measurement: Not specifically measured, but documented as issue
- Cause: Synchronous file enumeration
- Improvement path: Async scanning, progress reporting
- Documentation: `docs/trouble/initial_scan_performance_issue.md`

**Image loading:**
- Problem: Image thumbnails can delay UI updates
- Files: `ChronoView/UI/ViewModels/FileGroupMediaLoader.cs`
- Measurement: Delays documented in `docs/spec/fix_general_cam_thumbnail_delay/`
- Cause: Synchronous image loading, large image files
- Improvement path: Async loading with cancellation (partially implemented)

**Event processing:**
- Problem: File system events can queue up during high activity
- Files: `ChronoView/Core/FileWatching/EventProcessor.cs`
- Measurement: Debouncing implemented to reduce noise
- Cause: Rapid file creation events
- Improvement path: Priority queuing (implemented), batch processing

## Fragile Areas

**MonitoringOrchestrator state management:**
- File: `ChronoView/Core/FileWatching/MonitoringOrchestrator.cs`
- Why fragile: Coordinates 8+ services, complex startup/shutdown sequencing
- Common failures: Services not in correct state, event handler leaks
- Safe modification: Add state machine for lifecycle transitions
- Test coverage: Smoke tests exist, full integration tests needed

**File matching timing windows:**
- Files: `ChronoView/Core/FileMatching/FileMatchingEngine.cs`
- Why fragile: Timestamp-based matching with configurable time windows
- Common failures: Missed matches when timing is off, edge cases around midnight
- Safe modification: Add comprehensive test cases for time boundaries
- Test coverage: Property tests exist for consistency

**GroupManager matching logic:**
- File: `ChronoView/Core/FileWatching/GroupManager.cs`
- Why fragile: Three different match types, complex conditionals
- Common failures: Wrong match type selected, stale cache
- Safe modification: Extract matchers to separate classes (see tech debt)
- Test coverage: Partial, needs more edge case coverage

## Scaling Limits

**File group count:**
- Current capacity: Unknown (depends on memory)
- Limit: UI performance degrades with hundreds of groups
- Symptoms at limit: Slow DataGrid scrolling, memory pressure
- Scaling path: Virtualization in DataGrid, pagination

**Image cache:**
- Current capacity: Configurable (default 100MB)
- Limit: LruCache with memory pressure
- Symptoms at limit: Cache thrashing, reloads
- Scaling path: Smart caching based on visibility, predictive preloading

## Dependencies at Risk

**None identified**
- All NuGet packages are actively maintained
- .NET 10.0 is current version

## Missing Critical Features

**Undo/Redo for file operations:**
- Problem: No way to undo move/delete operations
- Current workaround: Manual file recovery from delete bucket
- Blocks: Safe experimentation with file operations
- Implementation complexity: High (requires operation history, reverse operations)

**Comprehensive abnormal detection:**
- Problem: Image size detection implemented but not integrated
- Current workaround: None
- Blocks: Reliable anomaly triage
- Implementation complexity: Medium (integration point exists)
- Documentation: `docs/spec/fix_anomaly_detection/`

## Test Coverage Gaps

**MonitoringOrchestrator integration:**
- What's not tested: Full file monitoring workflow from event to UI
- Risk: Workflow breakage during refactoring
- Priority: High
- Difficulty to test: Requires file system mocking, async coordination

**UI automation:**
- What's not tested: WPF UI interactions, visual correctness
- Risk: UI regressions, broken bindings
- Priority: Medium
- Difficulty to test: FlaUI available but not extensively used

**File operation rollback:**
- what's not tested: Error recovery during move/delete operations
- Risk: Data loss if operations fail mid-way
- Priority: High
- Difficulty to test: Complex file system setup

**Event processing edge cases:**
- What's not tested: Rapid file creation, duplicate events, concurrent access
- Risk: Missed files, duplicate groups
- Priority: Medium
- Difficulty to test: Timing-sensitive, hard to reproduce

## Extensive Troubleshooting Documentation

The project has extensive troubleshooting and fix documentation indicating active issue resolution:
- 35+ trouble documents in `docs/trouble/`
- 50+ fix specifications in `docs/spec/fix_*/`
- Indicates complex domain with many edge cases

This documentation serves as both a concern (many known issues) and a strength (well-documented problems).

---

*Concerns audit: 2026-01-16*
*Update as issues are fixed or new ones discovered*
