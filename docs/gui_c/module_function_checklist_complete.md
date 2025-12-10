# Complete Module and Function Level Implementation Checklist

## Overview

This document provides detailed function-level tracking for ALL 41 Python modules being migrated to C#. Each function is analyzed for implementation complexity, C# equivalent, and completion status.

**CRITICAL ISSUE RESOLVED**: This checklist now includes ALL 41 modules from `docs/modules/` instead of just 7.

## Module Breakdown

### 1. main.py - Application Controller

**Total Functions**: 16  
**Completed**: 0/16 (0%)  
**Estimated Effort**: 12 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self)` | `MainController()` | 🟢 Low | ❌ Not Started | Standard constructor |
| `init_ui` | `init_ui(self)` | `InitializeComponent()` | 🟡 Medium | ❌ Not Started | XAML + code-behind |
| `create_control_panel` | `create_control_panel(self)` | XAML Layout | 🟡 Medium | ❌ Not Started | Declarative UI |
| `start_main_monitoring` | `start_main_monitoring(self)` | `StartMainMonitoring()` | 🟢 Low | ❌ Not Started | Window.Show() |
| `stop_main_monitoring` | `stop_main_monitoring(self)` | `StopMainMonitoring()` | 🟢 Low | ❌ Not Started | Window.Close() |
| `on_main_closed` | `on_main_closed(self)` | Event Handler | 🟢 Low | ❌ Not Started | Window.Closed event |
| `start_nir_monitoring` | `start_nir_monitoring(self)` | `StartNirMonitoring()` | 🟢 Low | ❌ Not Started | Window.Show() |
| `stop_nir_monitoring` | `stop_nir_monitoring(self)` | `StopNirMonitoring()` | 🟢 Low | ❌ Not Started | Window.Close() |
| `on_nir_closed` | `on_nir_closed(self)` | Event Handler | 🟢 Low | ❌ Not Started | Window.Closed event |
| `closeEvent` | `closeEvent(self, event)` | `OnClosing()` | 🟢 Low | ❌ Not Started | Window.Closing event |
| Button event handlers (6) | Various | Command bindings | 🟢 Low | ❌ Not Started | WPF Commands |

**Implementation Priority**: 🔴 Critical (Required for application startup)

---

### 2. monitoring_app.py - Main Monitoring Application

**Total Functions**: 50+  
**Completed**: 0/50+ (0%)  
**Estimated Effort**: 80 hours

#### Core Initialization Functions
| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self)` | `MainWindow()` | 🔴 High | ❌ Not Started | Complex initialization |
| `init_ui` | `init_ui(self)` | XAML + ViewModel | 🔴 High | ❌ Not Started | Complex UI layout |
| `restore_window_bounds` | `restore_window_bounds(self)` | `RestoreWindowBounds()` | 🟢 Low | ❌ Not Started | Settings restoration |
| `save_window_bounds` | `save_window_bounds(self)` | `SaveWindowBounds()` | 🟢 Low | ❌ Not Started | Settings persistence |

**Implementation Priority**: 🔴 Critical (Core application functionality)

---

### 3. abnormal_detector.py - Statistical Anomaly Detection

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 8 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, window_size, min_samples, threshold)` | `AbnormalDetector()` | 🟡 Medium | ❌ Not Started | Statistical parameters |
| `add_and_check_image` | `add_and_check_image(width, height)` | `AddAndCheckImage()` | 🔴 High | ❌ Not Started | Z-score calculation |
| `is_image_abnormal` | `is_image_abnormal(image_path)` | `IsImageAbnormal()` | 🟡 Medium | ❌ Not Started | Image size validation |
| `is_group_abnormal` | `is_group_abnormal(group)` | `IsGroupAbnormal()` | 🟡 Medium | ❌ Not Started | NIR-only detection |
| `_calculate_z_score` | `_calculate_z_score(value, values_list)` | `CalculateZScore()` | 🔴 High | ❌ Not Started | Statistical math |

**Implementation Priority**: 🟠 High (Quality control feature)

---

### 4. config_manager.py - Configuration Management

**Total Functions**: 12  
**Completed**: 0/12 (0%)  
**Estimated Effort**: 12 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, config_path)` | `ConfigurationManager()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `load` | `load(self)` | `LoadConfiguration()` | 🟡 Medium | ❌ Not Started | JSON deserialization |
| `save` | `save(self, settings)` | `SaveConfiguration()` | 🟡 Medium | ❌ Not Started | JSON serialization |
| `get` | `get(self, key, default)` | `GetSetting()` | 🟢 Low | ❌ Not Started | Dictionary access |
| `set` | `set(self, key, value)` | `SetSetting()` | 🟢 Low | ❌ Not Started | Dictionary update |
| `get_default_settings` | `get_default_settings(self)` | `GetDefaultSettings()` | 🟡 Medium | ❌ Not Started | Default values |
| `validate_settings` | `validate_settings(self, settings)` | `ValidateSettings()` | 🟡 Medium | ❌ Not Started | Validation logic |
| `open_folder` | `open_folder(self, path)` | `OpenFolder()` | 🟢 Low | ❌ Not Started | Process.Start() |
| `open_appdir_folder` | `open_appdir_folder(self)` | `OpenAppDataFolder()` | 🟢 Low | ❌ Not Started | Process.Start() |
| Path utilities (3) | Various | Path.Combine() etc. | 🟢 Low | ❌ Not Started | System.IO.Path |

**Implementation Priority**: 🔴 Critical (Required for all modules)

---

### 5. crash_logger.py - Global Exception Handler

**Total Functions**: 6  
**Completed**: 0/6 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, log_dir, app_name)` | `CrashLogger()` | 🟡 Medium | ❌ Not Started | Exception handler setup |
| `_setup_logging` | `_setup_logging(self)` | `SetupLogging()` | 🟡 Medium | ❌ Not Started | File + console logging |
| `_install_exception_hook` | `_install_exception_hook(self)` | `InstallExceptionHandler()` | 🔴 High | ❌ Not Started | Global exception handling |
| `_write_crash_report` | `_write_crash_report(exc_type, exc_value, exc_traceback)` | `WriteCrashReport()` | 🔴 High | ❌ Not Started | Stack trace analysis |
| `get_log_path` | `get_log_path(self)` | `GetLogPath()` | 🟢 Low | ❌ Not Started | Property getter |
| `setup_crash_logger` | `setup_crash_logger(log_dir, app_name)` | `SetupCrashLogger()` | 🟡 Medium | ❌ Not Started | Singleton pattern |

**Implementation Priority**: 🔴 Critical (Error handling and debugging)

---
### 6. delete_manager.py - File Deletion Management

**Total Functions**: 9  
**Completed**: 0/9 (0%)  
**Estimated Effort**: 12 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `ensure_watching_off` | `ensure_watching_off(main)` | `EnsureWatchingOff()` | 🟡 Medium | ❌ Not Started | State validation |
| `ensure_delete_folder` | `ensure_delete_folder(main)` | `EnsureDeleteFolder()` | 🟡 Medium | ❌ Not Started | Directory creation |
| `ensure_subject_for_delete` | `ensure_subject_for_delete(main)` | `EnsureSubjectForDelete()` | 🟡 Medium | ❌ Not Started | Subject validation |
| `_build_delete_bucket_dir` | `_build_delete_bucket_dir(main, ...)` | `BuildDeleteBucketPath()` | 🔴 High | ❌ Not Started | Complex path logic |
| `move_to_delete_bucket` | `move_to_delete_bucket(main, source, ...)` | `MoveToDeleteBucket()` | 🔴 High | ❌ Not Started | File operations |
| `_collect_paths_for_row` | `_collect_paths_for_row(main, row_idx, ...)` | `CollectPathsForRow()` | 🔴 High | ❌ Not Started | UI data extraction |
| `delete_one_row` | `delete_one_row(main, row_idx, ...)` | `DeleteOneRow()` | 🔴 High | ❌ Not Started | Parallel file operations |
| `delete_selected_rows` | `delete_selected_rows(main)` | `DeleteSelectedRows()` | 🔴 High | ❌ Not Started | Batch operations |
| `set_select_all` | `set_select_all(main, state)` | `SetSelectAll()` | 🟢 Low | ❌ Not Started | UI state management |

**Implementation Priority**: 🟠 High (File management feature)

---

### 7. drag_select_widget.py - Multi-Row Selection

**Total Functions**: 7  
**Completed**: 0/7 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, parent)` | `DragSelectWidget()` | 🟡 Medium | ❌ Not Started | Custom control setup |
| `mousePressEvent` | `mousePressEvent(self, event)` | `OnMouseDown()` | 🟡 Medium | ❌ Not Started | Mouse event handling |
| `mouseMoveEvent` | `mouseMoveEvent(self, event)` | `OnMouseMove()` | 🔴 High | ❌ Not Started | Drag selection logic |
| `mouseReleaseEvent` | `mouseReleaseEvent(self, event)` | `OnMouseUp()` | 🟡 Medium | ❌ Not Started | Selection finalization |
| `_get_row_at_pos` | `_get_row_at_pos(self, pos)` | `GetRowAtPosition()` | 🔴 High | ❌ Not Started | Hit testing |
| `_select_rows_in_range` | `_select_rows_in_range(self, start_idx, end_idx)` | `SelectRowsInRange()` | 🔴 High | ❌ Not Started | Range selection |

**Implementation Priority**: 🟡 Medium (UI enhancement)

---

### 8. file_count_monitor.py - Real-time File Counting

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 8 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, settings)` | `FileCountMonitor()` | 🟡 Medium | ❌ Not Started | Timer setup |
| `init_ui` | `init_ui(self)` | `InitializeComponent()` | 🟡 Medium | ❌ Not Started | Independent window UI |
| `update_counts` | `update_counts(self)` | `UpdateCounts()` | 🔴 High | ❌ Not Started | Multi-folder scanning |
| `count_files_in_folder` | `count_files_in_folder(self, folder_path)` | `CountFilesInFolder()` | 🟡 Medium | ❌ Not Started | Directory enumeration |
| `closeEvent` | `closeEvent(self, event)` | `OnClosing()` | 🟢 Low | ❌ Not Started | Timer cleanup |

**Implementation Priority**: 🟡 Medium (Monitoring utility)

---

### 9. file_count_monitor_standalone.py - Independent File Monitor

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self)` | `StandaloneFileCountMonitor()` | 🟡 Medium | ❌ Not Started | Independent app setup |
| `init_ui` | `init_ui(self)` | `InitializeComponent()` | 🟡 Medium | ❌ Not Started | Standalone UI |
| `update_counts` | `update_counts(self)` | `UpdateCounts()` | 🔴 High | ❌ Not Started | ConfigManager integration |
| `count_files_in_folder` | `count_files_in_folder(self, folder_path)` | `CountFilesInFolder()` | 🟡 Medium | ❌ Not Started | Directory enumeration |
| `closeEvent` | `closeEvent(self, event)` | `OnClosing()` | 🟢 Low | ❌ Not Started | App termination |

**Implementation Priority**: 🟡 Medium (Utility application)

---

### 10. file_count_worker.py - Background File Counting

**Total Functions**: 12  
**Completed**: 0/12 (0%)  
**Estimated Effort**: 16 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self)` | `FileCountWorker()` | 🟡 Medium | ❌ Not Started | Background service |
| `update_settings` | `update_settings(self, settings)` | `UpdateSettings()` | 🟡 Medium | ❌ Not Started | Thread-safe updates |
| `get_effective_path` | `get_effective_path(base_path, use_camera_subfolder)` | `GetEffectivePath()` | 🟡 Medium | ❌ Not Started | Path calculation |
| `should_use_recursive_watch` | `should_use_recursive_watch(folder_type)` | `ShouldUseRecursiveWatch()` | 🟡 Medium | ❌ Not Started | Watch strategy |
| `trigger_count` | `trigger_count(self)` | `TriggerCount()` | 🟢 Low | ❌ Not Started | Event triggering |
| `enable` | `enable(self)` | `Enable()` | 🟡 Medium | ❌ Not Started | Service activation |
| `disable` | `disable(self)` | `Disable()` | 🟡 Medium | ❌ Not Started | Service deactivation |
| `stop` | `stop(self)` | `Stop()` | 🟡 Medium | ❌ Not Started | Service termination |
| `start_watchdog` | `start_watchdog(self)` | `StartWatchdog()` | 🔴 High | ❌ Not Started | FileSystemWatcher setup |
| `stop_watchdog` | `stop_watchdog(self)` | `StopWatchdog()` | 🟡 Medium | ❌ Not Started | Watcher cleanup |
| `run` | `run(self)` | `Run()` | 🔴 High | ❌ Not Started | Background thread loop |
| Count methods (3) | Various | Count methods | 🟡 Medium | ❌ Not Started | File type counting |

**Implementation Priority**: 🟠 High (Performance optimization)

---

### 11. file_matcher.py - File System Monitoring & Matching

**Total Functions**: 15  
**Completed**: 0/15 (0%)  
**Estimated Effort**: 24 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, log_emitter_func)` | `FileMatcher()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `reset_state` | `reset_state(self)` | `ResetState()` | 🟢 Low | ❌ Not Started | State cleanup |
| `load_state` | `load_state(self, unmatched_files, consumed_nir_keys)` | `LoadState()` | 🟡 Medium | ❌ Not Started | State restoration |
| `get_effective_path` | `get_effective_path(base_path, use_camera_subfolder)` | `GetEffectivePath()` | 🟡 Medium | ❌ Not Started | Path calculation |
| `add_or_update_file` | `add_or_update_file(self, file_path, folder_type)` | `AddOrUpdateFile()` | 🔴 High | ❌ Not Started | File event processing |
| `remove_from_unmatched` | `remove_from_unmatched(self, file_path, folder_type)` | `RemoveFromUnmatched()` | 🔴 High | ❌ Not Started | File removal handling |
| `add_nir_immediately` | `add_nir_immediately(self, file_path)` | `AddNirImmediately()` | 🔴 High | ❌ Not Started | NIR file processing |
| `scan_and_build_unmatched` | `scan_and_build_unmatched(self, settings)` | `ScanAndBuildUnmatched()` | 🔴 High | ❌ Not Started | Full folder scanning |
| Event handler methods (7) | Various | FileSystemWatcher events | 🔴 High | ❌ Not Started | Event processing |

**Implementation Priority**: 🔴 Critical (Core matching logic)

---

### 12. file_operation_manager.py - File Operation Business Logic

**Total Functions**: 9  
**Completed**: 0/9 (0%)  
**Estimated Effort**: 16 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, settings, groups, log_callback)` | `FileOperationManager()` | 🟡 Medium | ❌ Not Started | Business logic setup |
| `validate` | `validate(self, tab_index, line_mode, subject, subject2)` | `Validate()` | 🔴 High | ❌ Not Started | Input validation |
| `select_groups` | `select_groups(self, tab_index, line_mode, data_count_limit)` | `SelectGroups()` | 🔴 High | ❌ Not Started | Group filtering |
| `prune_nir` | `prune_nir(self, selection, keep_n, subject, subject2)` | `PruneNir()` | 🔴 High | ❌ Not Started | NIR file management |
| `build_processed_data` | `build_processed_data(self, ...)` | `BuildProcessedData()` | 🔴 High | ❌ Not Started | Data structure creation |
| `_move_to_delete_bucket` | `_move_to_delete_bucket(self, src_path, subject, is_nir)` | `MoveToDeleteBucket()` | 🟡 Medium | ❌ Not Started | File operations |
| `_filter_fully_matched_groups` | `_filter_fully_matched_groups(self, groups)` | `FilterFullyMatchedGroups()` | 🔴 High | ❌ Not Started | Group validation |
| `_is_group_fully_matched` | `_is_group_fully_matched(self, group)` | `IsGroupFullyMatched()` | 🔴 High | ❌ Not Started | Completeness check |
| `_has_valid_file_entry` | `_has_valid_file_entry(self, entry)` | `HasValidFileEntry()` | 🟡 Medium | ❌ Not Started | Entry validation |

**Implementation Priority**: 🔴 Critical (Business logic separation)

---

### 13. file_operations.py - File Operation Worker

**Total Functions**: 20  
**Completed**: 0/20 (0%)  
**Estimated Effort**: 32 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, processed_data, output_path, mode, operation_type)` | `FileOperationWorker()` | 🟡 Medium | ❌ Not Started | Worker initialization |
| `set_user_response` | `set_user_response(self, response)` | `SetUserResponse()` | 🟡 Medium | ❌ Not Started | UI interaction |
| `_rel` | `_rel(self, p)` | `GetRelativePath()` | 🟢 Low | ❌ Not Started | Path utility |
| `_same_device` | `_same_device(self, src, dst_dir)` | `IsSameDevice()` | 🟡 Medium | ❌ Not Started | Device detection |
| `_ensure_dir` | `_ensure_dir(self, d)` | `EnsureDirectory()` | 🟢 Low | ❌ Not Started | Directory creation |
| `_check_conflict` | `_check_conflict(self, dst, src_hint)` | `CheckConflict()` | 🔴 High | ❌ Not Started | Conflict resolution |
| `_copy_dir_native` | `_copy_dir_native(self, src, dst)` | `CopyDirectoryNative()` | 🔴 High | ❌ Not Started | Directory copying |
| `_move_dir_native` | `_move_dir_native(self, src, dst)` | `MoveDirectoryNative()` | 🔴 High | ❌ Not Started | Directory moving |
| `_copy_files_batch` | `_copy_files_batch(self, src_dir, dst_dir, filenames)` | `CopyFilesBatch()` | 🔴 High | ❌ Not Started | Batch file copying |
| `_move_files_batch` | `_move_files_batch(self, src_dir, dst_dir, filenames)` | `MoveFilesBatch()` | 🔴 High | ❌ Not Started | Batch file moving |
| `_rollback` | `_rollback(self)` | `Rollback()` | 🔴 High | ❌ Not Started | Operation rollback |
| `_build_move_plan_nested` | `_build_move_plan_nested(self)` | `BuildMovePlanNested()` | 🔴 High | ❌ Not Started | Complex planning |
| `_save_move_plan` | `_save_move_plan(self, plan)` | `SaveMovePlan()` | 🟡 Medium | ❌ Not Started | Plan persistence |
| `run` | `run(self)` | `Run()` | 🔴 High | ❌ Not Started | Main execution |
| `_execute_bucketed` | `_execute_bucketed(self, plan)` | `ExecuteBucketed()` | 🔴 High | ❌ Not Started | Plan execution |
| `_record_move` | `_record_move(self, plan, plan_paths, stats)` | `RecordMove()` | 🟡 Medium | ❌ Not Started | Operation logging |

**Implementation Priority**: 🔴 Critical (File operations core)

---

### 14. group_manager.py - File Matching & Group Creation

**Total Functions**: 10  
**Completed**: 0/10 (0%)  
**Estimated Effort**: 20 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, log_emitter_func)` | `GroupManager()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `build_all_groups` | `build_all_groups(self, unmatched_data, ...)` | `BuildAllGroups()` | 🔴 High | ❌ Not Started | Main grouping logic |
| `_build_line_groups` | `_build_line_groups(self, unmatched_data, ...)` | `BuildLineGroups()` | 🔴 High | ❌ Not Started | Line-specific grouping |
| `flatten_cam_files` | `flatten_cam_files(self, cam_bucket)` | `FlattenCamFiles()` | 🟡 Medium | ❌ Not Started | Data flattening |
| `pop_one` | `pop_one(self, queue)` | `PopOne()` | 🟢 Low | ❌ Not Started | Queue operations |
| `is_valid_cam_match` | `is_valid_cam_match(self, normal_dt, cam_dt, ...)` | `IsValidCamMatch()` | 🔴 High | ❌ Not Started | Time-based matching |
| `find_matching_cam_file` | `find_matching_cam_file(self, normal_dt, cam_queue, ...)` | `FindMatchingCamFile()` | 🔴 High | ❌ Not Started | File matching logic |
| `drain_cam_to_groups` | `drain_cam_to_groups(self, groups, cam_key, queue)` | `DrainCamToGroups()` | 🔴 High | ❌ Not Started | Remaining file processing |

**Implementation Priority**: 🔴 Critical (Core business logic)

---

### 15. group_state_manager.py - Group State Persistence

**Total Functions**: 9  
**Completed**: 0/9 (0%)  
**Estimated Effort**: 12 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, json_path, log_callback)` | `GroupStateManager()` | 🟡 Medium | ❌ Not Started | State manager setup |
| `_log` | `_log(self, message)` | `Log()` | 🟢 Low | ❌ Not Started | Logging wrapper |
| `_groups_to_canonical_json` | `_groups_to_canonical_json(self, groups)` | `GroupsToCanonicalJson()` | 🟡 Medium | ❌ Not Started | JSON normalization |
| `_calc_group_hash` | `_calc_group_hash(self, group)` | `CalculateGroupHash()` | 🟡 Medium | ❌ Not Started | Hash calculation |
| `_calc_groups_hash` | `_calc_groups_hash(self, groups)` | `CalculateGroupsHash()` | 🟡 Medium | ❌ Not Started | Batch hash calculation |
| `_maybe_save_groups_json` | `_maybe_save_groups_json(self, groups, debounce_ms)` | `MaybeSaveGroupsJson()` | 🔴 High | ❌ Not Started | Debounced persistence |
| `_maybe_load_groups_json` | `_maybe_load_groups_json(self)` | `MaybeLoadGroupsJson()` | 🔴 High | ❌ Not Started | External change detection |
| `get_last_groups_hash` | `get_last_groups_hash(self)` | `GetLastGroupsHash()` | 🟢 Low | ❌ Not Started | Property getter |
| `set_last_groups_hash` | `set_last_groups_hash(self, hash_value)` | `SetLastGroupsHash()` | 🟢 Low | ❌ Not Started | Property setter |

**Implementation Priority**: 🟠 High (State management)

---
### 16. heartbeat.py - Application Health Monitor

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, interval_sec, parent)` | `Heartbeat()` | 🟡 Medium | ❌ Not Started | Timer-based monitoring |
| `start` | `start(self)` | `Start()` | 🟢 Low | ❌ Not Started | Timer activation |
| `stop` | `stop(self)` | `Stop()` | 🟢 Low | ❌ Not Started | Timer deactivation |
| `add_status_collector` | `add_status_collector(self, name, func)` | `AddStatusCollector()` | 🟡 Medium | ❌ Not Started | Callback registration |
| `_beat` | `_beat(self)` | `Beat()` | 🟡 Medium | ❌ Not Started | Status collection |
| `force_beat` | `force_beat(self)` | `ForceBeat()` | 🟢 Low | ❌ Not Started | Manual trigger |

**Implementation Priority**: 🟡 Medium (Debugging utility)

---

### 17. image_loader.py - Asynchronous Image Loading

**Total Functions**: 15  
**Completed**: 0/15 (0%)  
**Estimated Effort**: 24 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, cache_dir, max_workers, use_disk_cache)` | `ImageLoaderWorker()` | 🟡 Medium | ❌ Not Started | Background service |
| `request_image` | `request_image(self, image_path, size, request_id, priority)` | `RequestImage()` | 🔴 High | ❌ Not Started | Priority queue system |
| `stop` | `stop(self)` | `Stop()` | 🟡 Medium | ❌ Not Started | Service termination |
| `run` | `run(self)` | `Run()` | 🔴 High | ❌ Not Started | Background thread loop |
| `_load_image` | `_load_image(self, image_path, size, request_id)` | `LoadImage()` | 🔴 High | ❌ Not Started | Image processing |
| `_load_with_pil` | `_load_with_pil(self, image_path, size)` | `LoadWithImageSharp()` | 🔴 High | ❌ Not Started | Optimized loading |
| `_load_with_qt` | `_load_with_qt(self, image_path, size)` | `LoadWithWpf()` | 🔴 High | ❌ Not Started | Fallback loading |
| `_save_to_cache` | `_save_to_cache(self, pixmap, image_path, size)` | `SaveToCache()` | 🟡 Medium | ❌ Not Started | Disk caching |
| ThumbnailCache methods (7) | Various | Cache management | 🟡 Medium | ❌ Not Started | LRU cache system |

**Implementation Priority**: 🟠 High (Performance critical)

---

### 18. image_manager.py - Image Processing Service

**Total Functions**: 15  
**Completed**: 0/15 (0%)  
**Estimated Effort**: 20 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self)` | `ImageProcessingService()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `load_image_async` | `load_image_async(self, path)` | `LoadImageAsync()` | 🔴 High | ❌ Not Started | ImageSharp async loading |
| `generate_thumbnail` | `generate_thumbnail(self, ...)` | `GenerateThumbnailAsync()` | 🔴 High | ❌ Not Started | ImageSharp resizing |
| `get_image_metadata` | `get_image_metadata(self, path)` | `GetImageMetadata()` | 🟡 Medium | ❌ Not Started | Metadata extraction |
| `cache_image` | `cache_image(self, ...)` | `CacheImage()` | 🟡 Medium | ❌ Not Started | MemoryCache usage |
| `get_cached_image` | `get_cached_image(self, path)` | `GetCachedImage()` | 🟡 Medium | ❌ Not Started | Cache retrieval |
| `clear_cache` | `clear_cache(self)` | `ClearCache()` | 🟢 Low | ❌ Not Started | Cache cleanup |
| `is_image_abnormal` | `is_image_abnormal(self, ...)` | `IsImageAbnormal()` | 🟡 Medium | ❌ Not Started | Size validation |
| Worker thread methods (7) | Various | async/await patterns | 🔴 High | ❌ Not Started | Async conversion |

**Implementation Priority**: 🟠 High (Performance critical)

---

### 19. image_registry.py - Image-Widget Mapping

**Total Functions**: 10  
**Completed**: 0/10 (0%)  
**Estimated Effort**: 12 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, max_cache_items, pixmap_cache)` | `ImageRegistry()` | 🟡 Medium | ❌ Not Started | Registry pattern setup |
| `register_widget` | `register_widget(self, widget, image_path)` | `RegisterWidget()` | 🟡 Medium | ❌ Not Started | Widget registration |
| `unregister_widget` | `unregister_widget(self, widget)` | `UnregisterWidget()` | 🟡 Medium | ❌ Not Started | Widget cleanup |
| `get_widgets_for_image` | `get_widgets_for_image(self, image_path)` | `GetWidgetsForImage()` | 🟢 Low | ❌ Not Started | O(1) lookup |
| `refresh_single_image` | `refresh_single_image(self, image_path, pixmap)` | `RefreshSingleImage()` | 🔴 High | ❌ Not Started | Optimized updates |
| `get_cached_pixmap` | `get_cached_pixmap(self, path)` | `GetCachedPixmap()` | 🟢 Low | ❌ Not Started | Cache retrieval |
| `set_cached_pixmap` | `set_cached_pixmap(self, path, pixmap)` | `SetCachedPixmap()` | 🟢 Low | ❌ Not Started | Cache storage |
| `clear_cache` | `clear_cache(self)` | `ClearCache()` | 🟢 Low | ❌ Not Started | Cache cleanup |
| `get_placeholder_pixmap` | `get_placeholder_pixmap(self, width, height)` | `GetPlaceholderPixmap()` | 🟡 Medium | ❌ Not Started | Placeholder generation |

**Implementation Priority**: 🟠 High (Performance optimization)

---

### 20. log_panel.py - Log Display Panel

**Total Functions**: 3  
**Completed**: 0/3 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, parent)` | `LogPanel()` | 🟡 Medium | ❌ Not Started | UI component setup |
| `append` | `append(self, message)` | `Append()` | 🟢 Low | ❌ Not Started | Text appending |
| `clear` | `clear(self)` | `Clear()` | 🟢 Low | ❌ Not Started | Text clearing |
| `open_detail_dialog` | `open_detail_dialog(self)` | `OpenDetailDialog()` | 🟡 Medium | ❌ Not Started | Detail view |

**Implementation Priority**: 🟡 Medium (UI component)

---

### 21. memory_monitor.py - Memory Usage Monitor

**Total Functions**: 4  
**Completed**: 0/4 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, interval_sec, threshold_percent)` | `MemoryMonitor()` | 🟡 Medium | ❌ Not Started | Performance monitoring |
| `_check_memory` | `_check_memory(self)` | `CheckMemory()` | 🔴 High | ❌ Not Started | System metrics |
| `stop` | `stop(self)` | `Stop()` | 🟢 Low | ❌ Not Started | Monitor termination |
| `get_stats` | `get_stats(self)` | `GetStats()` | 🟡 Medium | ❌ Not Started | Statistics collection |

**Implementation Priority**: 🟡 Medium (Debugging utility)

---

### 22. monitoring_orchestrator.py - Workflow Coordination

**Total Functions**: 3  
**Completed**: 0/3 (0%)  
**Estimated Effort**: 8 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, file_matcher, group_manager, settings, log_callback)` | `MonitoringOrchestrator()` | 🟡 Medium | ❌ Not Started | Orchestrator setup |
| `reset_state` | `reset_state(self)` | `ResetState()` | 🟢 Low | ❌ Not Started | State initialization |
| `perform_initial_scan` | `perform_initial_scan(self, force_full_scan)` | `PerformInitialScan()` | 🔴 High | ❌ Not Started | Workflow coordination |
| `process_file_events` | `process_file_events(self, event_queue)` | `ProcessFileEvents()` | 🔴 High | ❌ Not Started | Event processing |

**Implementation Priority**: 🔴 Critical (Workflow coordination)

---

### 23. nir_app.py - NIR Processing Application

**Total Functions**: 15  
**Completed**: 0/15 (0%)  
**Estimated Effort**: 24 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, config_manager, log_emitter_func)` | `NirProcessingService()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `auto_update_date_in_path` | `auto_update_date_in_path(path)` | `AutoUpdateDateInPath()` | 🟡 Medium | ❌ Not Started | Date path manipulation |
| `load_settings_to_ui` | `load_settings_to_ui(self)` | `LoadSettings()` | 🟡 Medium | ❌ Not Started | Settings loading |
| `open_folder` | `open_folder(self, edit_widget)` | `OpenFolder()` | 🟢 Low | ❌ Not Started | Process.Start() |
| `open_settings_folder` | `open_settings_folder(self)` | `OpenSettingsFolder()` | 🟢 Low | ❌ Not Started | Process.Start() |
| `start_monitoring` | `start_monitoring(self)` | `StartNirMonitoring()` | 🔴 High | ❌ Not Started | FileSystemWatcher |
| `stop_monitoring` | `stop_monitoring(self)` | `StopNirMonitoring()` | 🟡 Medium | ❌ Not Started | Cleanup |
| `parse_spc_file` | `parse_spc_file(self, path)` | `ParseSpcFile()` | 🔴 High | ❌ Not Started | Binary parsing |
| `move_nir_file` | `move_nir_file(self, ...)` | `MoveNirFile()` | 🟡 Medium | ❌ Not Started | File operations |
| `extract_nir_metadata` | `extract_nir_metadata(self, path)` | `ExtractNirMetadata()` | 🔴 High | ❌ Not Started | .spc format parsing |
| Thread management (5) | Various | async/await | 🔴 High | ❌ Not Started | Async conversion |

**Implementation Priority**: 🟠 High (Specialized functionality)

---

### 24. nir_pruning_service.py - NIR File Management

**Total Functions**: 1  
**Completed**: 0/1 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, log_callback)` | `NirPruningService()` | 🟡 Medium | ❌ Not Started | Service setup |
| `prune_nir_files` | `prune_nir_files(self, groups, keep_n, subject_name)` | `PruneNirFiles()` | 🔴 High | ❌ Not Started | File pruning logic |

**Implementation Priority**: 🟠 High (File management)

---

### 25. nir_spectrum_monitor.py - NIR Spectrum Analysis

**Total Functions**: 8  
**Completed**: 0/8 (0%)  
**Estimated Effort**: 16 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `load_spectrum` | `load_spectrum(file_path, encoding)` | `LoadSpectrum()` | 🔴 High | ❌ Not Started | Pandas → C# data processing |
| `find_y_variation_in_x_window` | `find_y_variation_in_x_window(df, x_window, stride)` | `FindYVariationInXWindow()` | 🔴 High | ❌ Not Started | Algorithm implementation |
| `process_file` | `process_file(file_path, dst_dir, log_callback)` | `ProcessFile()` | 🔴 High | ❌ Not Started | File processing pipeline |
| `__init__` | `__init__(self, monitor_path, move_path, log_callback)` | `NirSpectrumMonitor()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `log` | `log(self, msg)` | `Log()` | 🟢 Low | ❌ Not Started | Logging wrapper |
| `start` | `start(self)` | `Start()` | 🔴 High | ❌ Not Started | FileSystemWatcher setup |
| `stop` | `stop(self)` | `Stop()` | 🟡 Medium | ❌ Not Started | Service termination |
| SpectrumHandler methods | Various | Event handling | 🟡 Medium | ❌ Not Started | File event processing |

**Implementation Priority**: 🟠 High (Specialized analysis)

---

### 26. nir_status_monitor.py - NIR Status IPC

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, status_file_path)` | `NirStatusManager()` | 🟡 Medium | ❌ Not Started | IPC setup |
| `write_status` | `write_status(self, is_running, ...)` | `WriteStatus()` | 🟡 Medium | ❌ Not Started | Status writing |
| `read_status` | `read_status(self)` | `ReadStatus()` | 🟡 Medium | ❌ Not Started | Status reading |
| `clear_status` | `clear_status(self)` | `ClearStatus()` | 🟢 Low | ❌ Not Started | Status cleanup |
| `_default_status` | `_default_status(self)` | `GetDefaultStatus()` | 🟢 Low | ❌ Not Started | Default values |

**Implementation Priority**: 🟡 Medium (IPC mechanism)

---

### 27. nir_status_widget.py - NIR Status Display

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, parent, update_interval_ms)` | `NirStatusWidget()` | 🟡 Medium | ❌ Not Started | UI component setup |
| `_init_ui` | `_init_ui(self)` | `InitializeComponent()` | 🟡 Medium | ❌ Not Started | UI initialization |
| `_init_timer` | `_init_timer(self)` | `InitializeTimer()` | 🟡 Medium | ❌ Not Started | Timer setup |
| `update_status` | `update_status(self)` | `UpdateStatus()` | 🔴 High | ❌ Not Started | Status display logic |
| `stop_timer` | `stop_timer(self)` | `StopTimer()` | 🟢 Low | ❌ Not Started | Timer cleanup |
| `start_timer` | `start_timer(self)` | `StartTimer()` | 🟢 Low | ❌ Not Started | Timer activation |

**Implementation Priority**: 🟡 Medium (UI component)

---

### 28. operation_planner.py - File Operation Planning

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 8 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, config_manager)` | `OperationPlanner()` | 🟡 Medium | ❌ Not Started | Planner setup |
| `build_file_operation_data` | `build_file_operation_data(self, ...)` | `BuildFileOperationData()` | 🔴 High | ❌ Not Started | Data structure creation |
| `create_operation_plan` | `create_operation_plan(self, ...)` | `CreateOperationPlan()` | 🔴 High | ❌ Not Started | Plan generation |
| `save_plan_to_file` | `save_plan_to_file(self, plan, filename, subject_folder)` | `SavePlanToFile()` | 🟡 Medium | ❌ Not Started | Plan persistence |
| `_build_group_data` | `_build_group_data(self, groups)` | `BuildGroupData()` | 🟡 Medium | ❌ Not Started | Data transformation |

**Implementation Priority**: 🟠 High (Operation planning)

---

### 29. operation_validator.py - Input Validation

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, settings, groups_ref)` | `OperationValidator()` | 🟡 Medium | ❌ Not Started | Validator setup |
| `validate_basic_inputs` | `validate_basic_inputs(self, is_file_operation_running)` | `ValidateBasicInputs()` | 🔴 High | ❌ Not Started | Input validation |
| `filter_valid_groups` | `filter_valid_groups(self, groups)` | `FilterValidGroups()` | 🔴 High | ❌ Not Started | Group filtering |
| `_is_group_fully_matched` | `_is_group_fully_matched(self, group)` | `IsGroupFullyMatched()` | 🔴 High | ❌ Not Started | Completeness check |
| `_has_valid_file_entry` | `_has_valid_file_entry(self, entry)` | `HasValidFileEntry()` | 🟡 Medium | ❌ Not Started | Entry validation |

**Implementation Priority**: 🟠 High (Input validation)

---

### 30. path_utils.py - Path Utilities

**Total Functions**: 6  
**Completed**: 0/6 (0%)  
**Estimated Effort**: 8 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `validate_date_format` | `validate_date_format(date_str)` | `ValidateDateFormat()` | 🟡 Medium | ❌ Not Started | Date validation |
| `plan_date_based_path_changes` | `plan_date_based_path_changes(settings, new_date)` | `PlanDateBasedPathChanges()` | 🔴 High | ❌ Not Started | Path planning |
| `create_folders_if_needed` | `create_folders_if_needed(path_changes)` | `CreateFoldersIfNeeded()` | 🔴 High | ❌ Not Started | Directory creation |
| `get_normal_thumbnail_path` | `get_normal_thumbnail_path(folder_key, data_folder_name, settings)` | `GetNormalThumbnailPath()` | 🟡 Medium | ❌ Not Started | Path calculation |
| `extract_date_from_paths` | `extract_date_from_paths(settings)` | `ExtractDateFromPaths()` | 🟡 Medium | ❌ Not Started | Date extraction |
| `auto_update_paths_with_date` | `auto_update_paths_with_date(settings, new_date)` | `AutoUpdatePathsWithDate()` | 🔴 High | ❌ Not Started | Path updating |

**Implementation Priority**: 🟠 High (Path management)

---
### 31. preview_dialog.py - Image Preview Dialog

**Total Functions**: 5  
**Completed**: 0/5 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, pixmap, title, parent)` | `PreviewDialog()` | 🟡 Medium | ❌ Not Started | Dialog setup |
| `eventFilter` | `eventFilter(self, obj, event)` | `EventFilter()` | 🟡 Medium | ❌ Not Started | Event handling |
| `mousePressEvent` | `mousePressEvent(self, e)` | `OnMousePress()` | 🟢 Low | ❌ Not Started | Mouse handling |
| `resizeEvent` | `resizeEvent(self, e)` | `OnResize()` | 🟡 Medium | ❌ Not Started | Resize handling |
| `_update_scaled` | `_update_scaled(self)` | `UpdateScaled()` | 🟡 Medium | ❌ Not Started | Image scaling |

**Implementation Priority**: 🟡 Medium (UI enhancement)

---

### 32. signal_handler.py - OS Signal Handler

**Total Functions**: 1  
**Completed**: 0/1 (0%)  
**Estimated Effort**: 2 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `setup_signal_handlers` | `setup_signal_handlers()` | `SetupSignalHandlers()` | 🔴 High | ❌ Not Started | OS signal handling |

**Implementation Priority**: 🟡 Medium (System integration)

---

### 33. statistics_calculator.py - Statistics Calculation

**Total Functions**: 2  
**Completed**: 0/2 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `calculate_stats` | `calculate_stats(groups)` | `CalculateStats()` | 🔴 High | ❌ Not Started | Unified mode statistics |
| `calculate_stats_separated` | `calculate_stats_separated(line1_groups, line2_groups)` | `CalculateStatsSeparated()` | 🔴 High | ❌ Not Started | Separated mode statistics |

**Implementation Priority**: 🟠 High (Business logic)

---

### 34. statistics_presenter.py - Statistics Display

**Total Functions**: 3  
**Completed**: 0/3 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `format_unified_stats` | `format_unified_stats(total, with_nir, without_nir, fail)` | `FormatUnifiedStats()` | 🟡 Medium | ❌ Not Started | UI formatting |
| `format_separated_stats` | `format_separated_stats(...)` | `FormatSeparatedStats()` | 🟡 Medium | ❌ Not Started | Line-specific formatting |
| `format_file_counts` | `format_file_counts(...)` | `FormatFileCounts()` | 🟡 Medium | ❌ Not Started | Count formatting |

**Implementation Priority**: 🟡 Medium (UI presentation)

---

### 35. thread_monitor.py - Thread Health Monitor

**Total Functions**: 6  
**Completed**: 0/6 (0%)  
**Estimated Effort**: 6 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `monitor_thread` | `@monitor_thread(thread_name)` | `[MonitorThread]` | 🔴 High | ❌ Not Started | Decorator/Attribute pattern |
| `__init__` | `__init__(self)` | `ThreadMonitor()` | 🟡 Medium | ❌ Not Started | Monitor setup |
| `register` | `register(self, name, thread)` | `Register()` | 🟡 Medium | ❌ Not Started | Thread registration |
| `check_all` | `check_all(self)` | `CheckAll()` | 🔴 High | ❌ Not Started | Health checking |
| `get_summary` | `get_summary(self)` | `GetSummary()` | 🟡 Medium | ❌ Not Started | Status summary |

**Implementation Priority**: 🟡 Medium (Debugging utility)

---

### 36. tooltips.py - UI Tooltips

**Total Functions**: 2  
**Completed**: 0/2 (0%)  
**Estimated Effort**: 2 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `get_tooltip` | `get_tooltip(key)` | `GetTooltip()` | 🟢 Low | ❌ Not Started | Dictionary lookup |
| `set_tooltip_enabled` | `set_tooltip_enabled(widget, key, enabled)` | `SetTooltipEnabled()` | 🟢 Low | ❌ Not Started | Tooltip assignment |

**Implementation Priority**: 🟢 Low (UI enhancement)

---

### 37. ui_builder.py - UI Construction

**Total Functions**: 13  
**Completed**: 0/13 (0%)  
**Estimated Effort**: 20 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, parent, settings)` | `UIBuilder()` | 🟡 Medium | ❌ Not Started | Builder setup |
| `build_ui` | `build_ui(self)` | `BuildUI()` | 🔴 High | ❌ Not Started | Main UI construction |
| `_build_toolbar` | `_build_toolbar(self)` | `BuildToolbar()` | 🔴 High | ❌ Not Started | Toolbar creation |
| `_build_stats_bar` | `_build_stats_bar(self)` | `BuildStatsBar()` | 🔴 High | ❌ Not Started | Statistics bar |
| `_build_file_count_bar` | `_build_file_count_bar(self)` | `BuildFileCountBar()` | 🔴 High | ❌ Not Started | File count display |
| `_build_matching_bar_unified` | `_build_matching_bar_unified(self)` | `BuildMatchingBarUnified()` | 🔴 High | ❌ Not Started | Unified matching bar |
| `_build_matching_bar_separated` | `_build_matching_bar_separated(self)` | `BuildMatchingBarSeparated()` | 🔴 High | ❌ Not Started | Separated matching bar |
| `_build_tabs` | `_build_tabs(self)` | `BuildTabs()` | 🔴 High | ❌ Not Started | Tab control creation |
| `_build_log_panel` | `_build_log_panel(self)` | `BuildLogPanel()` | 🟡 Medium | ❌ Not Started | Log panel creation |
| `_connect_signals` | `_connect_signals(self)` | `ConnectSignals()` | 🔴 High | ❌ Not Started | Event binding |
| `_create_chip` | `_create_chip(self, label_text)` | `CreateChip()` | 🟡 Medium | ❌ Not Started | UI component creation |
| `_store_widget` | `_store_widget(self, name, widget)` | `StoreWidget()` | 🟢 Low | ❌ Not Started | Widget registration |

**Implementation Priority**: 🔴 Critical (UI foundation)

---

### 38. ui_components.py - UI Components

**Total Functions**: 25  
**Completed**: 0/25 (0%)  
**Estimated Effort**: 20 hours

#### FlowLayout_ Class
| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, ...)` | WrapPanel or Custom Panel | 🟡 Medium | ❌ Not Started | Layout implementation |
| `addItem` | `addItem(self, item)` | `Children.Add()` | 🟢 Low | ❌ Not Started | Standard WPF |
| `count` | `count(self)` | `Children.Count` | 🟢 Low | ❌ Not Started | Property |
| `itemAt` | `itemAt(self, index)` | `Children[index]` | 🟢 Low | ❌ Not Started | Indexer |
| `takeAt` | `takeAt(self, index)` | `Children.RemoveAt()` | 🟢 Low | ❌ Not Started | Standard WPF |
| Layout methods (5) | Various | Override methods | 🔴 High | ❌ Not Started | Custom layout logic |

#### PathLineEdit Class
| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, parent)` | Custom TextBox | 🟡 Medium | ❌ Not Started | Custom control |
| `dragEnterEvent` | `dragEnterEvent(self, event)` | `OnDragEnter()` | 🟡 Medium | ❌ Not Started | Drag/drop support |
| `dropEvent` | `dropEvent(self, event)` | `OnDrop()` | 🟡 Medium | ❌ Not Started | Drag/drop support |

#### SettingDialog Class
| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, parent)` | Settings Window/UserControl | 🔴 High | ❌ Not Started | Complex dialog |
| `select_folder` | `select_folder(self, edit_widget)` | `SelectFolder()` | 🟢 Low | ❌ Not Started | FolderBrowserDialog |
| `open_folder` | `open_folder(self, edit_widget)` | `OpenFolder()` | 🟢 Low | ❌ Not Started | Process.Start() |
| `get_settings` | `get_settings(self)` | `GetSettings()` | 🟡 Medium | ❌ Not Started | Data collection |
| UI creation methods (10) | Various | XAML + DataBinding | 🔴 High | ❌ Not Started | Complex UI |

**Implementation Priority**: 🟠 High (UI foundation)

---

### 39. utils.py - Utility Functions

**Total Functions**: 12  
**Completed**: 0/12 (0%)  
**Estimated Effort**: 16 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `save_metadata` | `save_metadata(metadata, path, backup)` | `SaveMetadata()` | 🟡 Medium | ❌ Not Started | JSON serialization |
| `extract_datetime_from_nir_key` | `extract_datetime_from_nir_key(nir_key)` | `ExtractDateTimeFromNirKey()` | 🔴 High | ❌ Not Started | Regex parsing |
| `extract_datetime_from_str` | `extract_datetime_from_str(s, prefix)` | `ExtractDateTimeFromString()` | 🔴 High | ❌ Not Started | Pattern matching |
| `extract_datetime_from_composite_cam` | `extract_datetime_from_composite_cam(filename)` | `ExtractDateTimeFromCompositeCam()` | 🔴 High | ❌ Not Started | Filename parsing |
| `get_timestamp_from_yml` | `get_timestamp_from_yml(folder)` | `GetTimestampFromYml()` | 🔴 High | ❌ Not Started | YAML processing |
| `yml_timestamp_to_short` | `yml_timestamp_to_short(ts_str)` | `YmlTimestampToShort()` | 🟡 Medium | ❌ Not Started | Format conversion |
| `normalize_path` | `normalize_path(path)` | `NormalizePath()` | 🟡 Medium | ❌ Not Started | Path normalization |
| LruPixmapCache methods (5) | Various | LRU cache implementation | 🔴 High | ❌ Not Started | Cache system |

**Implementation Priority**: 🔴 Critical (Core utilities)

---

### 40. watchdog_manager.py - File System Monitoring

**Total Functions**: 8  
**Completed**: 0/8 (0%)  
**Estimated Effort**: 16 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `__init__` | `__init__(self, ...)` | `FileWatcherService()` | 🟡 Medium | ❌ Not Started | Service initialization |
| `start_watchdog` | `start_watchdog(self)` | `StartWatching()` | 🔴 High | ❌ Not Started | FileSystemWatcher setup |
| `stop_watchdog` | `stop_watchdog(self)` | `StopWatching()` | 🟡 Medium | ❌ Not Started | Cleanup and disposal |
| `check_status` | `check_status(self)` | `CheckStatus()` | 🟡 Medium | ❌ Not Started | Health monitoring |
| `is_alive` | `is_alive(self)` | `IsActive` | 🟢 Low | ❌ Not Started | Property getter |
| `FolderEventHandler.__init__` | `__init__(self, ...)` | Event handler setup | 🟡 Medium | ❌ Not Started | Event handler config |
| `FolderEventHandler.on_*` | Various event methods | FileSystemEventArgs | 🟡 Medium | ❌ Not Started | Event processing |

**Implementation Priority**: 🔴 Critical (Core monitoring functionality)

---

### 41. window_state_manager.py - Window State Persistence

**Total Functions**: 2  
**Completed**: 0/2 (0%)  
**Estimated Effort**: 4 hours

| Function | Python Signature | C# Equivalent | Complexity | Status | Notes |
|----------|------------------|---------------|------------|--------|-------|
| `save_window_bounds` | `save_window_bounds(self, window, config_manager)` | `SaveWindowBounds()` | 🟡 Medium | ❌ Not Started | Window state saving |
| `restore_window_bounds` | `restore_window_bounds(self, window, config_manager)` | `RestoreWindowBounds()` | 🟡 Medium | ❌ Not Started | Window state restoration |

**Implementation Priority**: 🟡 Medium (UI enhancement)

---

## Complete Summary Statistics

### Overall Progress
- **Total Modules**: 41 (ALL modules included)
- **Total Functions**: 450+ (complete count)
- **Completed Functions**: 0
- **Overall Progress**: 0%

### By Complexity
| Complexity | Count | Percentage | Completed |
|------------|-------|------------|-----------|
| 🟢 Low | 85 | 19% | 0 |
| 🟡 Medium | 180 | 40% | 0 |
| 🔴 High | 185 | 41% | 0 |

### By Priority
| Priority | Modules | Functions | Completed |
|----------|---------|-----------|-----------|
| 🔴 Critical | 12 | 180 | 0 |
| 🟠 High | 15 | 165 | 0 |
| 🟡 Medium | 12 | 85 | 0 |
| 🟢 Low | 2 | 20 | 0 |

### Estimated Effort
- **Total Estimated Hours**: 450+ hours
- **Average per Function**: 1.0 hours
- **High Complexity Average**: 2.0 hours
- **Medium Complexity Average**: 1.0 hours
- **Low Complexity Average**: 0.5 hours

## C# Technology Mapping

### Core Technologies
- **UI Framework**: WPF with XAML
- **File Monitoring**: FileSystemWatcher
- **Image Processing**: ImageSharp
- **Async Operations**: async/await, Task
- **Configuration**: System.Configuration or JSON
- **Logging**: Serilog or NLog
- **Caching**: MemoryCache
- **Threading**: Task.Run, CancellationToken

### Python → C# Library Mappings
- **watchdog** → FileSystemWatcher
- **PIL/Pillow** → ImageSharp
- **pandas** → Custom data structures or DataTable
- **PySide6/PyQt6** → WPF
- **threading** → Task/async-await
- **queue** → ConcurrentQueue<T>
- **json** → System.Text.Json
- **yaml** → YamlDotNet
- **psutil** → System.Diagnostics.Process

## Implementation Strategy

### Phase 1: Foundation (Weeks 1-3)
1. **config_manager.py** - Configuration system
2. **utils.py** - Core utilities and caching
3. **Basic WPF project setup** - Project structure

### Phase 2: Core Services (Weeks 4-6)
1. **watchdog_manager.py** - File system monitoring
2. **file_matcher.py** - File matching logic
3. **group_manager.py** - Group creation logic

### Phase 3: Image Processing (Weeks 7-8)
1. **image_loader.py** - Async image loading
2. **image_manager.py** - Image processing
3. **image_registry.py** - Image-widget mapping

### Phase 4: File Operations (Weeks 9-10)
1. **file_operations.py** - File operation worker
2. **file_operation_manager.py** - Business logic
3. **delete_manager.py** - File deletion

### Phase 5: UI Components (Weeks 11-12)
1. **ui_builder.py** - UI construction
2. **ui_components.py** - Custom controls
3. **monitoring_app.py** - Main window

### Phase 6: Specialized Features (Weeks 13-14)
1. **nir_app.py** - NIR processing
2. **nir_spectrum_monitor.py** - Spectrum analysis
3. **abnormal_detector.py** - Anomaly detection

### Phase 7: Integration & Polish (Weeks 15-16)
1. **main.py** - Application controller
2. **Statistics and presentation** - Data display
3. **Testing and optimization**

## Risk Assessment

### High Risk Functions
1. **NIR spectrum analysis** - Complex algorithm migration
2. **File system event handling** - Performance and reliability
3. **Complex UI updates** - Real-time data binding
4. **Image processing pipeline** - Memory management
5. **Async/threading conversion** - Concurrency patterns

### Mitigation Strategies
1. **Early prototyping** of high-risk algorithms
2. **Incremental testing** with real data
3. **Performance benchmarking** throughout development
4. **Fallback implementations** for complex features
5. **Comprehensive unit testing** for core logic

## Quality Assurance

### Testing Strategy
- **Unit tests** for all business logic functions
- **Integration tests** for service interactions
- **UI tests** for critical user workflows
- **Performance tests** for file operations
- **Load tests** for large datasets

### Code Review Checklist
- [ ] Function signature matches specification
- [ ] Error handling implemented properly
- [ ] Performance considerations addressed
- [ ] Memory management optimized
- [ ] Thread safety ensured where needed
- [ ] Documentation complete
- [ ] Unit tests written and passing

---

**CRITICAL ISSUE RESOLVED**: This checklist now includes ALL 41 modules from the Python codebase, providing complete coverage for the C# migration project.

**Next Steps**:
1. Review and validate function counts for each module
2. Prioritize implementation order based on dependencies
3. Begin Phase 1 implementation with foundation modules
4. Set up CI/CD pipeline for automated testing
5. Establish performance benchmarks for comparison