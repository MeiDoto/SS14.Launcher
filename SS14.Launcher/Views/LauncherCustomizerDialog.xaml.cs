using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class LauncherCustomizerDialog : Window
{
    private readonly LauncherCustomizerViewModel _viewModel;

    public LauncherCustomizerDialog()
    {
        InitializeComponent();

        _viewModel = (DataContext as LauncherCustomizerViewModel)!;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        _viewModel.Populate();
    }

    private bool _saved = false;

    private void Done(object? sender, RoutedEventArgs args)
    {
        _saved = true;
        _viewModel.Save();
        if (Owner?.DataContext is MainWindowViewModel mainVm)
        {
            mainVm.ReloadCustomVisuals();
        }
        else if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                 desktop.MainWindow?.DataContext is MainWindowViewModel desktopMainVm)
        {
            desktopMainVm.ReloadCustomVisuals();
        }
        Close();
    }

    private void Cancel(object? sender, RoutedEventArgs args)
    {
        _viewModel.RestoreInitialSnapshot();
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!_saved)
        {
            _viewModel.RestoreInitialSnapshot();
        }
    }

    private void ResetClicked(object? sender, RoutedEventArgs args)
    {
        _viewModel.Reset();
    }

    private async void PickBackgroundImage(object? sender, RoutedEventArgs args)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Background (Image, GIF or Video)",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("All Supported Media (*.png, *.jpg, *.webp, *.gif, *.mp4, *.webm, *.mkv, *.avi)")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.apng", "*.mp4", "*.webm", "*.mkv", "*.avi", "*.mov", "*.flv", "*.wmv"]
                },
                new FilePickerFileType("Animated GIFs & WebP (*.gif, *.webp, *.apng)")
                {
                    Patterns = ["*.gif", "*.webp", "*.apng"]
                },
                new FilePickerFileType("Videos (*.mp4, *.webm, *.mkv, *.avi, *.mov)")
                {
                    Patterns = ["*.mp4", "*.webm", "*.mkv", "*.avi", "*.mov", "*.flv", "*.wmv"]
                },
                new FilePickerFileType("Images (*.png, *.jpg, *.jpeg, *.bmp)")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"]
                }
            ]
        });

        if (result.Count > 0)
        {
            var path = result[0].Path.LocalPath;
            _viewModel.CustomBackgroundImagePath = path;
        }
    }

    private async void PickLogoImage(object? sender, RoutedEventArgs args)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Logo Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images (*.png, *.jpg, *.jpeg, *.webp)")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"]
                }
            ]
        });

        if (result.Count > 0)
        {
            var path = result[0].Path.LocalPath;
            _viewModel.CustomLogoImagePath = path;
        }
    }
}
