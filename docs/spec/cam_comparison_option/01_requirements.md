---
Task: cam_comparison_option
Created: 2026-01-14
Status: Draft
Summary: Add option to compare Cam2/3 (Line 1) and Cam5/6 (Line 2) timestamps directly against Cam1/4
Research Required: Yes
---

# Camera Comparison Option - Requirements

## 1. Goal

### 1.1 Primary Goal

Enable users to configure Cam2/Cam3 (and Cam5/Cam6) to compare their timestamps directly against Cam1 (and Cam4) instead of the immediately preceding data type in the sequence.

### 1.2 Success Criteria

- [ ] A new option (checkbox) is available in the Data Sequence Settings (or equivalent configuration area).
- [ ] When enabled, Cam2 and Cam3 are validated/grouped based on their time difference from Cam1, using their own configured delay settings.
- [ ] If Cam1 is disabled or missing in the sequence, Cam2 and Cam3 fall back to comparing against the **valid preceding sequence item**, effectively treating them as standard sequential matching.
- [ ] When enabled, Cam5 and Cam6 are validated/grouped based on their time difference from Cam4.
- [ ] If Cam4 is disabled or missing, Cam5/Cam6 fall back to comparing against the valid preceding sequence item.
- [ ] When disabled (default), the existing sequential matching logic (comparing against the previous item) is preserved.
- [ ] The option persists across application restarts.

## 2. Constraints

### 2.1 Technical Constraints

- **Note**: Internally, the matching engine processes Line 2 (files from cam4/5/6) using `DataType.Cam1`, `DataType.Cam2`, and `DataType.Cam3` respectively.
  - Therefore, the "Reference Camera" logic applies simply to `DataType.Cam1`, covering both Line 1 (Cam1) and Line 2 (Cam4).
  - The configuration option applies to `DataType`s, effectively enabling the behavior for both lines simultaneously.
- **Fallback Definition**: "Valid preceding sequence item" is defined as the **nearest enabled item** preceding the current item in the configured `orderedTypes` list (index-based lookup).

### 2.2 Business Constraints

- Existing sequence configurations must not be broken by this change (backward compatibility).

### 2.3 Non-Goals (Out of Scope)

- Changing the visual order of columns in the UI.
- Arbitrary reference selection (e.g., comparing Cam3 to Normal). Only "Compare to Cam1" is required.

## 3. Questions to Investigate

> Skip this section if Research Required = No

## 4. Assumptions

- Users understand that "Cam1" (and Cam4) must be present and processed earlier in the sequence for this option to work effectively.
- "Cam2" and "Cam3" refer to the data types, regardless of their position in the configured sequence list, but typically they follow Cam1.

### 4.1 Preconditions

- Cam1 (for Line 1) and Cam4 (for Line 2) are the preferred references.
- If "Compare to Cam1" is checked but Cam1 is disabled (Exceptional Case):
  - Comparison falls back to the **preceding order**.
  - Both Cam2 and Cam3 will compare to the valid preceding sequence item.
- The delay settings (`MinDelay`, `MaxDelay`) configured for each camera are applied against the determined reference.

## 5. Dependencies

### 5.1 Blocked By

None.

### 5.2 Blocks

None.

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md
