using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SS14.Launcher.Localization;
using SS14.Launcher.ViewModels;
using TerraFX.Interop.Windows;
using IDataObject = Avalonia.Input.IDataObject;

namespace SS14.Launcher.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    private MainWindowContent _content;

    public MainWindow()
    {
        InitializeComponent();

        DarkMode();

        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DragLeaveEvent, DragLeave);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);

        _content = (MainWindowContent) Content!;

        if (Icon == null && Application.Current?.Resources.TryGetResource("WindowIcon", null, out var iconObj) == true && iconObj is WindowIcon winIcon)
        {
            Icon = winIcon;
        }

        ReloadTitle();
    }

    public void ReloadContent()
    {
        ReloadTitle();

        Content = _content = new MainWindowContent();
    }

    private void ReloadTitle()
    {
        Title = LocalizationManager.Instance.GetString("main-window-title");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Control = null;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel != null)
        {
            _viewModel.Control = this;
        }

        base.OnDataContextChanged(e);
    }

    private unsafe void DarkMode()
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (TryGetPlatformHandle() is not { HandleDescriptor: "HWND" } handle)
        {
            return;
        }

        var hWnd = (HWND)handle.Handle;

        // Immersive dark mode for Windows 10 (19041+) and Windows 11 (22000+)
        int useDarkMode = 1;
        TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute(hWnd, 20, &useDarkMode, (uint) sizeof(int));

        if (Environment.OSVersion.Version.Build >= 22000)
        {
            COLORREF caption = 0x00262121;
            TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute(hWnd, 35, &caption, (uint) sizeof(COLORREF));

            COLORREF text = 0x00E0E0E0;
            TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute(hWnd, 36, &text, (uint) sizeof(COLORREF));

            // Removes the top margin of the window on Windows 11, since there's ample space after we recolor the title bar.
            Classes.Add("WindowsTitlebarColorActive");
        }
    }

    private void Drop(object? sender, DragEventArgs args)
    {
        _content.DragDropOverlay.IsVisible = false;

        if (!IsDragDropValid(args.Data))
            return;

        var file = GetDragDropFile(args.Data)!;
        _viewModel!.Dropped(file);
    }

    private void DragOver(object? sender, DragEventArgs args)
    {
        if (!IsDragDropValid(args.Data))
        {
            args.DragEffects = DragDropEffects.None;
            return;
        }

        args.DragEffects = DragDropEffects.Link;
    }

    private void DragLeave(object? sender, RoutedEventArgs args)
    {
        _content.DragDropOverlay.IsVisible = false;
    }

    private void DragEnter(object? sender, DragEventArgs args)
    {
        if (!IsDragDropValid(args.Data))
            return;

        _content.DragDropOverlay.IsVisible = true;
    }

    private bool IsDragDropValid(IDataObject dataObject)
    {
        if (_viewModel == null)
            return false;

        if (GetDragDropFile(dataObject) is not { } fileName)
            return false;

        return _viewModel.IsContentBundleDropValid(fileName);
    }

    private static IStorageFile? GetDragDropFile(IDataObject dataObject)
    {
        if (!dataObject.Contains(DataFormats.Files))
            return null;

        return dataObject.GetFiles()?.OfType<IStorageFile>().FirstOrDefault();
    }
}
