using System.Windows;
using ChronoView.Models;
using ChronoView.UI.ViewModels;

namespace ChronoView.UI.Views;

/// <summary>
/// Interaction logic for SettingsDialog.xaml
/// </summary>
public partial class SettingsDialog : Window
{
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
}
