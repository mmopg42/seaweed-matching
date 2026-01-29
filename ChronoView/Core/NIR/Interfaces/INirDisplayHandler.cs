using ChronoView.Models;

namespace ChronoView.Core.NIR.Interfaces;

/// <summary>
/// Interface for NIR display handling in UI components.
/// Provides templates and update logic for NIR visualization in DataGrid.
/// </summary>
public interface INirDisplayHandler
{
    /// <summary>
    /// Get the DataTemplate for displaying NIR data in a DataGrid cell.
    /// </summary>
    /// <returns>DataTemplate for NIR display</returns>
    System.Windows.DataTemplate GetTemplate();

    /// <summary>
    /// Update a DataGrid cell with NIR data for a specific file group.
    /// </summary>
    /// <param name="cell">The framework element (cell) to update</param>
    /// <param name="group">The file group containing NIR data</param>
    void UpdateCell(System.Windows.FrameworkElement cell, FileGroup group);
}
