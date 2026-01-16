# Investigation: Checkbox Header Alignment Issue

## 1. Issue Description
The checkbox header (Select All) in the data grid is left-aligned, while other headers are centered. The user requested an investigation into the cause.

## 2. Analysis of `FileGroupDataGrid.xaml`
*   **Checkbox Column Definition (Lines 43-60)**:
    ```xml
    <DataGridTemplateColumn Width="40">
        <DataGridTemplateColumn.HeaderTemplate>
            <DataTemplate>
                <CheckBox x:Name="SelectAllCheckBox"
                          HorizontalAlignment="Center" 
                          VerticalAlignment="Center"
                          ... />
            </DataTemplate>
        </DataGridTemplateColumn.HeaderTemplate>
        <!-- Missing HeaderStyle -->
    </DataGridTemplateColumn>
    ```

*   **Comparison with Other Columns**:
    *   **Index Column (Lines 63-75)**:
        ```xml
        <DataGridTextColumn Header="{x:Static res:Strings.Column_Index}" ...>
            <DataGridTextColumn.HeaderStyle>
                <Style TargetType="DataGridColumnHeader">
                    <Setter Property="HorizontalContentAlignment" Value="Center"/>
                </Style>
            </DataGridTextColumn.HeaderStyle>
            ...
        </DataGridTextColumn>
        ```
    *   **Dynamic Columns (C# Code)**:
        ```csharp
        var headerStyle = new Style(typeof(DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
        // applied to column.HeaderStyle
        ```

## 3. Root Cause
The `DataGridTemplateColumn` for the checkbox lacks an explicit `HeaderStyle` definition.
*   By default, `DataGridColumnHeader` elements often have `HorizontalContentAlignment` set to `Left` (or inherit a style that does).
*   Even though the `CheckBox` inside the `DataTemplate` has `HorizontalAlignment="Center"`, it is centering itself within the `ContentPresenter` of the header.
*   If the `ContentPresenter` itself is aligned to the Left of the header cell (due to `HorizontalContentAlignment="Left"` on the header control), the centered CheckBox appears on the left side.

## 4. Solution
Add a `HeaderStyle` to the `DataGridTemplateColumn` that explicitly sets `HorizontalContentAlignment` to `Center`.

```xml
<DataGridTemplateColumn.HeaderStyle>
    <Style TargetType="DataGridColumnHeader">
        <Setter Property="HorizontalContentAlignment" Value="Center"/>
    </Style>
</DataGridTemplateColumn.HeaderStyle>
```

## 5. Additional Verification
*   **Global Style Check**: Checked `SharedResources.xaml` and confirmed there is no global style for `DataGridColumnHeader`. This means the WPF default behavior (Left alignment) applies unless overridden.
*   **Consistency Check**: All other columns (Index, Status, and dynamic columns) explicitly set `HeaderStyle` with `HorizontalContentAlignment="Center"`. The checkbox column is the **only exception**, confirming this is an oversight and not a design choice.
