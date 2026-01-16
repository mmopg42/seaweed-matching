# Log Window Resize/Collapse Bug

## 1. Issue Description
**Symptom**: When the log window contains enough data to show scrollbars (or generally when used), clicking the "Close" (Collapse) button does not reclaim the space for the main content area effectively. The log panel area might remain as empty space or not collapse fully.

**Context**: 
- User observed this when "data goes beyond the log window" (scrollbars active).
- Action: Clicking the close button on the Log Panel.

## 2. Technical Investigation
**File**: `ChronoView/MainWindow.xaml`

### Current Implementation
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="*"/> <!-- Main Content -->
    <RowDefinition ... />       <!-- Splitter Row -->
    <RowDefinition>             <!-- Log Panel Row -->
        <RowDefinition.Style>
            <Style TargetType="RowDefinition">
                <Setter Property="Height" Value="200"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsLogPanelVisible}" Value="False">
                        <Setter Property="Height" Value="0"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </RowDefinition.Style>
    </RowDefinition>
</Grid.RowDefinitions>
```

### Root Cause Analysis
The issue is likely caused by the **Dependency Property Value Precedence** in WPF when interacting with a `GridSplitter`.

1. **Initial State**: The `RowDefinition.Height` is set via the **Style Setter** (Height="200").
2. **User Interaction**: If the user drags the `GridSplitter` (or sometimes just by the nature of GridSplitter initialization), the `GridSplitter` sets the `Height` property of the RowDefinition **locally** (Local Value).
3. **Precedence Rule**: **Local Value > Style Trigger**.
4. **The Bug**: When `IsLogPanelVisible` becomes `False`, the DataTrigger in the Style attempts to set `Height` to `0`. However, because the `GridSplitter` has set a **Local Value** on that RowDefinition, the Style Trigger is ignored. The Row remains at the height set by the splitter (e.g., "250" or whatever pixel value it was dragged to).

## 3. Proposed Fix
To fix this, we need to ensure the `Height` is controlled in a way that allows the "Collapsed" state to override the user's manual resizing.

### Option A: ViewModel Binding (Recommended)
Bind the `RowDefinition.Height` to a property in the ViewModel (e.g., `LogPanelHeight`) using a `GridLength` type.
- In ViewModel: When `IsLogPanelVisible` is toggled to `False`, set `LogPanelHeight = new GridLength(0)`.
- When toggled `True`, restore it to valid height (e.g. `new GridLength(200)` or saved value).

### Option B: Attached Behavior
Use an attached behavior that clears the `LocalValue` of the Height property when visibility changes, or forces the height to 0.

### Option C: XAML-only (Complex)
Moving the `GridSplitter` and `LogPanel` into a separate constrained container that hides completely might work, but standard GridSplitter behavior usually requires more explicit control.
