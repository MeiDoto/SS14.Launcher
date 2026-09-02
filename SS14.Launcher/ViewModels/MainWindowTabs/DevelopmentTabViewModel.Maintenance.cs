using System;
using System.Diagnostics;
using System.IO;
using Serilog;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Utility;
using Splat;
using System.Threading.Tasks;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// Maintenance operations: clearing engines, servers, logs, caches, CVars, and opening folders.
/// </summary>
public sealed partial class DevelopmentTabViewModel
{
    /// <summary>
    /// Removes all locally installed engine versions.
    /// </summary>
    public void ClearAllInstalledEngines()
    {
        try
        {
            _engineManager.ClearAllEngines();
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-cleared-engines");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-engines", ("error", e.Message));
        }
    }

    /// <summary>
    /// Removes all installed engines and server content bundles.
    /// </summary>
    public async Task ClearAllInstalledServers()
    {
        try
        {
            _engineManager.ClearAllEngines();
            await _contentManager.ClearAll();
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-cleared-content");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-content", ("error", e.Message));
        }
    }

    /// <summary>
    /// Deletes all log files from the launcher log directory.
    /// </summary>
    public void ClearLogs()
    {
        try
        {
            if (Directory.Exists(LauncherPaths.DirLogs))
            {
                foreach (var file in Directory.GetFiles(LauncherPaths.DirLogs))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Failed to delete log file {File}", file);
                    }
                }
            }
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-cleared-logs");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-logs", ("error", e.Message));
        }
    }

    /// <summary>
    /// Opens the user data directory in the system file manager.
    /// </summary>
    public void OpenUserDataFolder()
    {
        try
        {
            Directory.CreateDirectory(LauncherPaths.DirUserData);
            Process.Start(new ProcessStartInfo
            {
                FileName = LauncherPaths.DirUserData,
                UseShellExecute = true
            });
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-opened-user-data");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-folder", ("error", e.Message));
        }
    }

    /// <summary>
    /// Opens the logs directory in the system file manager.
    /// </summary>
    public void OpenLogsFolder()
    {
        try
        {
            Directory.CreateDirectory(LauncherPaths.DirLogs);
            Process.Start(new ProcessStartInfo
            {
                FileName = LauncherPaths.DirLogs,
                UseShellExecute = true
            });
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-opened-logs");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-logs-folder", ("error", e.Message));
        }
    }

    /// <summary>
    /// Clears stored server playtime statistics.
    /// </summary>
    public void ClearServerPlaytime()
    {
        try
        {
            _cfg.SetCVar(CVars.ServerPlaytime, "{}");
            _cfg.CommitConfig();
            ActionStatus = _loc.GetString("tab-dev-cleared-playtime");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }

    /// <summary>
    /// Clears saved watched server slot notifications.
    /// </summary>
    public void ClearWatchedSlots()
    {
        try
        {
            _cfg.SetCVar(CVars.WatchedSlotServers, "[]");
            _ = _cfg.CommitConfig();
            ActionStatus = _loc.GetString("tab-dev-cleared-slots");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }

    /// <summary>
    /// Deletes the cached news feed JSON file.
    /// </summary>
    public void ClearNewsCache()
    {
        try
        {
            var file = Path.Combine(LauncherPaths.DirLocalData, "news_cache.json");
            if (File.Exists(file))
                File.Delete(file);
            ActionStatus = _loc.GetString("tab-dev-cleared-news-cache");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }

    /// <summary>
    /// Resets all configuration variables to their default values.
    /// </summary>
    public void ResetAllCVarsToDefaults()
    {
        try
        {
            _cfg.ResetAllCVarsToDefault();
            ActionStatus = _loc.GetString("tab-dev-cleared-cvars");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }
}
