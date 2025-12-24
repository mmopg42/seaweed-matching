---
Task: File Count Display Fix
Created: 2025-12-18
Status: Draft
Depends On: 01_requirements.md, 02_research.md
---

# File Count Display Fix - Implementation Plan

## 0. Requirements Traceability

| Success Criterion | Addressed By | Verification Method |
|-------------------|--------------|---------------------|
| NIR file counts (NIR1) display correctly | UI already exists, config fixed | Manual test: Start monitoring, verify NIR1 count appears |
| Normal folder counts (Normal1) display correctly | UI already exists, config fixed | Manual test: Start monitoring, verify Normal1 count appears |
| Camera file counts (Cam1, Cam2, Cam3) display correctly | UI already exists | Manual test: Start monitoring, verify Cam counts appear |
| Counts update within 3 seconds | StatisticsService already implements 2s polling | Manual test: Add new file, verify count updates within 3s |
| Counts reflect actual file system state | Config paths now correct | Manual test: Compare displayed count with actual file count |
| UI remains responsive during count updates | Background threading already implemented | Manual test: Monitor should not freeze UI |
| Counts reset to 0 when monitoring is stopped | Already implemented in ExecuteStopAsync | Manual test: Stop monitoring, verify counts reset |
| Counts refresh correctly when "Refresh" button is clicked | Refresh command already exists | Manual test: Click Refresh, verify counts update |
| **Line 2 counts display (NIR2, Normal2, Cam4-6)** | **MISSING - needs UI addition** | **Manual test after implementation** |

> **Critical Finding**: Line 2 file counts (NIR2, Normal2, Cam4-6) are NOT currently displayed in UI despite backend support existing.

---

## 1. Architecture Overview

### 1.1 System Context

The file count display system shows real-time statistics of files detected in monitored directories. The backend (`StatisticsService`) counts files every 2 seconds and raises events, which the UI (`MainWindowViewModel`) receives and displays in a statistics bar.

**Current State**:
- ✅ Backend supports Line 1 (NIR1, Normal1, Cam1-3)
- ✅ Backend supports Line 2 (NIR2, Normal2, Cam4-6)
- ✅ UI displays Line 1 counts
- ❌ UI does NOT display Line 2 counts

**Goal**: Add UI elements to display Line 2 counts.

### 1.2 Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                      ChronoView UI                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────────┐                              │
│  │  MainWindow.xaml     │  ← ADD Line 2 count chips    │
│  │  (File Count Bar)    │                              │
│  └──────────────────────┘                              │
│           ▲                                             │
│           │ Binding                                     │
│  ┌──────────────────────┐                              │
│  │MainWindowViewModel   │  ← Properties already exist  │
│  │  - Nir2Count         │                              │
│  │  - Normal2Count      │                              │
│  │  - Cam4-6Count       │                              │
│  └──────────────────────┘                              │
│           ▲                                             │
│           │ FileCountsUpdated event                     │
└───────────┼─────────────────────────────────────────────┘
            │
┌───────────┼─────────────────────────────────────────────┐
│           │           Backend Services                  │
├───────────┼─────────────────────────────────────────────┤
│  ┌──────────────────────┐                              │
│  │ StatisticsService    │  ← Already counts Line 2     │
│  │  - MonitorFileCountsAsync()                         │
│  │  - GetFileCountsAsync()                             │
│  └──────────────────────┘                              │
│           │                                             │
│           ▼ File system enumeration                     │
│  [ config.json paths ]                                  │
│     - nir2Path, normal2Path, camera4-6Path             │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 1.3 Data Flow

```
[File System]
    │
    ▼
StatisticsService.MonitorFileCountsAsync() (2s loop)
    │
    ▼
GetFileCountsAsync() - counts files/directories
    │
    ├─► CountFilesInDirectoryAsync(nir1Path) → NirCount
    ├─► CountFilesInDirectoryAsync(nir2Path) → Nir2Count
    ├─► CountDirectoriesInDirectoryAsync(normal1Path) → NormalCount
    ├─► CountDirectoriesInDirectoryAsync(normal2Path) → Normal2Count
    ├─► CountFilesInDirectoryAsync(camera1-6Paths) → Cam1-6Count
    │
    ▼
FileCountsUpdated event raised (if changed)
    │
    ▼
MainWindowViewModel.OnFileCountsUpdated()
    │
    ├─► NirCount = stats.NirCount
    ├─► Nir2Count = stats.Nir2Count  ← Already implemented!
    ├─► NormalCount = stats.NormalCount
    ├─► Normal2Count = stats.Normal2Count  ← Already implemented!
    ├─► Cam1-6Count = stats.Cam1-6Count
    │
    ▼
UI Binding updates (WPF data binding)
    │
    ├─► {Binding NirCount} → TextBlock (exists)
    ├─► {Binding Nir2Count} → TextBlock (MISSING - needs to be added)
    ├─► {Binding NormalCount} → TextBlock (exists)
    ├─► {Binding Normal2Count} → TextBlock (MISSING - needs to be added)
    ├─► {Binding Cam4-6Count} → TextBlocks (MISSING - needs to be added)
    │
    ▼
[User sees counts in statistics bar]
```

---

## 2. Components

### 2.1 New Components

**None** - All backend components already exist and work correctly.

### 2.2 Modified Components

| Component | Location | Changes | Breaking Change? |
|-----------|----------|---------|------------------|
| `MainWindow.xaml` | `ChronoView/MainWindow.xaml` | Add UI chips for Line 2 counts (NIR2, Normal2, Cam4-6) after existing Line 1 chips | No |

### 2.3 Deleted Components

**None**

---

## 3. Implementation Details

### 3.1 MainWindow.xaml Changes

**Current UI** (lines 167-222):
- Shows: NIR1, Normal1, Cam1, Cam2, Cam3
- Missing: NIR2, Normal2, Cam4, Cam5, Cam6

**Change Required**:
Add 5 additional `<Border Style="{StaticResource ChipStyle}">` blocks after line 220:

```xml
<!-- Existing Line 1 counts (lines 176-220) -->
<Border Style="{StaticResource ChipStyle}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Foreground="#6c757d">
            <Run Text="{x:Static res:Strings.Label_NIR1}"/>
            <Run Text=":"/>
        </TextBlock>
        <TextBlock Text="{Binding NirCount}" FontWeight="Bold" Margin="5,0,0,0"/>
    </StackPanel>
</Border>
<!-- ... (Normal1, Cam1, Cam2, Cam3) ... -->

<!-- NEW: Line 2 counts (to be added after line 220) -->
<Border Style="{StaticResource ChipStyle}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Foreground="#6c757d">
            <Run Text="{x:Static res:Strings.Label_NIR2}"/>
            <Run Text=":"/>
        </TextBlock>
        <TextBlock Text="{Binding Nir2Count}" FontWeight="Bold" Margin="5,0,0,0"/>
    </StackPanel>
</Border>

<Border Style="{StaticResource ChipStyle}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Foreground="#6c757d">
            <Run Text="{x:Static res:Strings.Label_Normal2}"/>
            <Run Text=":"/>
        </TextBlock>
        <TextBlock Text="{Binding Normal2Count}" FontWeight="Bold" Margin="5,0,0,0"/>
    </StackPanel>
</Border>

<!-- (Cam4, Cam5, Cam6 similar pattern) -->
```

**No code-behind changes needed** - all ViewModel properties already exist.

---

## 4. Key Design Decisions

### 4.1 Backend vs Frontend Fix

**Context**: The problem could be fixed in backend or frontend.

| Option | Approach | Pros | Cons |
|--------|----------|------|------|
| Option A: Fix backend | Modify StatisticsService counting logic | N/A | Backend already works correctly! |
| Option B: Fix config | Correct swapped paths in config.json | Quick, no code change | Already done by user |
| Option C: Add UI | Add missing UI elements for Line 2 | Shows all counts | Requires XAML changes |

**Decision**: Option C (Add UI for Line 2)

**Rationale**: 
- Backend is already correct and counts Line 2 files
- Config paths are now correct (user fixed)
- Only missing piece is UI display for Line 2 counts

### 4.2 UI Layout: Separate Lines vs Unified

**Context**: How to visually organize Line 1 and Line 2 counts?

| Option | Layout | Pros | Cons |
|--------|--------|------|------|
| Option A: All in one row | Single WrapPanel with all 10 counts | Simple | May wrap awkwardly |
| Option B: Two separate rows | Line 1 row + Line 2 row | Clear separation | More vertical space |
| Option C: Add separator | Keep existing + add visual separator + Line 2 | Best of both | Slightly more complex |

**Decision**: Option A (All in one row)

**Rationale**: 
- Existing ChipStyle already handles wrapping gracefully
- Consistent with current design
- Minimal code change

**Note**: If user prefers separated layout, can change later.

---

## 5. Configuration

**No configuration changes needed** - all paths already exist in `matchingSettings`:

| Existing Config Key | Purpose | Already Used |
|---------------------|---------|--------------|
| `nir2Path` | NIR files for Line 2 | ✅ Backend counts |
| `normal2Path` | Normal folders for Line 2 | ✅ Backend counts |
| `camera4Path` | Camera 4 images | ✅ Backend counts |
| `camera5Path` | Camera 5 images | ✅ Backend counts |
| `camera6Path` | Camera 6 images | ✅ Backend counts |

---

## 6. External Dependencies

**None** - This is a pure UI change using existing WPF databinding.

---

## 7. Glossary Updates

### Existing Terms Check

| Checked | Existing Term | Relevance |
|---------|---------------|-----------|
| [x] | `StatisticsService` | Backend service that counts files |
| [x] | `FileCountStatistics` | Model containing all count values |
| [x] | `MainWindowViewModel` | ViewModel with count properties |

### New Terms to Add

**None** - All terms already exist in glossary.

---

## 8. Architecture Documentation Plan

### Documents to Update

| Document | Changes |
|----------|---------|
| `module_configuration.md` | Already created - documents config paths and common errors |
| `trouble/file_count_path_swap_issue.md` | Already created - documents config path swap issue |

**No new documents needed** - Issue was configuration, not architectural.

---

## 9. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Missing string resources (`Label_NIR2`, etc.) | Medium | Medium | Check Strings.resx before implementation, add if missing |
| UI layout breaks on small screens | Low | Low | Test on different window sizes |
| User has empty Line 2 paths in config | Medium | Low | Counts will show 0 (expected behavior) |

---

## 10. Verification Plan

### 10.1 Manual Testing Steps

**Test 1: Line 1 Counts (Regression Test)**
1. Open ChronoView
2. Click "Start" button
3. Wait 3 seconds
4. **Verify**: NIR1, Normal1, Cam1-3 counts appear and are non-zero (if files exist)

**Test 2: Line 2 Counts (New Feature)**
1. Ensure `nir2Path`, `normal2Path`, `camera4-6Paths` are set in config
2. Ensure actual files/folders exist in those paths
3. Open ChronoView
4. Click "Start" button
5. Wait 3 seconds
6. **Verify**: NIR2, Normal2, Cam4-6 counts appear and match actual file counts

**Test 3: Empty Paths**
1. Set `nir2Path=""` in config (empty)
2. Open ChronoView
3. Click "Start" button
4. **Verify**: NIR2 count shows 0 (not error)

**Test 4: Count Updates**
1. Start monitoring
2. Add a new NIR file to `nir2Path`
3. Wait 3 seconds
4. **Verify**: NIR2 count increments by 1

**Test 5: Stop/Refresh**
1. Start monitoring (counts appear)
2. Click "Stop"
3. **Verify**: All counts reset to 0
4. Click "Start" again
5. **Verify**: Counts reappear

### 10.2 String Resource Verification

**Before implementing**, verify string resources exist:

```powershell
# Check if Label_NIR2, Label_Normal2, Label_Cam4-6 exist in Strings.resx
Get-Content ChronoView/Resources/Strings.resx | Select-String "Label_NIR2"
Get-Content ChronoView/Resources/Strings.resx | Select-String "Label_Normal2"
Get-Content ChronoView/Resources/Strings.resx | Select-String "Label_Cam4"
```

**If missing**, add to `Strings.resx` (can reuse existing patterns):
- `Label_NIR2` = "NIR2" (or localized equivalent)
- `Label_Normal2` = "Normal2"
- `Label_Cam4` = "Cam4"
- `Label_Cam5` = "Cam5"
- `Label_Cam6` = "Cam6"

---

## 11. Open Questions

- [x] Is backend counting Line 2 correctly? → YES, verified in research
- [x] Do ViewModel properties exist? → YES, all 10 count properties exist
- [x] Are config paths correct? → YES, user fixed the swapped paths
- [ ] Do string resources exist for Line 2 labels? → **Needs verification before implementation**

---

## Approval

- [x] All requirements traced to components (Section 0)
- [x] Component interfaces reviewed (no changes needed)
- [x] Design decisions documented with rationale (Section 4)
- [x] Verification plan is clear and executable (Section 10)
- [ ] String resources verified (blocker - must check before implementation)

---

이 Plan 문서를 검토해 주세요.
승인하시면 다음 단계로 진행하여 UI 구현하겠습니다.
수정이 필요하면 말씀해 주세요.
