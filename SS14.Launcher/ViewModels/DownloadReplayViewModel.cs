using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Serilog;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class DownloadReplayViewModel : ViewModelBase
{
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#4EAF51"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF6B6B"));
    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));

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
                OnPropertyChanged(nameof(IsIndeterminate));
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

                var clean = RoundIdInput.Trim().TrimStart('#');
                return !string.IsNullOrWhiteSpace(clean) && int.TryParse(clean, out _);
            }

            return Uri.TryCreate(UrlInput, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public bool IsIndeterminate => IsDownloading && DownloadProgress <= 0;

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        private set
        {
            if (SetProperty(ref _downloadProgress, value))
            {
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }
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
                OnPropertyChanged(nameof(StatusBrush));
            }
        }
    }

    private bool _isError;
    public bool IsError
    {
        get => _isError;
        private set
        {
            if (SetProperty(ref _isError, value))
            {
                OnPropertyChanged(nameof(StatusBrush));
            }
        }
    }

    public IBrush StatusBrush => IsSuccess ? SuccessBrush : (IsError ? ErrorBrush : DefaultBrush);

    public string? DownloadedFilePath { get; private set; }

    public DownloadReplayViewModel(string targetDirectory, Action<string>? onPlay = null, Action? onCompleted = null)
    {
        _targetDirectory = targetDirectory;
        _onPlay = onPlay;
        _onCompleted = onCompleted;
        _statusMessage = _loc.GetString("replay-download-status-ready");
    }

    public async Task TryAutoPasteFromClipboardAsync()
    {
        try
        {
            var text = await ClipboardHelper.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
                return;

            text = text.Trim();

            // If it's a URL
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                if (string.IsNullOrWhiteSpace(UrlInput) && string.IsNullOrWhiteSpace(RoundIdInput))
                {
                    UrlInput = text;
                    IsByUrl = true;
                }
                return;
            }

            // If it's a round number (like 50135 or #50135 or round_50135)
            var match = Regex.Match(text, @"^(?:round[_\-\s]?|#)?(\d{2,8})$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var clean = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(RoundIdInput) && string.IsNullOrWhiteSpace(UrlInput))
                {
                    RoundIdInput = clean;
                    IsByRoundId = true;
                    SelectedProviderIndex = 0; // Space Stories
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to auto-paste from clipboard");
        }
    }

    public async Task PasteUrlFromClipboard()
    {
        var text = await ClipboardHelper.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
            return;

        text = text.Trim();

        // If it's a URL, switch to ByUrl
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            UrlInput = text;
            IsByUrl = true;
            return;
        }

        // If it's a round ID (e.g. 50135, #50135, round_50135)
        var match = Regex.Match(text, @"^(?:round[_\-\s]?|#)?(\d{1,8})$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            RoundIdInput = match.Groups[1].Value;
            IsByRoundId = true;
            return;
        }

        if (IsByUrl)
            UrlInput = text;
        else
            RoundIdInput = text.TrimStart('#').Trim();
    }

    public async Task StartDownloadAsync()
    {
        if (!CanDownload)
            return;

        IsDownloading = true;
        IsSuccess = false;
        IsError = false;
        DownloadProgress = 0;
        DownloadProgressText = "";
        OnPropertyChanged(nameof(StatusBrush));

        _cancelTokenSource = new CancellationTokenSource();

        string targetUrl;
        try
        {
            if (IsByRoundId)
            {
                var preset = (ReplayProviderPreset)SelectedProviderIndex;
                if (preset == ReplayProviderPreset.SpaceStories)
                {
                    StatusMessage = _loc.GetString("replay-download-status-resolving");
                }
                else
                {
                    StatusMessage = _loc.GetString("replay-download-status-connecting");
                }

                targetUrl = await ReplayDownloader.ResolveUrlFromRoundIdAsync(
                    RoundIdInput,
                    preset,
                    CustomTemplateInput,
                    cancellationToken: _cancelTokenSource.Token);
            }
            else
            {
                StatusMessage = _loc.GetString("replay-download-status-connecting");
                targetUrl = ReplayDownloader.NormalizeDownloadUrl(UrlInput.Trim());
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            IsDownloading = false;
            IsError = true;
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
            IsError = false;
            StatusMessage = _loc.GetString("replay-download-status-success");
            _onCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _loc.GetString("replay-download-status-canceled");
            DownloadProgress = 0;
            DownloadProgressText = "";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download replay from {Url}", targetUrl);
            StatusMessage = _loc.GetString("replay-download-status-failed", ("error", ex.Message));
            IsError = true;
            DownloadProgress = 0;
            DownloadProgressText = "";
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
