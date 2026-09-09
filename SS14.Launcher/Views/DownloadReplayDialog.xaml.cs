using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class DownloadReplayDialog : Window
{
    public DownloadReplayDialog()
    {
        InitializeComponent();
    }

    public DownloadReplayDialog(string targetDirectory, Action<string>? onPlay = null, Action? onCompleted = null) : this()
    {
        DataContext = new DownloadReplayViewModel(targetDirectory, onPlay, onCompleted);
    }

    private async void StartDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadReplayViewModel vm)
        {
            await vm.StartDownloadAsync();
        }
    }

    private void CancelDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadReplayViewModel vm)
        {
            vm.Cancel();
        }
    }

    private async void PasteClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadReplayViewModel vm)
        {
            await vm.PasteUrlFromClipboard();
        }
    }

    private void PlayClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadReplayViewModel vm)
        {
            vm.PlayNow();
        }
        Close();
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadReplayViewModel vm && vm.IsDownloading)
        {
            vm.Cancel();
        }
        Close();
    }
}
