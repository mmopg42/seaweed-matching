using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridRow = System.Windows.Controls.DataGridRow;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace ChronoView.UI.Behaviors;

/// <summary>
/// Attached behavior that enables drag-to-select multiple rows in a DataGrid.
/// </summary>
public class DragSelectBehavior : Behavior<WpfDataGrid>
{
    private bool _isDragging;
    private WpfPoint _startPoint;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
        AssociatedObject.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
        AssociatedObject.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.SelectedItems.Count > 0 && 
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(AssociatedObject);
            AssociatedObject.CaptureMouse();
        }
    }

    private void OnPreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPoint = e.GetPosition(AssociatedObject);
            
            // Get the row at the current mouse position
            var hitTestResult = VisualTreeHelper.HitTest(AssociatedObject, currentPoint);
            if (hitTestResult != null)
            {
                var row = FindVisualParent<WpfDataGridRow>(hitTestResult.VisualHit);
                if (row != null && !AssociatedObject.SelectedItems.Contains(row.Item))
                {
                    AssociatedObject.SelectedItems.Add(row.Item);
                }
            }
        }
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            AssociatedObject.ReleaseMouseCapture();
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        
        if (parentObject == null)
            return null;
        
        if (parentObject is T parent)
            return parent;
        
        return FindVisualParent<T>(parentObject);
    }
}
