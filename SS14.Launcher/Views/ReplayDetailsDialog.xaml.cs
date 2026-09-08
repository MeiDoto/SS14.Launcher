using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class ReplayDetailsDialog : Window
{
    public ReplayDetailsDialog()
    {
        InitializeComponent();
    }

    public ReplayDetailsDialog(string filePath, Action? onPlay = null) : this()
    {
        DataContext = new ReplayDetailsViewModel(filePath, onPlay);
    }

    private void PlayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReplayDetailsViewModel vm)
        {
            vm.Play();
        }
        Close();
    }

    private void OpenFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReplayDetailsViewModel vm)
        {
            vm.OpenContainingFolder();
        }
    }

    private async void CopyPathClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReplayDetailsViewModel vm)
        {
            await vm.CopyPathToClipboard();
        }
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
