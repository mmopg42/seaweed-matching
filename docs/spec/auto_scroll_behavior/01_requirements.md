# Requirements: Auto Scroll Behavior

## 1. Overview
The current Grid UI in `ChronoView` automatically scrolls to the bottom whenever a new row is added during monitoring. The user requires this behavior to be conditional: the auto-scroll should only occur if the user is currently at the bottom of the list. If the user has scrolled up to view previous items, the UI should **not** force-scroll to the bottom upon new data arrival.

## 2. Goals
- **Smart Auto-Scroll**: Scroll to the latest item only if the viewport is arguably "at the bottom".
- **User Control**: Prevent interruption of user analysis (viewing history) when new data arrives.
- **Visual Feedback**: (Optional but recommended) Indication that new data has arrived if auto-scroll is paused. *Note: User request specifically focuses on the locking behavior, not the indicator.*

## 3. Detailed Requirements
### 3.1 Functional Requirements
- **FR-01**: The system MUST detect if the `FileGroupDataGrid` vertical scroll bar is at the bottom (maximum offset).
- **FR-02**: When a new `FileGroup` is added to the list:
    - If the scroll bar is at the bottom, the grid MUST scroll to the new item.
    - If the scroll bar is NOT at the bottom, the grid MUST maintain its current scroll position.
- **FR-03**: This logic applies only when `IsMonitoring` is true (implied by current usage context).

### 3.2 Non-Functional Requirements
- **Performance**: The scroll detection and position check must be lightweight and not impede UI rendering.
- **Reliability**: Must work correctly with variable row heights (though currently rows seem fixed).

## 4. User Interface Change
- No visible UI element changes (buttons/toggles).
- The *behavior* of the scrollbar changes.

## 5. Constraints
- Must be implemented within the existing `FileGroupDataGrid` control or `MainWindow` logic.
- Target framework: .NET 10.0 / WPF.
