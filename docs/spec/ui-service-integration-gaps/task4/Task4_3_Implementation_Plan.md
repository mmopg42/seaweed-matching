# Task 4.3 Implementation Plan: MainWindow.xaml Modifications

## 1. Goal
Modifying `MainWindow.xaml` to improve user feedback and interaction state management by adding a progress bar and enabling two-way selection binding.

## 2. Proposed Changes

### 2.1 StatusBar Enhancements
- **Add ProgressBar**: Insert a `ProgressBar` into the StatusBar to visually indicate operation progress.
- **Binding**: 
  - `Value` -> `ProgressValue` (Confirmed property exists in ViewModel)
  - `Visibility` -> `IsOperationInProgress` (via `BooleanToVisibilityConverter`)

### 2.2 DataGrid Integration
- **Row Selection Binding**: 
  - Update `FileGroupRowStyle` to bind `DataGridRow.IsSelected` to the ViewModel item's `IsSelected` property.
  - This enables two-way synchronization between the UI selection state and the ViewModel data state.

### 2.3 Verification of Existing Bindings
- **TabControl**: Verify `SelectedIndex="{Binding ActiveTabIndex}"` is functioning.
- **Line 1 DataGrid**: Verify `SelectedItem="{Binding SelectedLine1Group}"` is functioning.

## 3. Detailed Code Changes

### 3.1 StatusBar (MainWindow.xaml)
```xml
<!-- Existing StatusBar -->
<StatusBar ...>
    <StatusBarItem>
        <TextBlock Text="{Binding StatusMessage}"/>
    </StatusBarItem>
    
    <!-- Add new StatusBarItem for Progress -->
    <StatusBarItem>
         <ProgressBar Value="{Binding ProgressValue}" 
                      Maximum="100" 
                      Height="12" 
                      Width="120"
                      Visibility="{Binding IsOperationInProgress, Converter={StaticResource BooleanToVisibilityConverter}}"/>
    </StatusBarItem>
    
    <Separator/>
    <!-- ... -->
</StatusBar>
```

### 3.2 DataGridRow Style (MainWindow.xaml)
```xml
<Style x:Key="FileGroupRowStyle" TargetType="DataGridRow">
    <!-- Add IsSelected Binding -->
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

## 4. Verification Plan
1.  **Build**: Ensure no XAML binding errors.
2.  **Runtime**: 
    - Trigger a long-running operation (e.g., Refresh or AutoConfig) and observe the ProgressBar.
    - Select rows in the DataGrid and verify the `IsSelected` property in the ViewModel is updated.

## 5. Dependencies
- `MainWindowViewModel` properties `ProgressValue` and `IsOperationInProgress` (Verified).
- `BooleanToVisibilityConverter` (Verified in Resources).
