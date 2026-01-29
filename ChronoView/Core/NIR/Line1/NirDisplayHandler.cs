using System.Windows.Controls;
using ChronoView.Core.NIR.Interfaces;
using ChronoView.Models;

namespace ChronoView.Core.NIR.Line1;

/// <summary>
/// NIR display handler for Line 1.
/// Provides templates and update logic for NIR visualization in DataGrid.
/// </summary>
public class NirDisplayHandler : INirDisplayHandler
{
    /// <summary>
    /// Get the DataTemplate for displaying NIR data in a DataGrid cell.
    /// Returns the shared NirFileTemplate from SharedResources.
    /// </summary>
    /// <returns>DataTemplate for NIR display</returns>
    public System.Windows.DataTemplate GetTemplate()
    {
        // The template is defined in SharedResources.xaml with key "NirFileTemplate"
        // This method returns a reference to that template
        return (System.Windows.DataTemplate)System.Windows.Application.Current.FindResource("NirFileTemplate");
    }

    /// <summary>
    /// Update a DataGrid cell with NIR data for a specific file group.
    /// Finds the NIR column and updates its content binding.
    /// </summary>
    /// <param name="cell">The framework element (cell) to update</param>
    /// <param name="group">The file group containing NIR data</param>
    public void UpdateCell(System.Windows.FrameworkElement cell, FileGroup group)
    {
        if (cell == null || group == null)
            return;

        // Find the Image control in the cell template
        if (cell is System.Windows.Controls.ContentControl contentControl)
        {
            // The template will handle the actual display
            contentControl.Content = group;
        }
    }
}
