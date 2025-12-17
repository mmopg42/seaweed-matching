---
Owner: ChronoView Development Team
Last Updated: 2025-12-15
Code Ref: MainWindow.xaml, SettingsDialog.xaml, SplashWindow.xaml
---

# ChronoView Design Guidelines

## Overview

This document defines the UI/UX design system for ChronoView application. All new windows, dialogs, and controls MUST follow these guidelines to maintain visual consistency and user experience quality across the application.

## Design Philosophy

ChronoView follows a **professional desktop application** design approach with these core principles:

- **Clarity**: Clean, uncluttered interfaces with clear visual hierarchy
- **Consistency**: Unified color palette, typography, and spacing across all views
- **Functionality**: Form follows function - UI elements serve clear purposes
- **Professionalism**: Industrial-strength design suitable for manufacturing/monitoring environments

---

## 1. Color Palette

### 1.1 Primary Colors

| Color Name | Hex Code | Usage | XAML Resource Key |
|------------|----------|-------|-------------------|
| Background | `#f0f0f0` | Main window background | `BackgroundBrush` |
| Panel Background | `#f5f5f5` | Toolbar, sidebars, status bars | `PanelBackgroundBrush` |
| Border | `#d0d0d0` | Borders, dividers | `BorderBrush` |
| Selection | `#0078d4` | Selected items, focus states | `SelectionBrush` |
| Hover | `#e0e0e0` | Hover states for buttons, rows | `HoverBrush` |
| Title Bar | `#2d2d2d` | Window title bars (dark theme) | `TitleBarBrush` |
| White | `#ffffff` | DataGrid background, cards | - |

### 1.2 Semantic Colors

| Purpose | Color | Hex Code | Usage |
|---------|-------|----------|-------|
| Success | Green | System default | Status indicators (Ready, Active) |
| Warning | Yellow/Orange | `#ffc107` (border), `#fff3cd` (bg) | Abnormal data highlighting |
| Error | Red | `#dc2626` | Failed operations, critical issues |
| Info | Blue | `#0078d4` | Informational messages |
| Neutral | Gray | `#6c757d` | Secondary text, labels |

### 1.3 Brand Colors

| Element | Color | Hex Code | Usage |
|---------|-------|----------|-------|
| PRISCHE Red | Red | `#D41C24` | Splash screen, branding elements |

### 1.4 Image Placeholder Colors

| Element | Background | Border | Hex Codes |
|---------|------------|--------|-----------|
| Image containers | Dark gray | Medium gray | `#3a3a3a` / `#505050` |

---

## 2. Typography

### 2.1 Font Family

- **Primary Font**: `Segoe UI` (Windows standard)
- **Fallback**: System default sans-serif

### 2.2 Font Sizes

| Element Type | Size (px) | Weight | Usage |
|--------------|-----------|--------|-------|
| Splash Title | 60 | Bold | Main branding on splash screen |
| Splash Subtitle | 32 | SemiBold | Application name on splash |
| Window Title | Default | Default | Window chrome titles |
| Section Header | 14 | Bold | Major section titles (Settings tabs) |
| Subsection Header | 12 | Bold | Minor section titles (Combined view headers) |
| Body Text | Default (11-12) | Normal | Labels, content, form fields |
| Small Text | 10 | Normal | Button labels, hints |
| Micro Text | Default | Light | Descriptions, helper text |

### 2.3 Text Colors

| Context | Color | Usage |
|---------|-------|-------|
| Default | Black | Main text content |
| Secondary | `#6c757d` | Labels, less important text |
| Error | `#dc2626` | Error messages, failed counts |
| Warning | `#f59e0b` | Warning messages, abnormal indicators |
| Success | Green (system) | Success messages, active status |
| White | White | Text on dark backgrounds (splash, title bars) |
| Hint | Gray | Placeholder text, helper hints |

---

## 3. Layout & Spacing

### 3.1 Window Dimensions

| Window Type | Default Size | Constraints |
|-------------|--------------|-------------|
| Main Window | 1400 × 900 | Resizable |
| Settings Dialog | 800 × 600 | Fixed or minimal resize |
| Splash Screen | 700 × 450 | Fixed, centered |

### 3.2 Spacing System

Use consistent spacing based on 4px/8px grid:

| Size | Value (px) | Usage |
|------|------------|-------|
| XS | 4 | Tight spacing within components |
| S | 8 | Component padding, internal spacing |
| M | 10-12 | Section separation, panel padding |
| L | 20-40 | Major section separation |

### 3.3 Common Margins/Padding

| Element | Padding/Margin |
|---------|----------------|
| Window content | 10px |
| TabControl content | 10px |
| StackPanel items | 0,5 (vertical spacing) |
| Section headers | 0,10,0,5 (top, bottom) |
| Button panels | 10px |
| Toolbar | 12,8 |
| Status bar | 10,0 |

### 3.4 Layout Patterns

#### Grid Layout
- Use `Grid.ColumnDefinitions` and `Grid.RowDefinitions` for complex layouts
- Common pattern: Sidebar (250px) + Main content (*)
- Use `GridSplitter` for resizable sections (5px width)

#### DockPanel Layout
- Use for main window structure (Menu → Toolbar → Status Bar → Content)
- `DockPanel.Dock="Top|Bottom|Left|Right"`

#### StackPanel Layout
- Use for form fields, simple vertical/horizontal arrangements
- Set `Orientation="Horizontal"` for horizontal layouts

---

## 4. UI Components

### 4.1 Buttons

#### Toolbar Buttons
```xaml
<Style x:Key="ToolbarButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="Margin" Value="2,0"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{StaticResource HoverBrush}"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**Structure**:
- Icon (Unicode emoji or symbol, 16px)
- Label (10px, below icon)

**Common Icons**:
- Start: `▶`
- Stop: `■`
- Settings: `⚙`
- Refresh: `↻`
- Move: `→`
- Delete: `🗑`

#### Dialog Buttons
- Width: 80px
- Height: 30px
- Margin: 0,0,10,0 (right spacing)
- Alignment: Right
- Order: Apply → OK → Cancel

### 4.2 Statistics Chips

```xaml
<Style x:Key="ChipStyle" TargetType="Border">
    <Setter Property="Background" Value="#f5f5f5"/>
    <Setter Property="BorderBrush" Value="#d0d0d0"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="3"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="Margin" Value="4,0"/>
</Style>
```

**Usage**:
- File counts, matching statistics
- Label (gray `#6c757d`) + Value (bold)
- Always used in `WrapPanel` or horizontal `StackPanel`

### 4.3 DataGrid

```xaml
<Style x:Key="FileGroupDataGridStyle" TargetType="DataGrid">
    <Setter Property="Background" Value="White"/>
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="GridLinesVisibility" Value="Horizontal"/>
    <Setter Property="HorizontalGridLinesBrush" Value="#e0e0e0"/>
    <Setter Property="AutoGenerateColumns" Value="False"/>
    <Setter Property="SelectionMode" Value="Extended"/>
    <Setter Property="SelectionUnit" Value="FullRow"/>
    <Setter Property="RowHeight" Value="{Binding DataContext.DataGridRowHeight, FallbackValue=100}"/>
</Style>
```

**Key Features**:
- Transparent selection (highlight with borders/background instead)
- Horizontal grid lines only
- Configurable row height
- Full-row selection

#### Image Columns
- Container: `Border` with dark background `#3a3a3a`, border `#505050`
- Image: `Stretch="Uniform"`
- Loading indicator: Indeterminate `ProgressBar` at bottom, 4px height
- Size: Bound to ViewModel properties (default 120×90)

#### Row Highlighting
```xaml
<Style x:Key="FileGroupRowStyle" TargetType="DataGridRow">
    <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay}"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding IsAbnormal}" Value="True">
            <Setter Property="Background" Value="#fff3cd"/>
            <Setter Property="BorderBrush" Value="#ffc107"/>
            <Setter Property="BorderThickness" Value="2,0,0,0"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

### 4.4 Forms (Settings Dialog Pattern)

#### Label Style
```xaml
<Style x:Key="LabelStyle" TargetType="Label">
    <Setter Property="Width" Value="150"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>
```

#### Field Layout
```xaml
<StackPanel Orientation="Horizontal" Margin="0,5">
    <Label Content="Field Name:" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding PropertyName}" Width="400" Margin="0,0,5,0"/>
    <Button Content="Browse..." Width="80" Command="{Binding Command}"/>
</StackPanel>
```

#### Section Headers
```xaml
<Style x:Key="SectionHeaderStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14"/>
    <Setter Property="FontWeight" Value="Bold"/>
    <Setter Property="Margin" Value="0,10,0,5"/>
</Style>
```

### 4.5 Tabs

- Use `TabControl` for multi-view UIs
- Tab headers: Simple text labels
- Tab content: Wrap in `ScrollViewer` with `VerticalScrollBarVisibility="Auto"`
- TabControl content margin: 10px

### 4.6 Sidebars/Panels

- Background: `{StaticResource PanelBackgroundBrush}`
- Border: 1px, `{StaticResource BorderBrush}`
- Common width: 250px
- Use `Expander` for collapsible sections
- Expander headers: Bold text with hover background

### 4.7 Status Bars

- Background: `{StaticResource PanelBackgroundBrush}`
- Border: 1px top, `{StaticResource BorderBrush}`
- Content: `StatusBarItem` elements with separators
- Progress bar: 120-150px width, 12-16px height
- Alignment: Status message (left), progress (center), time (right)

### 4.8 Toolbars/Menus

- Background: `{StaticResource PanelBackgroundBrush}`
- Border: 1px bottom, `{StaticResource BorderBrush}`
- Menu items: Standard WPF `Menu` and `MenuItem`
- ToolBar: Use `ToolBarTray` with `ToolBar` children
- Separator: Use `<Separator/>` between button groups

---

## 5. Visual States & Interactions

### 5.1 Hover States

- Buttons: Background changes to `{StaticResource HoverBrush}`
- DataGrid rows: System default (subtle highlight)
- Interactive elements: `Cursor="Hand"`

### 5.2 Selection States

- DataGrid: Transparent highlight with border (defined in row style)
- List items: Use system default or custom highlight colors

### 5.3 Disabled States

- Opacity: 0.5-0.6
- Use `IsEnabled` binding

### 5.4 Loading States

- Use indeterminate `ProgressBar` for ongoing operations
- Display at bottom of image containers
- Height: 4px
- Visibility bound to data availability (`Converter={StaticResource NullToVisibilityConverter}`)

### 5.5 Abnormal/Error States

- Background: `#fff3cd` (yellow tint)
- Border: `#ffc107` (warning color)
- Border thickness: `2,0,0,0` (left accent)
- Text color: `#f59e0b` or `#dc2626` for critical errors

---

## 6. Window Patterns

### 6.1 Main Window Structure

```
Window
└── DockPanel
    ├── Menu (DockPanel.Dock="Top")
    ├── ToolBarTray (DockPanel.Dock="Top")
    ├── Statistics Bars (DockPanel.Dock="Top", multiple)
    ├── StatusBar (DockPanel.Dock="Bottom")
    └── Grid (Main content area)
        ├── Sidebar (Column 0)
        └── Content (Column 1)
            ├── TabControl or DataGrid
            └── (Bottom panel if needed)
```

### 6.2 Dialog Window Structure

```
Window (Background="#f0f0f0")
└── DockPanel
    ├── Border (DockPanel.Dock="Bottom" - Button panel)
    │   └── StackPanel (HorizontalAlignment="Right")
    │       └── Buttons (Apply, OK, Cancel)
    └── TabControl (Main content)
        └── TabItems
            └── ScrollViewer
                └── StackPanel
                    └── Form fields
```

**Properties**:
- `WindowStartupLocation="CenterOwner"`
- Fixed or minimal resize
- Background: `#f0f0f0`

### 6.3 Splash Screen Structure

```
Window (WindowStyle="None", AllowsTransparency="True", Background="Transparent")
└── Border (Background="#D41C24", CornerRadius="10")
    └── Grid
        └── StackPanel (Centered)
            ├── Brand Logo (Border + TextBlock)
            ├── Application Title (Large, SemiBold)
            ├── Subtitle (Light)
            └── Loading Indicator
```

**Properties**:
- No window chrome (`WindowStyle="None"`)
- Rounded corners (`CornerRadius="10"`)
- Centered on screen
- Fixed size
- Topmost window

---

## 7. Converters

### 7.1 Required Converters

All new windows SHOULD include these standard converters:

```xaml
<Window.Resources>
    <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
    <local:NullToVisibilityConverter x:Key="NullToVisibilityConverter"/>
    <local:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
</Window.Resources>
```

### 7.2 Converter Usage

| Converter | Input | Output | Usage |
|-----------|-------|--------|-------|
| `BooleanToVisibilityConverter` | `bool` | `Visibility` | Show/hide based on boolean flag |
| `NullToVisibilityConverter` | `object` | `Visibility` | Show loading indicator when data is null |
| `BoolToVisibilityConverter` | `bool` | `Visibility` | Custom bool-to-visibility logic |

---

## 8. Resource Organization

### 8.1 Resource Hierarchy

1. **Application-level** (`App.xaml`): Global resources (limited usage in ChronoView)
2. **Window-level** (`Window.Resources`): Window-specific styles, converters, colors
3. **Control-level**: Inline resources for one-off customizations

### 8.2 Resource Naming Conventions

| Type | Naming Pattern | Example |
|------|----------------|---------|
| Color Brush | `{Purpose}Brush` | `BackgroundBrush`, `SelectionBrush` |
| Style | `{TargetControl}{Purpose}Style` | `ToolbarButtonStyle`, `FileGroupDataGridStyle` |
| Converter | `{Conversion}Converter` | `BooleanToVisibilityConverter` |

### 8.3 Reusable Resources

Define these in EVERY window for consistency:

```xaml
<Window.Resources>
    <!-- Converters -->
    <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
    <local:NullToVisibilityConverter x:Key="NullToVisibilityConverter"/>
    
    <!-- Color Resources -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="#f0f0f0"/>
    <SolidColorBrush x:Key="PanelBackgroundBrush" Color="#f5f5f5"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#d0d0d0"/>
    <SolidColorBrush x:Key="SelectionBrush" Color="#0078d4"/>
    <SolidColorBrush x:Key="HoverBrush" Color="#e0e0e0"/>
    
    <!-- Common Styles (as needed) -->
    <Style x:Key="SectionHeaderStyle" TargetType="TextBlock">...</Style>
    <Style x:Key="LabelStyle" TargetType="Label">...</Style>
</Window.Resources>
```

---

## 9. Accessibility & UX

### 9.1 Keyboard Navigation

- All interactive controls MUST be keyboard-accessible
- Set `IsDefault="True"` on primary action buttons (OK, Apply)
- Set `IsCancel="True"` on Cancel buttons
- Use `TabIndex` if logical tab order differs from visual order

### 9.2 Focus Indicators

- Use system default focus visualization
- Ensure sufficient contrast for focus states

### 9.3 Tooltips

- Add tooltips to toolbar buttons
- Format: `ToolTip="Action description"`
- Use imperative verbs: "Start monitoring", "Open settings"

### 9.4 Feedback

- Show progress indicators for long-running operations
- Update status bar with current operation
- Display success/error messages clearly

---

## 10. Data Binding Patterns

### 10.1 Binding to ViewModel

- Use `{Binding PropertyName}` for simple bindings
- Use `UpdateSourceTrigger=PropertyChanged` for real-time updates
- Use `Mode=TwoWay` for bi-directional bindings (selection, input fields)

### 10.2 Relative Source Bindings

For accessing ancestor DataContext (e.g., image size bindings):

```xaml
{Binding DataContext.PropertyName, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=DefaultValue}
```

### 10.3 String Formatting

```xaml
Text="{Binding Value, StringFormat={}{0:F1}%}"
```

---

## 11. Design Checklist for New Windows/Views

Use this checklist when creating new UI components:

### Planning
- [ ] Determine window type (Main, Dialog, Splash, Custom)
- [ ] Define required sections/panels
- [ ] Identify data to display and user interactions

### Layout
- [ ] Choose appropriate layout container (DockPanel, Grid, StackPanel)
- [ ] Define window size and resize behavior
- [ ] Set window startup location (CenterScreen, CenterOwner)
- [ ] Implement proper spacing (4px/8px grid)

### Styling
- [ ] Define `Window.Resources` with color brushes
- [ ] Include required converters
- [ ] Create component-specific styles
- [ ] Use consistent font sizes (14px headers, 12px body, 10px labels)

### Components
- [ ] Use ToolbarButtonStyle for toolbar buttons
- [ ] Use ChipStyle for statistics/badges
- [ ] Use FileGroupDataGridStyle for data tables
- [ ] Use SectionHeaderStyle for form sections
- [ ] Add appropriate borders with `#d0d0d0`

### Colors
- [ ] Background: `#f0f0f0`
- [ ] Panel/Toolbar: `#f5f5f5`
- [ ] Borders: `#d0d0d0`
- [ ] Hover: `#e0e0e0`
- [ ] Selection: `#0078d4`
- [ ] Error: `#dc2626`
- [ ] Warning: `#ffc107` / `#fff3cd`

### Interactions
- [ ] Set `Cursor="Hand"` on clickable elements
- [ ] Add hover states to buttons
- [ ] Implement proper focus management
- [ ] Add tooltips to non-obvious controls
- [ ] Set `IsDefault` and `IsCancel` on dialog buttons

### Data Binding
- [ ] Bind to ViewModel properties
- [ ] Use appropriate UpdateSourceTrigger and Mode
- [ ] Add FallbackValue where needed
- [ ] Implement loading states with converters

### Verification
- [ ] Test keyboard navigation
- [ ] Test window at different sizes (if resizable)
- [ ] Verify color contrast
- [ ] Check visual alignment and spacing
- [ ] Confirm consistency with existing windows

---

## 12. Anti-Patterns (DO NOT DO)

### ❌ Avoid These Mistakes

1. **Inconsistent Colors**
   - Don't use hard-coded colors outside the defined palette
   - Don't use system colors where custom colors are defined

2. **Inconsistent Spacing**
   - Don't use random margins like 3px, 7px, 13px
   - Stick to the 4px/8px grid

3. **Poor Contrast**
   - Don't use low-contrast text (e.g., `#aaa` on `#ccc`)
   - Ensure text meets accessibility standards

4. **Over-styling**
   - Don't add unnecessary decorations (shadows, gradients, animations)
   - Keep design clean and functional

5. **Mixing Fonts**
   - Don't use multiple font families
   - Stick to Segoe UI

6. **Ignoring Window Chrome**
   - Don't create custom title bars unless absolutely necessary (e.g., splash screen)
   - Use standard Windows chrome for consistency

7. **Hard-coded Sizes**
   - Don't hard-code image sizes in DataGrid templates
   - Use ViewModel bindings with fallback values

8. **Inconsistent Button Styles**
   - Don't create custom button styles without reason
   - Use ToolbarButtonStyle for toolbars, standard buttons for dialogs

---

## 13. Reference Files

### Primary References

| File | Purpose | Key Patterns |
|------|---------|--------------|
| [`MainWindow.xaml`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/MainWindow.xaml) | Main application window | DockPanel structure, DataGrid styling, statistics chips, sidebar layout |
| [`SettingsDialog.xaml`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/UI/Views/SettingsDialog.xaml) | Settings/configuration dialog | TabControl layout, form patterns, label styles, button panel |
| [`SplashWindow.xaml`](file:///c:/workspace/seaweed/gui_kiro/ChronoView/UI/Views/SplashWindow.xaml) | Splash screen | Transparent window, rounded corners, brand colors, centered layout |

### Style Extraction

To extract a style for reuse:

1. Identify the pattern in reference files
2. Copy the `<Style>` definition to your `Window.Resources`
3. Adjust `x:Key` if needed
4. Apply via `Style="{StaticResource KeyName}"`

---

## 14. Glossary

| Term | Definition |
|------|------------|
| **Chip** | Small, rounded rectangle displaying a label-value pair (used for statistics) |
| **Toolbar Button** | Icon + label button in transparent style with hover effect |
| **Panel** | Container area with light gray background (`#f5f5f5`) |
| **FileGroup** | Data model representing a matched set of files (NIR, Normal, Cameras) |
| **Abnormal** | State indicating data mismatch or quality issue (highlighted in yellow) |
| **Statistics Bar** | Horizontal bar displaying multiple chips with file counts or matching status |

---

## Changelog

- **2025-12-15**: Initial creation based on MainWindow.xaml, SettingsDialog.xaml, and SplashWindow.xaml
