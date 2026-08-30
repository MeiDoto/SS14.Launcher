using System;
using SS14.Launcher.Localization;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class LauncherUpdatePromptOverlayViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _windowVm;
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly LauncherUpdateInfo _updateInfo;

    public string TitleText => _loc.GetString("launcher-update-available-title");
    public string MessageText => _loc.GetString("launcher-update-available-message", ("version", _updateInfo.TagName));
    public string CurrentVersionText => _loc.GetString("launcher-update-current-version", ("version", "v" + ConfigConstants.LauncherCustomVersion));
    public string ReleaseNotes => _updateInfo.ReleaseNotes;
    public bool HasReleaseNotes => !string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes);

    public LauncherUpdatePromptOverlayViewModel(MainWindowViewModel windowVm, LauncherUpdateInfo updateInfo)
    {
        _windowVm = windowVm;
        _updateInfo = updateInfo;
    }

    public void UpdateNow()
    {
        _windowVm.OverlayViewModel = new LauncherUpdateProgressOverlayViewModel(_windowVm, _updateInfo);
    }

    public void RemindLater()
    {
        LauncherUpdateManager.Instance.HasDismissedStartupPrompt = true;
        _windowVm.OverlayViewModel = null;
    }

    /// <summary>
    /// Пропустить конкретно эту версию — больше не спрашивать до следующей.
    /// </summary>
    public void SkipThisVersion()
    {
        LauncherUpdateManager.Instance.SkipVersion(_updateInfo.TagName);
        LauncherUpdateManager.Instance.HasDismissedStartupPrompt = true;
        _windowVm.OverlayViewModel = null;
    }
}
