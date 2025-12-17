---
Task: Configurable Data Sequence Settings
Created: 2025-12-15
Status: Draft
Summary: Allow users to configure data arrival sequence, time tolerances, and UI column order
---

# Configurable Data Sequence Settings - Requirements

## 1. Goal

### Primary Goal
Enable users to configure the expected data arrival sequence (NIR, Normal, Cameras) and related settings to support different production workflows.

### Success Criteria
- [ ] Users can specify which data type arrives first/last
- [ ] Users can configure time tolerance between data types
- [ ] UI column order matches configured data sequence
- [ ] Settings are persisted across application restarts
- [ ] Real-time matching uses configured sequence and tolerances

## 2. User Requirements

### 2.1 Data Sequence Configuration

**User Story**: 
> "As a production line operator, I need to specify that NIR data arrives before Normal data in my workflow, so the system correctly groups files that arrive at different times."

**Requirements**:
1. **Sequence Editor**:
   - Drag-and-drop interface to reorder data types
   - Data types: NIR, Normal, Cam1, Cam2, Cam3, Cam4, Cam5, Cam6
   - Default order: Normal → NIR → Cam1 → Cam2 → Cam3

2. **Per-Type Settings**:
   - **Expected Delay**: Time after previous type (seconds)
     - Example: Normal arrives at T=0, NIR arrives at T=1s, Cam1 at T=5s
   - **Time Tolerance**: Allowed deviation (±seconds)
     - Example: NIR tolerance ±10s means accept files from T-10s to T+10s

3. **Presets**:
   - "NIR First" (NIR → Normal → Cameras)
   - "Normal First" (Normal → NIR → Cameras)
   - "Cameras First" (Cameras → Normal → NIR)
   - "Custom"

### 2.2 UI Column Order

**User Story**:
> "As a quality inspector, I want the monitoring table columns to match the actual data arrival order, so I can quickly see the production sequence."

**Requirements**:
1. **Auto-Sync**:
   - UI column order mirrors configured data sequence
   - Changes to sequence immediately update UI layout

2. **Column Visibility**:
   - Hide columns for unused data types
   - Example: If no Cam4-6, hide those columns

### 2.3 Settings Persistence

**Requirements**:
1. Settings saved to `appsettings.json`
2. Loaded on application startup
3. Validation on load (reject invalid sequences)

## 3. Technical Requirements

### 3.1 Configuration Model

```json
{
  "DataSequenceSettings": {
    "Sequence": [
      {
        "DataType": "Normal",
        "Order": 1,
        "ExpectedDelaySeconds": 0,
        "TimeToleranceSeconds": 5
      },
      {
        "DataType": "NIR",
        "Order": 2,
        "ExpectedDelaySeconds": 1,
        "TimeToleranceSeconds": 10
      },
      {
        "DataType": "Cam1",
        "Order": 3,
        "ExpectedDelaySeconds": 5,
        "TimeToleranceSeconds": 15
      }
    ],
    "EnabledDataTypes": ["Normal", "NIR", "Cam1", "Cam2", "Cam3"]
  }
}
```

### 3.2 Settings Dialog Tab

**New Tab**: "Data Sequence" (데이터 순서)

**UI Elements**:
1. **Sequence List** (Reorderable):
   - Item format: `[Icon] DataType | Expected Delay: Xs | Tolerance: ±Ys`
   - Drag handle for reordering
   - Edit button to change delays/tolerances

2. **Preset Dropdown**:
   - "NIR First", "Normal First", etc.
   - Apply button

3. **Per-Item Settings Dialog**:
   - Expected Delay: `[____] seconds`
   - Time Tolerance: `± [____] seconds`
   - Enabled checkbox

4. **Preview Panel**:
   - Shows example timeline
   - Visual representation of tolerances

### 3.3 Real-time Matching Logic

**Update FindMatchingExistingGroup**:
1. Use configured sequence to determine matching priority
2. Apply per-type time tolerances
3. Match earlier-arriving types first

**Example**:
```
Configuration: NIR (T=0±10s) → Normal (T=1±5s) → Cam1 (T=5±15s)

File arrives: Normal at T=10:30:15
  1. Search for NIR groups (T=10:30:05 to 10:30:25) ← Within tolerance
  2. If found, merge into NIR group
  3. If not found, create new group
```

### 3.4 UI Column Order

**DataGrid Columns**:
- Generated dynamically from `DataSequenceSettings.Sequence`
- Order matches configured sequence
- Hidden columns for disabled data types

## 4. UI/UX Specifications

### 4.1 Settings Dialog Layout

```
┌───────────────────────────────────────────────────────────┐
│ Settings                                        [×]        │
├─────────────┬─────────────────────────────────────────────┤
│ General     │ Data Sequence Configuration                 │
│ Paths       │                                             │
│ Matching    │ Preset: [NIR First ▼] [Apply]              │
│ UI Options  │                                             │
│► Data       │ ┌─────────────────────────────────────┐   │
│  Sequence   │ │ [≡] NIR        | Delay: 0s  | ±10s │   │
│             │ │ [≡] Normal     | Delay: 1s  | ±5s  │   │
│             │ │ [≡] Cam1       | Delay: 5s  | ±15s │   │
│             │ │ [≡] Cam2       | Delay: 6s  | ±15s │   │
│             │ │ [≡] Cam3       | Delay: 7s  | ±15s │   │
│             │ └─────────────────────────────────────┘   │
│             │                                             │
│             │ Preview:                                    │
│             │ ─NIR──────Normal───Cam1──Cam2──Cam3─→      │
│             │ 0s        1s       5s    6s    7s    time  │
│             │ ◄10s►     ◄5s►     ◄──15s──►                │
│             │                                             │
│             │              [Reset] [Save] [Cancel]        │
└─────────────┴─────────────────────────────────────────────┘
```

### 4.2 Main Window Column Order

**Before** (Fixed order):
```
| Main Img | NIR Graph | Cam1 | Cam2 | Cam3 |
```

**After** (Configurable):
```
If sequence: NIR → Normal → Cam1 → Cam2 → Cam3
| NIR Graph | Main Img | Cam1 | Cam2 | Cam3 |
```

## 5. Constraints

### Technical Constraints
- Maximum 10 data types in sequence
- Minimum time tolerance: 1 second
- Maximum time tolerance: 300 seconds (5 minutes)
- Delays must be >= 0

### Non-Goals (Out of Scope)
- Per-line sequence configuration (all lines use same sequence)
- ML-based sequence detection
- Automatic tolerance adjustment

## 6. Edge Cases

| Scenario | Handling |
|----------|----------|
| All delays = 0 | Treat as simultaneous arrival, use narrow tolerances |
| Delay > tolerance | WARNING: "Delay exceeds tolerance, files may not match" |
| Duplicate data types | ERROR: "Each data type can appear only once" |
| Empty sequence | ERROR: "At least one data type required" |
| Files arrive out of order | Match using tolerance windows |
| Negative delays | ERROR: "Delays must be >= 0" |

## 7. Implementation Phases

### Phase 1: Configuration Model
- [ ] Create `DataSequenceSettings` class
- [ ] Add to `ApplicationConfiguration`
- [ ] Default configuration
- [ ] Validation logic

### Phase 2: Settings Dialog
- [ ] Add "Data Sequence" tab
- [ ] Reorderable list control
- [ ] Per-item edit dialog
- [ ] Preset dropdown
- [ ] Preview panel

### Phase 3: Real-time Matching
- [ ] Update `FindMatchingExistingGroup` to use sequence
- [ ] Apply per-type tolerances
- [ ] Priority-based matching

### Phase 4: UI Column Order
- [ ] Dynamic column generation
- [ ] Column order from sequence
- [ ] Column visibility

### Phase 5: Testing
- [ ] Test all presets
- [ ] Test custom sequences
- [ ] Test time tolerance edge cases
- [ ] UI responsiveness

## 8. Open Questions

- [x] Should tolerances be per-type or global? → **Per-type** (more flexible)
- [ ] Should sequence be per-line or global? → **TBD** (global for now)
- [ ] Should UI show expected vs actual delays? → **TBD** (future enhancement)
- [ ] Auto-detect sequence from actual data? → **TBD** (future enhancement)

---

**Status**: [ ] Ready for approval
**Next Step**: Create detailed design document
