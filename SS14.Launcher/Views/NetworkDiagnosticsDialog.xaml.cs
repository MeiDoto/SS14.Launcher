using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class NetworkDiagnosticsDialog : Window
{
    public NetworkDiagnosticsDialog()
    {
        InitializeComponent();
    }

    public NetworkDiagnosticsDialog(string targetAddress) : this()
    {
        if (DataContext is NetworkDiagnosticsViewModel vm)
        {
            vm.TargetAddress = targetAddress;
            _ = vm.RunDiagnosticsAsync();
        }
    }

    private void RunDiagnosticsClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NetworkDiagnosticsViewModel vm)
        {
            _ = vm.RunDiagnosticsAsync();
        }
    }

    private async void CopyReportClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NetworkDiagnosticsViewModel vm)
        {
            await vm.CopyReportToClipboard();
        }
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NetworkDiagnosticsViewModel vm)
        {
            vm.Cancel();
        }
        Close();
    }
}
