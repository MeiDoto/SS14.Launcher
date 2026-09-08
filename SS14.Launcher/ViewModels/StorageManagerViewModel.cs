using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;

namespace SS14.Launcher.ViewModels;

public sealed class StorageManagerViewModel : ViewModelBase
{
    private readonly ContentManager _contentManager;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            SetProperty(ref _isLoading, value);
            OnPropertyChanged(nameof(CanOperate));
        }
    }

    public bool CanOperate => !IsLoading;

    private string _totalUsageFormatted = "";
    public string TotalUsageFormatted
    {
        get => _totalUsageFormatted;
        private set => SetProperty(ref _totalUsageFormatted, value);
    }

    private string _contentDbFormatted = "";
    public string ContentDbFormatted
    {
        get => _contentDbFormatted;
        private set => SetProperty(ref _contentDbFormatted, value);
    }

    private string _enginesFormatted = "";
    public string EnginesFormatted
    {
        get => _enginesFormatted;
        private set => SetProperty(ref _enginesFormatted, value);
    }

    private string _logsFormatted = "";
    public string LogsFormatted
    {
        get => _logsFormatted;
        private set => SetProperty(ref _logsFormatted, value);
    }

    private string _replaysFormatted = "";
    public string ReplaysFormatted
    {
        get => _replaysFormatted;
        private set => SetProperty(ref _replaysFormatted, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<EngineStorageItem> Engines { get; } = new();

    private EngineStorageItem? _selectedEngine;
    public EngineStorageItem? SelectedEngine
    {
        get => _selectedEngine;
        set
        {
            SetProperty(ref _selectedEngine, value);
            OnPropertyChanged(nameof(HasSelectedEngine));
        }
    }

    public bool HasSelectedEngine => SelectedEngine != null;

    public StorageManagerViewModel()
    {
        _contentManager = Locator.Current.GetService<ContentManager>() ?? new ContentManager();
        _ = RefreshStorageAsync();
    }

    public async Task RefreshStorageAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        StatusMessage = LocalizationManager.Instance.GetString("storage-status-analyzing");

        try
        {
            var breakdown = await StorageAnalyzer.AnalyzeStorageAsync();

            TotalUsageFormatted = breakdown.FormattedTotal;
            ContentDbFormatted = breakdown.FormattedContentDb;
            EnginesFormatted = breakdown.FormattedEngines;
            LogsFormatted = breakdown.FormattedLogs;
            ReplaysFormatted = breakdown.FormattedReplays;

            Engines.Clear();
            foreach (var engine in breakdown.InstalledEngines)
            {
                Engines.Add(engine);
            }

            StatusMessage = LocalizationManager.Instance.GetString("storage-status-ready");
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationManager.Instance.GetString("storage-status-error", ("err", ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteSelectedEngineAsync()
    {
        if (SelectedEngine is not { } engine)
            return;

        IsLoading = true;
        try
        {
            var success = StorageAnalyzer.DeleteEngineVersion(engine.Version);
            StatusMessage = success
                ? LocalizationManager.Instance.GetString("storage-engine-deleted", ("ver", engine.Version))
                : LocalizationManager.Instance.GetString("storage-engine-delete-failed", ("ver", engine.Version));

            await RefreshStorageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CleanOldContentAsync(int days = 14)
    {
        IsLoading = true;
        StatusMessage = LocalizationManager.Instance.GetString("storage-status-cleaning");

        try
        {
            var culled = await _contentManager.RunSmartCleanerAsync(days);
            StatusMessage = LocalizationManager.Instance.GetString("storage-clean-completed", ("count", culled));
            await RefreshStorageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task VacuumDatabaseAsync()
    {
        IsLoading = true;
        StatusMessage = LocalizationManager.Instance.GetString("storage-status-vacuuming");

        try
        {
            var success = await StorageAnalyzer.VacuumContentDatabaseAsync();
            StatusMessage = success
                ? LocalizationManager.Instance.GetString("storage-vacuum-completed")
                : LocalizationManager.Instance.GetString("storage-vacuum-failed");
            await RefreshStorageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ClearLogsAsync()
    {
        IsLoading = true;
        try
        {
            StorageAnalyzer.ClearLogs();
            StatusMessage = LocalizationManager.Instance.GetString("storage-logs-cleared");
            await RefreshStorageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
