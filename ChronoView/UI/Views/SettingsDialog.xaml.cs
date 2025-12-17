using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChronoView.Models;
using ChronoView.UI.ViewModels;

namespace ChronoView.UI.Views;

/// <summary>
/// Interaction logic for SettingsDialog.xaml
/// </summary>
public partial class SettingsDialog : Window
{
    private System.Windows.Point _dragStartPoint;
    private DataSequenceItemViewModel? _draggedItem;

    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        
        viewModel.CloseRequested += (s, result) =>
        {
            DialogResult = result;
            Close();
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

   private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #region Drag-Drop Event Handlers

    private void SequenceListBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void SequenceListBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Check if left mouse button is pressed and we've moved enough to start drag
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            System.Windows.Point currentPosition = e.GetPosition(null);
            System.Windows.Vector diff = _dragStartPoint - currentPosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // Get the dragged item
                var listBox = sender as System.Windows.Controls.ListBox;
                var listBoxItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem != null && listBox != null)
                {
                    _draggedItem = listBoxItem.DataContext as DataSequenceItemViewModel;

                    if (_draggedItem != null)
                    {
                        // Create drag data
                        var dragData = new System.Windows.DataObject("SequenceItem", _draggedItem);

                        // Start drag operation
                        System.Windows.DragDrop.DoDragDrop(listBoxItem, dragData, System.Windows.DragDropEffects.Move);

                        _draggedItem = null;
                    }
                }
            }
        }
    }

    private void SequenceListBox_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        // Validate drag source
        if (!e.Data.GetDataPresent("SequenceItem"))
        {
            e.Effects = System.Windows.DragDropEffects.None;
            return;
        }

        var draggedItem = e.Data.GetData("SequenceItem") as DataSequenceItemViewModel;
        var target = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        var targetItem = target?.DataContext as DataSequenceItemViewModel;

        if (draggedItem == null || targetItem == null || draggedItem == targetItem)
        {
            e.Effects = System.Windows.DragDropEffects.None;
            return;
        }

        // Allow move
        e.Effects = System.Windows.DragDropEffects.Move;

        // Visual feedback: highlight drop target
        if (DataContext is SettingsDialogViewModel viewModel)
        {
            // Clear previous highlights
            foreach (var item in viewModel.SequenceItems)
            {
                item.IsDropTarget = false;
            }

            // Highlight current target
            targetItem.IsDropTarget = true;
        }

        e.Handled = true;
    }

    private void SequenceListBox_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var draggedItem = e.Data.GetData("SequenceItem") as DataSequenceItemViewModel;
        var target = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        var targetItem = target?.DataContext as DataSequenceItemViewModel;

        if (draggedItem == null || targetItem == null || draggedItem == targetItem)
            return;

        if (DataContext is SettingsDialogViewModel viewModel)
        {
            // Clear drop target highlight
            foreach (var item in viewModel.SequenceItems)
            {
                item.IsDropTarget = false;
            }

            // Get indices
            int oldIndex = viewModel.SequenceItems.IndexOf(draggedItem);
            int newIndex = viewModel.SequenceItems.IndexOf(targetItem);

            // Reorder using ViewModel method
            viewModel.ReorderSequenceItem(oldIndex, newIndex);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Helper method to find ancestor of a specific type in visual tree
    /// </summary>
    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T ancestor)
            {
                return ancestor;
            }
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    #endregion
}
