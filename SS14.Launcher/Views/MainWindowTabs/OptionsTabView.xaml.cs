using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Utility;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Views.MainWindowTabs;

public partial class OptionsTabView : UserControl
{
    public OptionsTabView()
    {
        InitializeComponent();
    }

    private void Flip(object? o, RoutedEventArgs routedEventArgs)
    {
        var window = (Window?) VisualRoot;
        if (window == null)
            return;

        window.Classes.Add("DoAFlip");

        DispatcherTimer.RunOnce(() => { window.Classes.Remove("DoAFlip"); }, TimeSpan.FromSeconds(1));
    }

    public async void ClearEnginesPressed(object? _1, RoutedEventArgs _2)
    {
        ((OptionsTabViewModel)DataContext!).ClearEngines();
        await ClearEnginesButton.DisplayDoneMessage();
    }

    public async void ClearServerContentPressed(object? _1, RoutedEventArgs _2)
    {
        var blocked = !await ((OptionsTabViewModel)DataContext!).ClearServerContent();
        var locMgr = Locator.Current.GetService<LocalizationManager>()!;

        await ClearServerContentButton.DisplayDoneMessage(
            blocked ? locMgr.GetString("tab-options-clear-content-close-client") : null);
    }

    public async void OpenAccountInfo(object? sender, RoutedEventArgs args)
    {
        await new AccountInfoDialog().ShowDialog((Window)this.GetVisualRoot()!);
    }

    public async void OpenLauncherCustomizer(object? sender, RoutedEventArgs args)
    {
        await new LauncherCustomizerDialog().ShowDialog((Window)this.GetVisualRoot()!);
    }

    public async void OpenLogViewer(object? sender, RoutedEventArgs args)
    {
        await new LogViewerDialog().ShowDialog((Window)this.GetVisualRoot()!);
    }

    public async void OpenLocalBuilds(object? sender, RoutedEventArgs args)
    {
        await new LocalBuildsDialog().ShowDialog((Window)this.GetVisualRoot()!);
    }

    public async void SmartCleanPressed(object? sender, RoutedEventArgs args)
    {
        var count = await ((OptionsTabViewModel)DataContext!).RunSmartCleaner();
        await SmartCleanButton.DisplayDoneMessage(LocalizationManager.Instance.GetString("button-done"));
    }

    public async void OpenHubSettings(object? sender, RoutedEventArgs args)
    {
        await new HubSettingsDialog().ShowDialog((Window)this.GetVisualRoot()!);
    }

    public async void OpenProxySettings(object? sender, RoutedEventArgs args)
    {
        await new ProxySettingsDialog().ShowDialog((Window)this.GetVisualRoot()!);
    }

    public async void CheckForUpdatesPressed(object? sender, RoutedEventArgs args)
    {
        if (DataContext is OptionsTabViewModel vm)
        {
            await vm.CheckForUpdates();
        }
    }

    public void ApplyUpdatePressed(object? sender, RoutedEventArgs args)
    {
        if (DataContext is OptionsTabViewModel vm)
        {
            vm.ApplyAvailableUpdate();
        }
    }

    public void CreateDesktopShortcutPressed(object? sender, RoutedEventArgs args)
    {
        var (success, msg) = DesktopIntegration.CreateDesktopAndMenuShortcuts();
        if (DataContext is OptionsTabViewModel vm)
        {
            vm.UpdateStatusText = success
                ? LocalizationManager.Instance.GetString("tab-options-desktop-shortcut-success")
                : LocalizationManager.Instance.GetString("tab-options-desktop-shortcut-error", ("error", msg));
        }
    }

    public async void VerifyDatabasePressed(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not OptionsTabViewModel vm) return;
        var btn = this.FindControl<Button>("VerifyDatabaseButton");
        if (btn != null) btn.IsEnabled = false;

        try
        {
            var (ok, cleaned) = await vm.VerifyAndOptimizeDatabase();
            if (btn != null)
            {
                btn.Content = ok
                    ? LocalizationManager.Instance.GetString("tab-options-db-integrity-ok", ("cleaned", cleaned))
                    : LocalizationManager.Instance.GetString("tab-options-db-integrity-error");
            }
        }
        finally
        {
            if (btn != null) btn.IsEnabled = true;
        }
    }

    public void TestNotificationPressed(object? sender, RoutedEventArgs args)
    {
        if (DataContext is OptionsTabViewModel vm)
        {
            vm.TestNotification();
        }
        else
        {
            var title = LocalizationManager.Instance.GetString("notification-test-title");
            var msg = LocalizationManager.Instance.GetString("notification-test-desc");
            DesktopNotificationManager.Notify(title, msg);
        }
    }
}
