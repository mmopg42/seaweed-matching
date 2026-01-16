# Design: Fix Delete and Checkbox Bugs

## 1. UI Binding Design (Checkbox Fix)

### 1.1 Problem
`DataGridRow.IsSelected` is currently not bound to `FileGroupViewModel.IsSelected`.
*   Header Checkbox -> Updates VM -> Updates Row (OneWay effective).
*   Row Click -> Updates Row -> **Does NOT update VM**.
*   **Result**: Delete command relies on VM, so row-clicked items are ignored.

### 1.2 Solution: TwoWay Binding in ItemContainerStyle
Modify `ChronoView/UI/Controls/FileGroupDataGrid.xaml`.

**Correct Strategy**: Remove the explicit `RowStyle` attribute from the `DataGrid` tag to avoid conflict, and apply the style exclusively via `ItemContainerStyle`.

**Code Specification**:
```xml
<DataGrid x:Name="MainDataGrid" 
          ... 
          <!-- REMOVE: RowStyle="{StaticResource FileGroupRowStyle}" --> >
    
    <DataGrid.ItemContainerStyle>
        <Style TargetType="DataGridRow" BasedOn="{StaticResource FileGroupRowStyle}">
            <!-- Critical: Bind DataGridRow.IsSelected to ViewModel.IsSelected -->
            <Setter Property="IsSelected" Value="{Binding IsSelected, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
        </Style>
    </DataGrid.ItemContainerStyle>

    ...
</DataGrid>
```

## 2. Message Logic Design (Delete Confirmation)

### 2.1 Problem
User sees a massive list of every single file component for every group.

### 2.2 Solution: Hybrid Summary (Details + Categories)
Modify `ExecuteDeleteWithConfirmation` in `MainWindowViewModel.cs`.

**Logic Flow**:
1.  **Classification**: Split selected groups into `fullDelete` and `partialDelete` lists.
2.  **Message Construction**:
    *   **Full Deletion Section**:
        *   Header: "• 전체 삭제: {Count}개 그룹"
        *   Loop Top 3: "  - {GroupId} ({ItemCount}개 항목: Normal, Cam1...)"
        *   Summary: "  ... 외 {Count - 3}개 그룹"
    *   **Partial Deletion Section**:
        *   Header: "• 부분 삭제: {Count}개 그룹"
        *   Loop Top 3: "  - {GroupId} ({ItemCount}개 항목: Cam1, Cam2...)"
        *   Summary: "  ... 외 {Count - 3}개 그룹"
    *   **Total Summary**: "📊 총 {TotalGroups}개 그룹, {TotalItems}개 항목"
    *   **Destination**: "📁 이동 경로: {QuarantinePath}" (Explicit Path)

**Code Structure (Conceptual)**:
```csharp
var sb = new StringBuilder();
sb.AppendLine("다음 데이터를 삭제합니다:\n");

// Helper Action to avoid code duplication
void AppendCategory(string title, List<FileGroupViewModel> items)
{
    if (items.Count == 0) return;
    sb.AppendLine($"• {title}: {items.Count}개 그룹");
    
    foreach (var group in items.Take(3))
    {
        var comps = group.GetSelectedComponents();
        // 상세 항목 표시 (User Requirement)
        sb.AppendLine($"  - {group.GroupId} ({comps.Count}개 항목: {string.Join(", ", comps)})");
    }
    
    if (items.Count > 3)
        sb.AppendLine($"  ... 외 {items.Count - 3}개 그룹");
    sb.AppendLine();
}

AppendCategory("전체 삭제", fullDelete);
AppendCategory("부분 삭제", partialDelete);

// Total Stats & Path
var config = await _configManager.LoadConfigurationAsync<ApplicationConfiguration>();
var quarantinePath = config.WorkflowSettings.DeleteQuarantinePath 
                     ?? Path.Combine(config.BasePath ?? @"D:\Data", "Quarantine");

sb.AppendLine($"📊 총 {totalGroups}개 그룹, {totalItems}개 항목");
sb.AppendLine($"📁 이동 경로: {quarantinePath}");
sb.AppendLine("\n진행하시겠습니까?");
```

## 3. Impact Analysis
*   **Performance**: Negligible.
*   **Risks**:
    *   If `GetSelectedComponents()` is empty for a selected group (shouldn't happen with `HasAnyPartialSelected` logic), it might show "group_xxx: ". (Logic handles this by checking `HasAnyPartialSelected` before adding to list).
*   **Conflict**: None with existing features. `ItemContainerStyle` is purely an additive binding.
