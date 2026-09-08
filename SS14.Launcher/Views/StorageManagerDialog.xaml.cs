using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class StorageManagerDialog : Window
{
    public StorageManagerDialog()
    {
        InitializeComponent();
    }

    private async void RefreshClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StorageManagerViewModel vm)
        {
            await vm.RefreshStorageAsync();
        }
    }

    private async void DeleteEngineClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StorageManagerViewModel vm)
        {
            await vm.DeleteSelectedEngineAsync();
        }
    }

    private async void CleanOldContentClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StorageManagerViewModel vm)
        {
            await vm.CleanOldContentAsync(14);
        }
    }

    private async void VacuumDbClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StorageManagerViewModel vm)
        {
            await vm.VacuumDatabaseAsync();
        }
    }

    private async void ClearLogsClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is StorageManagerViewModel vm)
        {
            await vm.ClearLogsAsync();
        }
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
