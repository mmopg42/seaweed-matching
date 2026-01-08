# Troubleshooting: Inconsistent Column Order in UI

**Date:** 2026-01-04
**Status:** Open
**Component:** MainWindow.xaml (DataGrid UI)

## 1. Issue Description
The user observed a discrepancy in the column arrangement and labeling between the **Line 1 Tab** and the **Combined Tab** (Line 1 section):

- **Line 1 Tab (User Intent):** 
  - Order: Index -> Status -> NIR -> Normal (일반카메라) -> Cameras
  - Labels: Localized (e.g., "일반카메라")
- **Combined Tab (Current Behavior):** 
  - Order: Index -> Status -> Main Img (Normal) -> NIR Graph -> Cameras
  - Labels: Hardcoded English ("Main Img", "NIR Graph")

## 2. Root Cause Analysis
The analysis of `MainWindow.xaml` reveals that the `DataGrid` component is not reused but **manually duplicated** in three separate locations within the XAML file:
1.  **Line 1 Tab**: Lines 182-226 (`Line1DataGrid`)
2.  **Line 2 Tab**: Lines 228-274 (`Line2DataGrid`)
3.  **Combined Tab**: Lines 298-341 (Embedded DataGrid)

### Specific Cause of Inconsistency
During the recent refactoring of `MainWindow.xaml` (to extract global resources and panels), the `DataGrid` definitions were replaced or modified. The code currently present in `MainWindow.xaml` (for both Line 1 and Combined tabs) dictates the **Normal -> NIR** order with hardcoded English headers ("Main Img").

Detailed finding:
- The **Code Duplication** means any change to column order must be manually applied to *all 3* DataGrids.
- The recent update seemingly applied the "Normal -> NIR" order (and hardcoded labels) to the code, differentiating it from the user's previous or expected state (NIR -> Normal).

## 3. Recommended Fix
To resolve this and prevent future drift:

1.  **Immediate Fix**: 
    - Edit `MainWindow.xaml`.
    - Modify the `<DataGrid.Columns>` section in **Line 1 Tab**, **Line 2 Tab**, and **Combined Tab**.
    - Reorder columns to: `NIR Graph` -> `Normal/Main Img` (or as per Settings match order).
    - Restore localized headers (e.g., using `{x:Static res:Strings...}`) instead of "Main Img".

2.  **Long-term Fix (Refactoring)**:
    - Create a shared `DataTemplate` or `UserControl` that encapsulates the `DataGrid` definition.
    - This would allow a single source of truth for column layout and headers.
