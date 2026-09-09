using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Serilog;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class DownloadReplayViewModel : ViewModelBase
{
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly string _targetDirectory;
    private readonly Action<string>? _onPlay;
    private readonly Action? _onCompleted;
    private CancellationTokenSource? _cancelTokenSource;

    private bool _isByRoundId = true;
    public bool IsByRoundId
    {
        get => _isByRoundId;
        set
        {
            if (SetProperty(ref _isByRoundId, value))
            {
                OnPropertyChanged(nameof(IsByUrl));
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public bool IsByUrl
    {
        get => !_isByRoundId;
        set => IsByRoundId = !value;
    }

    private string _roundIdInput = "";
    public string RoundIdInput
    {
        get => _roundIdInput;
        set
        {
            if (SetProperty(ref _roundIdInput, value))
            {
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    private string _urlInput = "";
    public string UrlInput
    {
        get => _urlInput;
        set
        {
            if (SetProperty(ref _urlInput, value))
            {
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    private int _selectedProviderIndex = 0;
    public int SelectedProviderIndex
    {
        get => _selectedProviderIndex;
        set
        {
            if (SetProperty(ref _selectedProviderIndex, value))
            {
                OnPropertyChanged(nameof(IsCustomProvider));
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public bool IsCustomProvider => SelectedProviderIndex == 1;

    private string _customTemplateInput = "https://example.com/replays/round_{roundId}.zip";
    public string CustomTemplateInput
    {
        get => _customTemplateInput;
        set
        {
            if (SetProperty(ref _customTemplateInput, value))
            {
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    private bool _isDownloading;
    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(CanDownload));
                OnPropertyChanged(nameof(CanChangeInputs));
            }
        }
    }

    public bool CanChangeInputs => !IsDownloading && !IsSuccess;

    public bool CanDownload
    {
        get
        {
            if (IsDownloading || IsSuccess)
                return false;

            if (IsByRoundId)
            {
                if (string.IsNullOrWhiteSpace(RoundIdInput))
                    return false;

                if (IsCustomProvider)
                {
                    return !string.IsNullOrWhiteSpace(CustomTemplateInput) &&
                           CustomTemplateInput.Contains("{roundId}", StringComparison.OrdinalIgnoreCase);
                }

                return true;
            }

            return Uri.TryCreate(UrlInput, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        private set => SetProperty(ref _downloadProgress, value);
    }

    private string _downloadProgressText = "";
    public string DownloadProgressText
    {
        get => _downloadProgressText;
        private set => SetProperty(ref _downloadProgressText, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private bool _isSuccess;
    public bool IsSuccess
    {
        get => _isSuccess;
        private set
        {
            if (SetProperty(ref _isSuccess, value))
            {
                OnPropertyChanged(nameof(CanChangeInputs));
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public string? DownloadedFilePath { get; private set; }

    public DownloadReplayViewModel(string targetDirectory, Action<string>? onPlay = null, Action? onCompleted = null)
    {
        _targetDirectory = targetDirectory;
        _onPlay = onPlay;
        _onCompleted = onCompleted;
    }

    public async Task PasteUrlFromClipboard()
    {
        var text = await ClipboardHelper.GetTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            text = text.Trim();
            if (IsByUrl)
            {
                UrlInput = text;
            }
            else
            {
                // If it's a number, paste into RoundId
                if (int.TryParse(text.TrimStart('#'), out _))
                {
                    RoundIdInput = text.TrimStart('#');
                }
                else
                {
                    // If it's a full URL, automatically switch to ByUrl
                    UrlInput = text;
                    IsByUrl = true;
                }
            }
        }
    }

    public async Task StartDownloadAsync()
    {
        if (!CanDownload)
            return;

        IsDownloading = true;
        StatusMessage = _loc.GetString("replay-download-status-connecting");
        DownloadProgress = 0;
        DownloadProgressText = "";

        _cancelTokenSource = new CancellationTokenSource();

        string targetUrl;
        try
        {
            if (IsByRoundId)
            {
                var preset = (ReplayProviderPreset)SelectedProviderIndex;
                targetUrl = await ReplayDownloader.ResolveUrlFromRoundIdAsync(
                    RoundIdInput,
                    preset,
                    CustomTemplateInput,
                    cancellationToken: _cancelTokenSource.Token);
            }
            else
            {
                targetUrl = ReplayDownloader.NormalizeDownloadUrl(UrlInput.Trim());
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            IsDownloading = false;
            return;
        }

        var progressReporter = new Progress<ReplayDownloadProgress>(p =>
        {
            DownloadProgress = p.ProgressPercentage;
            DownloadProgressText = p.FormattedProgress;
            StatusMessage = _loc.GetString("replay-download-status-downloading");
        });

        try
        {
            var savedPath = await ReplayDownloader.DownloadReplayAsync(
                targetUrl,
                _targetDirectory,
                progressReporter,
                cancellationToken: _cancelTokenSource.Token);

            DownloadedFilePath = savedPath;
            IsSuccess = true;
            StatusMessage = _loc.GetString("replay-download-status-success");
            _onCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _loc.GetString("replay-download-status-canceled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download replay from {Url}", targetUrl);
            StatusMessage = _loc.GetString("replay-download-status-failed", ("error", ex.Message));
        }
        finally
        {
            IsDownloading = false;
            _cancelTokenSource?.Dispose();
            _cancelTokenSource = null;
        }
    }

    public void Cancel()
    {
        _cancelTokenSource?.Cancel();
    }

    public void PlayNow()
    {
        if (DownloadedFilePath != null && File.Exists(DownloadedFilePath))
        {
            _onPlay?.Invoke(DownloadedFilePath);
        }
    }
}
