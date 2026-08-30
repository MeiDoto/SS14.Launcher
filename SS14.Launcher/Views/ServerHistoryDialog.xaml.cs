using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class ServerHistoryDialog : Window
{
    public ServerHistoryDialog()
    {
        InitializeComponent();

        if (DataContext is ServerHistoryViewModel vm)
        {
            vm.RequestClose += Close;
        }

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ServerHistoryViewModel newVm)
            {
                newVm.RequestClose += Close;
            }
        };
    }

    public void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
