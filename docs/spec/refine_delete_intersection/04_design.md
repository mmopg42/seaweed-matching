# Design: Deletion Logic Refinement (Intersection)

## 1. UI Binding Update
*   **File**: `ChronoView/Resources/SharedResources.xaml`
*   **Change**: Update `CheckBox` bindings in all Item Templates.
*   **Snippet**:
    ```xml
    <!-- Before -->
    <CheckBox Style="{StaticResource PartialSelectionCheckBoxStyle}" IsChecked="{Binding IsNormalSelected}" />
    
    <!-- After -->
    <CheckBox Style="{StaticResource PartialSelectionCheckBoxStyle}" 
              IsChecked="{Binding IsNormalSelected, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
    ```
*   **Affected Templates**: `NormalFileTemplate`, `NirFileTemplate`, `Camera1Template` ... `Camera6Template`.

## 2. ViewModel Logic Update
*   **File**: `ChronoView/UI/ViewModels/FileGroupViewModel.cs`
*   **Change**: Enforce "Row Selection" gate in component retrieval methods.
*   **Snippet**:
    ```csharp
    public List<string> GetSelectedComponents()
    {
        // Row Gate: if the row is not selected, no components are valid for delete/move operations.
        // (Avoid extra work: return immediately before checking individual components.)
        if (!IsSelected)
            return new List<string>();

        // Component checks are a fixed, small set (Normal/NIR/Cam1..Cam6) => effectively O(1).
        var comps = new List<string>();
        if (IsNormalSelected && !string.IsNullOrEmpty(NormalFolder)) comps.Add("Normal");
        // ...
        return comps;
    }

    public List<string> GetSelectedComponentDetails()
    {
        if (!IsSelected) return new List<string>(); // Safety Gate
        
        var details = new List<string>();
        // ...
        return details;
    }
    ```

## 2.1 Definitions / Source of Truth (Observed in Code)
*   **Row Selection ("Row Checked")**:
    *   The left-most row checkbox is bound to `DataGridRow.IsSelected`, and `DataGridRow.IsSelected` is TwoWay-bound to `FileGroupViewModel.IsSelected`.
    *   **Source**: `ChronoView/UI/Controls/FileGroupDataGrid.xaml`
*   **Component Selection ("Component Checked")**:
    *   The overlay checkboxes are bound to `FileGroupViewModel.IsNormalSelected`, `IsNirSelected`, `IsCam1Selected` ... `IsCam6Selected`.
    *   **Source**: `ChronoView/Resources/SharedResources.xaml`

## 3. Impact Analysis
*   **Delete Command**: `FileOperationViewModel.ExecuteDeleteAsync` calls `GetSelectedComponents()`. If Row is unchecked, it returns empty list -> Skipping deletion. This meets the requirement.
*   **Move Command**: `ExecuteMoveAsync` moves the *whole group* based on Group Model. It does *not* typically use partial selection. It moves specific counts (e.g. "Move 1 NIR file").
    *   **Check**: Does Move logic rely on `GetSelectedComponents`? No, it uses `_moveService.BatchMoveAsync` with `vm.Model`.
    *   **Implication**: Current "Move" logic (Row Selection) is handled by `Where(g => g.IsSelected)`. So Move is safe (Row Gate matches).
    
    *   `IsNormalSelected` = False.
    *   `GetSelectedComponents()` returns List without "Normal".
    *   **Result**: Normal NOT deleted. Correct.
*   **Scenario B**: Row Unchecked, Normal Checked (Manual).
    *   `IsSelected` = False.
    *   `IsNormalSelected` = True.
    *   `GetSelectedComponents()` returns Empty List.
    *   **Result**: Nothing deleted. Correct.

## 4. Testing Notes (Minimum)
*   **Logic Test**:
    *   Given `IsSelected=false` and `IsNormalSelected=true`, `GetSelectedComponents()` MUST return empty.
    *   Given `IsSelected=true` and `IsNormalSelected=false`, "Normal" MUST NOT appear in returned list.
