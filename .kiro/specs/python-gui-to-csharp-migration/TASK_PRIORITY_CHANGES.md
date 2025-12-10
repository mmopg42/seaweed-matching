# Task Priority Reorganization

## Summary of Changes

The task order has been reorganized to prioritize **core grouping functionality** and **UI display** over **NIR spectrum processing**, which is a less critical advanced feature.

## New Task Order (Tasks 4-12)

### ✅ Completed (Tasks 1-3)
- [x] 1. Project Setup and Core Infrastructure
- [x] 2. Configuration Management System  
- [x] 3. Data Models and Core Entities

### 🔥 HIGH PRIORITY - Core Functionality

#### Task 4: File Group Matching Service (MOVED UP from Task 7)
**Why First**: This is the **core business logic** that groups NIR files with camera images based on timestamps.
- Time-based correlation algorithms
- Configurable time windows
- Abnormal condition detection
- Multi-line mode support
- **Dependencies**: Requires Task 3 (Data Models)
- **Enables**: Tasks 5, 9, 10 (Image display, ViewModels, UI)

#### Task 5: Image Processing Service (KEPT at Task 5)
**Why Second**: Required for **displaying thumbnails** in the UI table.
- Thumbnail generation (200x150)
- LRU cache for performance
- Async image loading
- Metadata extraction
- **Dependencies**: Requires Task 3 (Data Models)
- **Enables**: Task 10 (UI Implementation)

#### Task 6: File System Monitoring Service (MOVED UP from Task 4)
**Why Third**: Monitors file changes and triggers grouping.
- FileSystemWatcher implementation
- Event buffering and aggregation
- Integration with File Group Matching
- **Dependencies**: Requires Task 4 (Grouping Service)
- **Enables**: Real-time file detection

### 📊 MEDIUM PRIORITY - Supporting Services

#### Task 7: Statistics and Analytics Service (MOVED UP from Task 8)
- Real-time file count monitoring
- Match rate calculations
- Performance metrics
- **Dependencies**: Requires Task 4 (Grouping Service)

#### Task 8: Checkpoint - Core Services Integration Test (MOVED UP from Task 9)
- Verify grouping, image processing, and statistics work together
- Integration testing

### 🎨 HIGH PRIORITY - User Interface

#### Task 9: MVVM ViewModels Implementation (MOVED UP from Task 10)
**Why Before UI**: ViewModels are required for data binding.
- MainWindowViewModel
- FileGroupViewModel
- Observable collections
- Commands (Start, Stop, Move, Delete)
- **Dependencies**: Requires Tasks 4, 5, 7 (Services)
- **Enables**: Task 10 (UI Implementation)

#### Task 10: WPF User Interface Implementation (MOVED UP from Task 11)
**Why High Priority**: Main visual interface for users.
- DataGrid with file groups
- Thumbnail display
- Sidebar panels
- Message log
- Toolbar and status bar
- **Dependencies**: Requires Tasks 5, 9 (Image Processing, ViewModels)
- **Reference Design**: C#_project/gui_c/components/chrono-view-pro.tsx

### 📁 MEDIUM PRIORITY - File Operations

#### Task 11: File Operations Service (MOVED UP from Task 12)
- File move/copy with progress
- Rollback capabilities
- Async operations
- **Dependencies**: Requires Task 4 (Grouping Service)

### 🔬 LOWER PRIORITY - Advanced Features

#### Task 12: NIR Processing Service (MOVED DOWN from Task 6)
**Why Later**: NIR spectrum analysis is **less critical** than core grouping.
- Spectrum file parsing
- Y variation detection (0.05 ≤ range ≤ 0.1)
- Sliding window analysis
- **Note**: This is an advanced quality control feature, not required for basic functionality
- **Dependencies**: Requires Task 4 (Grouping Service)

### 📋 REMAINING TASKS (13-17)
- Task 13: Migration Tracking System
- Task 14: Integration and System Testing
- Task 15: Error Handling and Logging Enhancement
- Task 16: Documentation and Deployment Preparation
- Task 17: Final Checkpoint - Complete System Validation

## Rationale

### Core Grouping First
The **File Group Matching Service** (Task 4) is the heart of the application. It:
- Matches NIR files with camera images by timestamp
- Creates the FileGroup objects that populate the UI table
- Enables all downstream functionality

Without grouping, there's nothing to display in the UI.

### Image Display Second
The **Image Processing Service** (Task 5) is critical for the UI because:
- Users need to see thumbnail previews in the table
- Visual feedback is essential for file verification
- Image loading must be async to keep UI responsive

### NIR Processing Later
The **NIR Processing Service** (Task 12) is moved to lower priority because:
- It's a quality control feature (Y variation detection)
- Not required for basic file grouping and display
- Can be added after core functionality is working
- More complex algorithm (sliding window analysis)

### UI Implementation Earlier
Moving **ViewModels** (Task 9) and **UI** (Task 10) earlier allows:
- Faster visual feedback during development
- Earlier user testing and validation
- Incremental feature addition to working UI

## Implementation Strategy

1. **Phase 1** (Tasks 4-6): Core grouping and monitoring
2. **Phase 2** (Tasks 7-8): Statistics and integration testing
3. **Phase 3** (Tasks 9-10): UI implementation with working backend
4. **Phase 4** (Tasks 11-12): File operations and advanced NIR features
5. **Phase 5** (Tasks 13-17): Testing, documentation, deployment

This approach delivers a **working, visible application** faster while deferring advanced features.
