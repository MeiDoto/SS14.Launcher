using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using SS14.Launcher.Localization;
using SS14.Launcher.Utility;
using SS14.Launcher.Utility.Network;

namespace SS14.Launcher.ViewModels;

public sealed class DiagnosticsStepViewModel : ViewModelBase
{
    private readonly DiagnosticsStep _step;

    public string Name => _step.Name;

    private DiagnosticsStepStatus _status;
    public DiagnosticsStepStatus Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            OnPropertyChanged(nameof(StatusBadge));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    private string _details = "";
    public string Details
    {
        get => _details;
        set => SetProperty(ref _details, value);
    }

    private long _elapsedMs;
    public long ElapsedMs
    {
        get => _elapsedMs;
        set
        {
            SetProperty(ref _elapsedMs, value);
            OnPropertyChanged(nameof(ElapsedFormatted));
        }
    }

    public string ElapsedFormatted => _elapsedMs > 0 ? $"{_elapsedMs} ms" : "";

    public string StatusBadge => Status switch
    {
        DiagnosticsStepStatus.Running => "⏳",
        DiagnosticsStepStatus.Success => "✅",
        DiagnosticsStepStatus.Warning => "⚠️",
        DiagnosticsStepStatus.Failed => "❌",
        _ => "⚪"
    };

    public string StatusColor => Status switch
    {
        DiagnosticsStepStatus.Success => "#4CAF50",
        DiagnosticsStepStatus.Warning => "#FFC107",
        DiagnosticsStepStatus.Failed => "#F44336",
        DiagnosticsStepStatus.Running => "#2196F3",
        _ => "#888888"
    };

    public DiagnosticsStepViewModel(DiagnosticsStep step)
    {
        _step = step;
        _status = step.Status;
        _details = step.Details;
        _elapsedMs = step.ElapsedMilliseconds;
    }

    public void UpdateFromModel()
    {
        Status = _step.Status;
        Details = _step.Details;
        ElapsedMs = _step.ElapsedMilliseconds;
    }
}

public sealed class NetworkDiagnosticsViewModel : ViewModelBase
{
    private readonly NetworkDiagnosticsRunner _runner = new();
    private CancellationTokenSource? _cts;

    private string _targetAddress = "ss14s://central.spacestation14.io:1212";
    public string TargetAddress
    {
        get => _targetAddress;
        set => SetProperty(ref _targetAddress, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            SetProperty(ref _isRunning, value);
            OnPropertyChanged(nameof(CanRun));
        }
    }

    public bool CanRun => !IsRunning && !string.IsNullOrWhiteSpace(TargetAddress);

    private string _statusSummary = "";
    public string StatusSummary
    {
        get => _statusSummary;
        private set => SetProperty(ref _statusSummary, value);
    }

    private string _reportText = "";
    public string ReportText
    {
        get => _reportText;
        private set
        {
            SetProperty(ref _reportText, value);
            OnPropertyChanged(nameof(HasReport));
        }
    }

    public bool HasReport => !string.IsNullOrEmpty(_reportText);

    public ObservableCollection<DiagnosticsStepViewModel> Steps { get; } = new();

    public NetworkDiagnosticsViewModel()
    {
    }

    public NetworkDiagnosticsViewModel(string initialTarget)
    {
        TargetAddress = initialTarget;
    }

    public async Task RunDiagnosticsAsync()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        StatusSummary = LocalizationManager.Instance.GetString("diagnostics-status-running");
        ReportText = "";
        Steps.Clear();

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var report = await _runner.RunDiagnosticsAsync(
                TargetAddress,
                step =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var existing = FindStepVm(step.Name);
                        if (existing == null)
                        {
                            var newVm = new DiagnosticsStepViewModel(step);
                            newVm.UpdateFromModel();
                            Steps.Add(newVm);
                        }
                        else
                        {
                            existing.UpdateFromModel();
                        }
                    });
                },
                _cts.Token);

            ReportText = report.GenerateFormattedReport();
            StatusSummary = report.IsSuccess
                ? LocalizationManager.Instance.GetString("diagnostics-status-passed")
                : LocalizationManager.Instance.GetString("diagnostics-status-issues-found");
        }
        catch (OperationCanceledException)
        {
            StatusSummary = LocalizationManager.Instance.GetString("diagnostics-status-cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error in NetworkDiagnosticsViewModel");
            StatusSummary = LocalizationManager.Instance.GetString("diagnostics-status-error", ("err", ex.Message));
        }
        finally
        {
            IsRunning = false;
        }
    }

    private DiagnosticsStepViewModel? FindStepVm(string name)
    {
        foreach (var step in Steps)
        {
            if (step.Name == name)
                return step;
        }
        return null;
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public async Task CopyReportToClipboard()
    {
        if (string.IsNullOrEmpty(ReportText))
            return;

        await ClipboardHelper.SetTextAsync(ReportText);
    }
}
