using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using SS14.Launcher.Localization;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class LauncherUpdateProgressOverlayViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _windowVm;
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly LauncherUpdateInfo _updateInfo;
    private readonly CancellationTokenSource _cts = new();

    private double _progress;
    private string _statusText = "";
    private string _progressText = "";
    private string _speedText = "";
    private bool _progressIndeterminate;
    private bool _isErrored;

    public string TitleText => _loc.GetString("launcher-update-downloading-title");

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public string SpeedText
    {
        get => _speedText;
        private set => SetProperty(ref _speedText, value);
    }

    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        private set => SetProperty(ref _progressIndeterminate, value);
    }

    public bool IsErrored
    {
        get => _isErrored;
        private set => SetProperty(ref _isErrored, value);
    }

    public LauncherUpdateProgressOverlayViewModel(MainWindowViewModel windowVm, LauncherUpdateInfo updateInfo)
    {
        _windowVm = windowVm;
        _updateInfo = updateInfo;
        _statusText = _loc.GetString("launcher-update-status-downloading");

        StartDownload();
    }

    private void StartDownload()
    {
        var progress = new Progress<(long Downloaded, long Total, double SpeedBytesPerSec)>(report =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (report.Total > 0)
                {
                    Progress = (double)report.Downloaded / report.Total;
                    var downMb = report.Downloaded / (1024.0 * 1024.0);
                    var totalMb = report.Total / (1024.0 * 1024.0);
                    ProgressText = $"{downMb:F1} / {totalMb:F1} MB ({Progress:P0})";
                    ProgressIndeterminate = false;
                }
                else
                {
                    var downMb = report.Downloaded / (1024.0 * 1024.0);
                    ProgressText = $"{downMb:F1} MB";
                    ProgressIndeterminate = true;
                }

                var speedMb = report.SpeedBytesPerSec / (1024.0 * 1024.0);
                SpeedText = $"{speedMb:F1} MB/s";
            });
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await LauncherUpdateManager.Instance.DownloadAndApplyUpdateAsync(_updateInfo, progress, _cts.Token);
                Dispatcher.UIThread.Post(() =>
                {
                    StatusText = _loc.GetString("launcher-update-status-installing");
                    ProgressIndeterminate = true;
                });
            }
            catch (OperationCanceledException)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _windowVm.OverlayViewModel = null;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsErrored = true;
                    StatusText = _loc.GetString("launcher-update-error-failed", ("error", ex.Message));
                });
            }
        });
    }

    public void Cancel()
    {
        _cts.Cancel();
        _windowVm.OverlayViewModel = null;
    }

    public void DismissError()
    {
        _windowVm.OverlayViewModel = null;
    }
}
