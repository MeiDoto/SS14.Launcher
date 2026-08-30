using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class ProxySettingsDialog : Window
{
    public ProxySettingsDialog()
    {
        InitializeComponent();
    }

    public async void TestClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProxySettingsViewModel vm)
        {
            await vm.TestConnectionAsync();
        }
    }

    public void SaveClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProxySettingsViewModel vm)
        {
            vm.Save();
        }
        Close();
    }

    public void CancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
