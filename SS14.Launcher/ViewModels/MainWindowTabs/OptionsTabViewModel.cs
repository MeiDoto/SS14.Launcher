using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public class OptionsTabViewModel : MainWindowTabViewModel
{
    public DataManager Cfg { get; }
    private readonly IEngineManager _engineManager;
    private readonly ContentManager _contentManager;

    public LanguageSelectorViewModel Language { get; } = new();

    public OptionsTabViewModel()
    {
        Cfg = Locator.Current.GetRequiredService<DataManager>();
        _engineManager = Locator.Current.GetRequiredService<IEngineManager>();
        _contentManager = Locator.Current.GetRequiredService<ContentManager>();

        DisableIncompatibleMacOS = OperatingSystem.IsMacOS();
    }
    public bool DisableIncompatibleMacOS { get; }
    public bool IsDesktopShortcutSupported => DesktopIntegration.IsSupported;

    public override void Selected()
    {
        base.Selected();
        RefreshStorageUsageAsync();
    }

    public override string Name
    {
        get
        {
            var custom = Cfg.GetCVar(CVars.CustomOptionsTabName);
            return !string.IsNullOrWhiteSpace(custom) ? custom : LocalizationManager.Instance.GetString("tab-options-title");
        }
    }

    public bool CompatMode
    {
        get => Cfg.GetCVar(CVars.CompatMode);
        set
        {
            Cfg.SetCVar(CVars.CompatMode, value);
            Cfg.CommitConfig();
        }
    }

    public bool LogLauncherVerbose
    {
        get => Cfg.GetCVar(CVars.LogLauncherVerbose);
        set
        {
            Cfg.SetCVar(CVars.LogLauncherVerbose, value);
            Cfg.CommitConfig();
        }
    }

    public bool OverrideAssets
    {
        get => Cfg.GetCVar(CVars.OverrideAssets);
        set
        {
            Cfg.SetCVar(CVars.OverrideAssets, value);
            Cfg.CommitConfig();
        }
    }

    public bool MultiAccounts
    {
        get => Cfg.GetCVar(CVars.MultiAccounts);
        set
        {
            Cfg.SetCVar(CVars.MultiAccounts, value);
            Cfg.CommitConfig();
        }
    }

    public bool EnableTieredPGO
    {
        get => Cfg.GetCVar(CVars.EnableTieredPGO);
        set
        {
            Cfg.SetCVar(CVars.EnableTieredPGO, value);
            Cfg.CommitConfig();
        }
    }

    public bool ForceServerGC
    {
        get => Cfg.GetCVar(CVars.ForceServerGC);
        set
        {
            Cfg.SetCVar(CVars.ForceServerGC, value);
            Cfg.CommitConfig();
        }
    }

    public bool EnableFastPing
    {
        get => Cfg.GetCVar(CVars.EnableFastPing);
        set
        {
            Cfg.SetCVar(CVars.EnableFastPing, value);
            Cfg.CommitConfig();
        }
    }

    public bool HighProcessPriority
    {
        get => Cfg.GetCVar(CVars.HighProcessPriority);
        set
        {
            Cfg.SetCVar(CVars.HighProcessPriority, value);
            Cfg.CommitConfig();
        }
    }

    public bool ForceDedicatedGpu
    {
        get => Cfg.GetCVar(CVars.ForceDedicatedGpu);
        set
        {
            Cfg.SetCVar(CVars.ForceDedicatedGpu, value);
            Cfg.CommitConfig();
        }
    }

    public bool MaxPerformanceJit
    {
        get => Cfg.GetCVar(CVars.MaxPerformanceJit);
        set
        {
            Cfg.SetCVar(CVars.MaxPerformanceJit, value);
            Cfg.CommitConfig();
        }
    }

    public bool LowLatencyNetworking
    {
        get => Cfg.GetCVar(CVars.LowLatencyNetworking);
        set
        {
            Cfg.SetCVar(CVars.LowLatencyNetworking, value);
            Cfg.CommitConfig();
        }
    }

    public bool DisableDiagnosticsOverhead
    {
        get => Cfg.GetCVar(CVars.DisableDiagnosticsOverhead);
        set
        {
            Cfg.SetCVar(CVars.DisableDiagnosticsOverhead, value);
            Cfg.CommitConfig();
        }
    }

    public bool LowPauseGc
    {
        get => Cfg.GetCVar(CVars.LowPauseGc);
        set
        {
            Cfg.SetCVar(CVars.LowPauseGc, value);
            Cfg.CommitConfig();
        }
    }

    public bool SmartCacheCleaner
    {
        get => Cfg.GetCVar(CVars.SmartCacheCleaner);
        set
        {
            Cfg.SetCVar(CVars.SmartCacheCleaner, value);
            Cfg.CommitConfig();
        }
    }

    public bool FastLaunchPreload
    {
        get => Cfg.GetCVar(CVars.FastLaunchPreload);
        set
        {
            Cfg.SetCVar(CVars.FastLaunchPreload, value);
            Cfg.CommitConfig();
        }
    }

    public bool DnsOverHttps
    {
        get => Cfg.GetCVar(CVars.DnsOverHttps);
        set
        {
            Cfg.SetCVar(CVars.DnsOverHttps, value);
            Cfg.CommitConfig();
        }
    }

    public async Task<int> RunSmartCleaner()
    {
        return await _contentManager.RunSmartCleanerAsync();
    }

    public bool ShowDevelopmentTab
    {
        get => Cfg.GetCVar(CVars.ShowDevelopmentTab);
        set
        {
            Cfg.SetCVar(CVars.ShowDevelopmentTab, value);
            Cfg.CommitConfig();
        }
    }

    public bool ShowNewsTab
    {
        get => Cfg.GetCVar(CVars.ShowNewsTab);
        set
        {
            Cfg.SetCVar(CVars.ShowNewsTab, value);
            Cfg.CommitConfig();
        }
    }

    public bool ShowReplaysTab
    {
        get => Cfg.GetCVar(CVars.ShowReplaysTab);
        set
        {
            Cfg.SetCVar(CVars.ShowReplaysTab, value);
            Cfg.CommitConfig();
        }
    }

    public bool TrackPlaytime
    {
        get => Cfg.GetCVar(CVars.TrackPlaytime);
        set
        {
            Cfg.SetCVar(CVars.TrackPlaytime, value);
            Cfg.CommitConfig();
        }
    }

    public bool EnableSlotNotifier
    {
        get => Cfg.GetCVar(CVars.EnableSlotNotifier);
        set
        {
            Cfg.SetCVar(CVars.EnableSlotNotifier, value);
            Cfg.CommitConfig();
        }
    }

    public void TestNotification()
    {
        var title = LocalizationManager.Instance.GetString("notification-test-title");
        var msg = LocalizationManager.Instance.GetString("notification-test-desc");
        if (string.IsNullOrWhiteSpace(title))
            title = "Space Station 14";
        if (string.IsNullOrWhiteSpace(msg))
            msg = "Оповещения на рабочем столе работают исправно!";
        DesktopNotificationManager.Notify(title, msg);
    }

    public void ClearEngines()
    {
        _engineManager.ClearAllEngines();
    }

    public async Task<bool> ClearServerContent()
    {
        return await _contentManager.ClearAll();
    }

    public async Task<(bool IntegrityOk, int CleanedOrphans)> VerifyAndOptimizeDatabase()
    {
        return await _contentManager.VerifyAndOptimizeDatabaseAsync();
    }

    public void OpenLogDirectory()
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = LauncherPaths.DirLogs
        });
    }

    public void OpenUserDataDirectory()
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = LauncherPaths.DirUserData
        });
    }

    private string _storageUsageText = "";
    public string StorageUsageText
    {
        get => _storageUsageText;
        set => SetProperty(ref _storageUsageText, value);
    }

    public async void RefreshStorageUsageAsync()
    {
        StorageUsageText = "...";
        var report = await Task.Run(() =>
        {
            try
            {
                long GetDirSize(string dir)
                {
                    if (!Directory.Exists(dir)) return 0;
                    long size = 0;
                    foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        try { size += new FileInfo(f).Length; } catch { }
                    }
                    return size;
                }

                long engines = GetDirSize(LauncherPaths.DirEngineInstallations);
                long logs = GetDirSize(LauncherPaths.DirLogs);
                long replays = GetDirSize(System.IO.Path.Combine(LauncherPaths.DirUserData, "replays"));
                long dbSize = File.Exists(LauncherPaths.PathContentDb) ? new FileInfo(LauncherPaths.PathContentDb).Length : 0;
                long total = engines + logs + replays + dbSize;

                string Fmt(long bytes) => bytes >= 1024 * 1024 * 1024
                    ? $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB"
                    : $"{(bytes / (1024.0 * 1024.0)):F1} MB";

                return $"💾 Диск: Всего {Fmt(total)} (Контент: {Fmt(dbSize)}, Движки: {Fmt(engines)}, Реплеи: {Fmt(replays)}, Логи: {Fmt(logs)})";
            }
            catch
            {
                return "";
            }
        });

        StorageUsageText = report;
    }

    public void OpenAccountSettings()
    {
        Helpers.OpenUri(ConfigConstants.AccountManagementUrl);
    }

    private string _updateStatusText = "";
    private bool _isCheckingUpdate;
    private bool _hasAvailableUpdate;
    private string _availableUpdateVersion = "";

    public string LauncherVersionDisplay => $"v{ConfigConstants.LauncherCustomVersion} (Custom Edition, .NET 10.0)";

    public string UpdateStatusText
    {
        get => _updateStatusText;
        set => SetProperty(ref _updateStatusText, value);
    }

    public bool IsCheckingUpdate
    {
        get => _isCheckingUpdate;
        set => SetProperty(ref _isCheckingUpdate, value);
    }

    public bool HasAvailableUpdate
    {
        get => _hasAvailableUpdate;
        set => SetProperty(ref _hasAvailableUpdate, value);
    }

    public string AvailableUpdateVersion
    {
        get => _availableUpdateVersion;
        set => SetProperty(ref _availableUpdateVersion, value);
    }

    public async Task CheckForUpdates()
    {
        IsCheckingUpdate = true;
        UpdateStatusText = LocalizationManager.Instance.GetString("tab-options-checking-updates");
        try
        {
            // ручная проверка — сбрасываем кулдаун и пропущенную версию
            LauncherUpdateManager.Instance.ResetCooldown();
            LauncherUpdateManager.Instance.ClearSkippedVersion();

            var update = await LauncherUpdateManager.Instance.CheckForUpdatesAsync();
            if (update != null)
            {
                HasAvailableUpdate = true;
                AvailableUpdateVersion = update.TagName;
                UpdateStatusText = LocalizationManager.Instance.GetString("tab-options-update-available-text", ("version", update.TagName));
            }
            else
            {
                HasAvailableUpdate = false;
                UpdateStatusText = LocalizationManager.Instance.GetString("tab-options-up-to-date-text");
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText = LocalizationManager.Instance.GetString("tab-options-update-error", ("error", ex.Message));
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    public void ApplyAvailableUpdate()
    {
        if (LauncherUpdateManager.Instance.CachedUpdate is { } update)
        {
            var mainVm = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.DataContext as MainWindowViewModel;
            if (mainVm != null)
            {
                mainVm.OverlayViewModel = new LauncherUpdateProgressOverlayViewModel(mainVm, update);
            }
        }
    }
}
