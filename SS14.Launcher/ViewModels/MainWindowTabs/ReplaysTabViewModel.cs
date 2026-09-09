using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public sealed class ReplayItemViewModel : ViewModelBase
{
    private readonly ReplaysTabViewModel _parent;
    private readonly ReplayMetadataCache _cache = ReplayMetadataCache.Instance;

    public string FilePath { get; }
    public string FileName { get; }
    public string Title { get; }
    public long FileSize { get; }
    public DateTime DateModified { get; }

    private string _mapName = "";
    public string MapName
    {
        get => _mapName;
        private set => SetProperty(ref _mapName, value);
    }

    private string _serverName = "";
    public string ServerName
    {
        get => _serverName;
        private set => SetProperty(ref _serverName, value);
    }

    private string _durationInfo = "";
    public string DurationInfo
    {
        get => _durationInfo;
        private set => SetProperty(ref _durationInfo, value);
    }

    private string _gamemode = "";
    public string Gamemode
    {
        get => _gamemode;
        private set => SetProperty(ref _gamemode, value);
    }

    private string _roundId = "";
    public string RoundId
    {
        get => _roundId;
        private set => SetProperty(ref _roundId, value);
    }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    private string? _note;
    public string? Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
            {
                OnPropertyChanged(nameof(HasNote));
            }
        }
    }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    private bool _isValidArchive = true;
    public bool IsValidArchive
    {
        get => _isValidArchive;
        private set => SetProperty(ref _isValidArchive, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _parent.NotifySelectionChanged();
            }
        }
    }

    private string _shareFeedback = "";
    public string ShareFeedback
    {
        get => _shareFeedback;
        set => SetProperty(ref _shareFeedback, value);
    }

    public long UncompressedBytes { get; private set; }
    public int EntriesCount { get; private set; }

    public string CompressionRatioFormatted
    {
        get
        {
            if (UncompressedBytes > 0 && FileSize > 0)
            {
                var saved = (1.0 - (double)FileSize / UncompressedBytes) * 100.0;
                return $"{saved:F0}%";
            }
            return "";
        }
    }

    public string FileSizeFormatted => StorageAnalyzer.FormatBytes(FileSize);
    public string DateFormatted => DateModified.ToString("dd.MM.yyyy HH:mm");

    public ReplayItemViewModel(string filePath, ReplaysTabViewModel parent)
    {
        _parent = parent;
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Title = Path.GetFileNameWithoutExtension(filePath);

        var fi = new FileInfo(filePath);
        FileSize = fi.Exists ? fi.Length : 0;
        DateModified = fi.Exists ? fi.LastWriteTime : DateTime.MinValue;

        if (_cache.TryGet(filePath, out var cached) && cached != null)
        {
            ApplyMetadata(cached);
        }

        _ = LoadMetadataAsync();
    }

    public void ApplyMetadata(ReplayMetadataEntry entry)
    {
        MapName = entry.MapName;
        ServerName = entry.ServerName;
        DurationInfo = entry.Duration;
        Gamemode = entry.Gamemode;
        RoundId = entry.RoundId;
        IsFavorite = entry.IsFavorite;
        Note = entry.Note;
        IsValidArchive = entry.IsValid;
        UncompressedBytes = entry.UncompressedBytes;
        EntriesCount = entry.EntriesCount;
        OnPropertyChanged(nameof(CompressionRatioFormatted));
        OnPropertyChanged(nameof(HasNote));
    }

    private async Task LoadMetadataAsync()
    {
        try
        {
            var entry = await _cache.GetOrUpdateAsync(FilePath);
            Dispatcher.UIThread.Post(() =>
            {
                ApplyMetadata(entry);
                _parent.NotifyItemUpdated(this);
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load cached metadata for replay {Path}", FilePath);
        }
    }

    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        _cache.SetFavorite(FilePath, IsFavorite);
        _parent.NotifyItemFavoriteChanged(this);
    }

    public async Task CopyShareInfo()
    {
        var summary = $"**Space Station 14 Replay**\n" +
                      $"🪐 **Server:** {(string.IsNullOrWhiteSpace(ServerName) ? "Unknown" : ServerName)}\n" +
                      $"🗺️ **Map:** {(string.IsNullOrWhiteSpace(MapName) ? "Unknown" : MapName)}\n" +
                      $"🎮 **Mode:** {(string.IsNullOrWhiteSpace(Gamemode) ? "Standard" : Gamemode)}" +
                      (string.IsNullOrWhiteSpace(RoundId) ? "" : $" (Round #{RoundId})") + "\n" +
                      $"⏱️ **Duration:** {(string.IsNullOrWhiteSpace(DurationInfo) ? "Unknown" : DurationInfo)}\n" +
                      $"📅 **Date:** {DateFormatted}\n" +
                      $"💾 **Size:** {FileSizeFormatted}" +
                      (string.IsNullOrWhiteSpace(Note) ? "" : $"\n📝 **Note:** {Note}");

        _ = ClipboardHelper.CopyWithFeedbackAsync(summary, s => ShareFeedback = s,
            LocalizationManager.Instance.GetString("tab-replays-copied-summary"));
        await Task.CompletedTask;
    }

    public void OpenContainingFolder()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null && Directory.Exists(dir))
            {
                Helpers.OpenUri(dir);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open replay folder");
        }
    }

    public async Task ExportReplayAsync()
    {
        if (_parent.Control?.GetVisualRoot() is not Window window)
            return;

        try
        {
            var result = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = LocalizationManager.Instance.GetString("tab-replays-export-picker-title"),
                SuggestedFileName = FileName,
                DefaultExtension = "zip",
                FileTypeChoices =
                [
                    new FilePickerFileType("SS14 Replay (*.zip)")
                    {
                        Patterns = ["*.zip"],
                        MimeTypes = ["application/zip"]
                    }
                ]
            });

            if (result != null)
            {
                await using var destStream = await result.OpenWriteAsync();
                await using var srcStream = File.OpenRead(FilePath);
                await srcStream.CopyToAsync(destStream);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export replay {Path}", FilePath);
        }
    }

    public void Play()
    {
        _ = _parent.LaunchReplay(FilePath);
    }

    public void ShowDetails()
    {
        var window = _parent.Control?.GetVisualRoot() as Window;
        var dialog = new Views.ReplayDetailsDialog(FilePath, () => Play());
        dialog.Closed += (_, _) =>
        {
            // Sync changes back from details dialog
            if (_cache.TryGet(FilePath, out var updated) && updated != null)
            {
                ApplyMetadata(updated);
                _parent.NotifyItemUpdated(this);
            }
        };

        if (window != null)
            dialog.ShowDialog(window);
        else
            dialog.Show();
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            _cache.Remove(FilePath);
            _ = _parent.RefreshReplays();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to delete replay {Path}", FilePath);
        }
    }
}

public sealed class ReplaysTabViewModel : MainWindowTabViewModel
{
    private readonly MainWindowViewModel _windowVm;
    private readonly DataManager _cfg = Locator.Current.GetRequiredService<DataManager>();
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly ReplayMetadataCache _cache = ReplayMetadataCache.Instance;

    public ObservableCollection<ReplayItemViewModel> AllReplays { get; } = new();
    public ObservableCollection<ReplayItemViewModel> FilteredReplays { get; } = new();

    public UserControl? Control { get; set; }

    private string _searchString = "";
    public string SearchString
    {
        get => _searchString;
        set
        {
            if (SetProperty(ref _searchString, value))
            {
                ApplyFilter();
            }
        }
    }

    private bool _onlyFavorites;
    public bool OnlyFavorites
    {
        get => _onlyFavorites;
        set
        {
            if (SetProperty(ref _onlyFavorites, value))
            {
                ApplyFilter();
            }
        }
    }

    private int _selectedSortIndex = 0;
    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                ApplyFilter();
            }
        }
    }

    public string[] SortOptions => [
        _loc.GetString("tab-replays-sort-date-desc"),
        _loc.GetString("tab-replays-sort-date-asc"),
        _loc.GetString("tab-replays-sort-size-desc"),
        _loc.GetString("tab-replays-sort-name-asc"),
        _loc.GetString("tab-replays-sort-duration-desc"),
        _loc.GetString("tab-replays-sort-duration-asc")
    ];

    public ObservableCollection<string> AvailableGamemodes { get; } = new();

    private string _selectedGamemode = "";
    public string SelectedGamemode
    {
        get => _selectedGamemode;
        set
        {
            if (SetProperty(ref _selectedGamemode, value))
            {
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<string> AvailableServers { get; } = new();

    private string _selectedServer = "";
    public string SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (SetProperty(ref _selectedServer, value))
            {
                ApplyFilter();
            }
        }
    }

    // DASHBOARD STATS
    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    private string _totalSizeFormatted = "0 B";
    public string TotalSizeFormatted
    {
        get => _totalSizeFormatted;
        private set => SetProperty(ref _totalSizeFormatted, value);
    }

    private string _totalDurationFormatted = "-";
    public string TotalDurationFormatted
    {
        get => _totalDurationFormatted;
        private set => SetProperty(ref _totalDurationFormatted, value);
    }

    private int _favoritesCount;
    public int FavoritesCount
    {
        get => _favoritesCount;
        private set => SetProperty(ref _favoritesCount, value);
    }

    // BATCH SELECTION MODE
    private bool _isBatchMode;
    public bool IsBatchMode
    {
        get => _isBatchMode;
        set
        {
            if (SetProperty(ref _isBatchMode, value))
            {
                if (!value)
                {
                    DeselectAll();
                }
            }
        }
    }

    private int _selectedCount;
    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                OnPropertyChanged(nameof(HasSelectedItems));
                OnPropertyChanged(nameof(DeleteSelectedButtonText));
            }
        }
    }

    public bool HasSelectedItems => SelectedCount > 0;

    public string DeleteSelectedButtonText =>
        _loc.GetString("tab-replays-batch-delete-selected", ("count", SelectedCount));

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ReplaysDirectory => LauncherPaths.DirReplays;

    public override string Name
    {
        get
        {
            var custom = _cfg.GetCVar(CVars.CustomReplaysTabName);
            if (!string.IsNullOrWhiteSpace(custom))
                return custom;
            return _loc.GetString("tab-replays-title");
        }
    }

    private FileSystemWatcher? _replaysWatcher;
    private DispatcherTimer? _watcherDebounce;

    public ReplaysTabViewModel(MainWindowViewModel windowVm)
    {
        _windowVm = windowVm;
        _loc.LanguageSwitched += () =>
        {
            OnPropertyChanged(nameof(SortOptions));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(DeleteSelectedButtonText));
            UpdateFilterDropdowns();
        };

        SetupDirectoryWatcher();
        _ = RefreshReplays();
    }

    private void SetupDirectoryWatcher()
    {
        try
        {
            if (!Directory.Exists(ReplaysDirectory))
            {
                Directory.CreateDirectory(ReplaysDirectory);
            }

            _watcherDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _watcherDebounce.Tick += (_, _) =>
            {
                _watcherDebounce.Stop();
                _ = RefreshReplays();
            };

            _replaysWatcher = new FileSystemWatcher(ReplaysDirectory, "*.zip")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _replaysWatcher.Created += (_, _) => TriggerWatcherDebounce();
            _replaysWatcher.Deleted += (_, _) => TriggerWatcherDebounce();
            _replaysWatcher.Renamed += (_, _) => TriggerWatcherDebounce();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not initialize replays FileSystemWatcher");
        }
    }

    public override void Selected()
    {
        base.Selected();
        if (_replaysWatcher != null)
        {
            _replaysWatcher.EnableRaisingEvents = true;
        }
        _ = RefreshReplays();
    }

    public override void Unselected()
    {
        base.Unselected();
        if (_replaysWatcher != null)
        {
            _replaysWatcher.EnableRaisingEvents = false;
        }
        _watcherDebounce?.Stop();
    }

    private void TriggerWatcherDebounce()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _watcherDebounce?.Stop();
            _watcherDebounce?.Start();
        });
    }

    public async Task RefreshReplays()
    {
        try
        {
            if (!Directory.Exists(ReplaysDirectory))
            {
                Directory.CreateDirectory(ReplaysDirectory);
            }

            var files = await Task.Run(() =>
            {
                try
                {
                    return Directory.GetFiles(ReplaysDirectory, "*.zip", SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error scanning replays directory");
                    return Array.Empty<string>();
                }
            });

            _cache.PruneMissing(files);

            var existingMap = AllReplays.ToDictionary(r => r.FilePath, StringComparer.OrdinalIgnoreCase);
            var updatedList = new List<ReplayItemViewModel>();

            foreach (var file in files)
            {
                if (existingMap.TryGetValue(file, out var existing))
                {
                    updatedList.Add(existing);
                }
                else
                {
                    updatedList.Add(new ReplayItemViewModel(file, this));
                }
            }

            AllReplays.Clear();
            foreach (var replay in updatedList)
            {
                AllReplays.Add(replay);
            }

            UpdateStats();
            UpdateFilterDropdowns();
            ApplyFilter();
            StatusMessage = "";
        }
        catch (Exception e)
        {
            Log.Error(e, "Error loading replays");
            StatusMessage = $"{e.Message}";
        }
    }

    public void NotifyItemUpdated(ReplayItemViewModel item)
    {
        UpdateStats();
        UpdateFilterDropdowns();
        ApplyFilter();
    }

    public void NotifyItemFavoriteChanged(ReplayItemViewModel item)
    {
        UpdateStats();
        ApplyFilter();
    }

    public void NotifySelectionChanged()
    {
        SelectedCount = AllReplays.Count(r => r.IsSelected);
    }

    private void UpdateStats()
    {
        TotalCount = AllReplays.Count;
        FavoritesCount = AllReplays.Count(r => r.IsFavorite);

        long totalBytes = 0;
        var totalDuration = TimeSpan.Zero;
        var hasValidDuration = false;

        foreach (var r in AllReplays)
        {
            totalBytes += r.FileSize;
            var parsed = ReplayMetadataCache.ParseDuration(r.DurationInfo);
            if (parsed.HasValue)
            {
                totalDuration += parsed.Value;
                hasValidDuration = true;
            }
        }

        TotalSizeFormatted = StorageAnalyzer.FormatBytes(totalBytes);
        TotalDurationFormatted = hasValidDuration ? ReplayMetadataCache.FormatDuration(totalDuration) : "-";
    }

    private void UpdateFilterDropdowns()
    {
        var allModesStr = _loc.GetString("tab-replays-filter-all-gamemodes");
        var allServersStr = _loc.GetString("tab-replays-filter-all-servers");

        var modes = AllReplays
            .Select(r => r.Gamemode)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m)
            .ToList();

        var servers = AllReplays
            .Select(r => r.ServerName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();

        var currentMode = SelectedGamemode;
        AvailableGamemodes.Clear();
        AvailableGamemodes.Add(allModesStr);
        foreach (var m in modes)
        {
            AvailableGamemodes.Add(m);
        }
        SelectedGamemode = AvailableGamemodes.Contains(currentMode) ? currentMode : allModesStr;

        var currentServer = SelectedServer;
        AvailableServers.Clear();
        AvailableServers.Add(allServersStr);
        foreach (var s in servers)
        {
            AvailableServers.Add(s);
        }
        SelectedServer = AvailableServers.Contains(currentServer) ? currentServer : allServersStr;
    }

    private void ApplyFilter()
    {
        FilteredReplays.Clear();

        var query = SearchString.Trim();
        var allModesStr = _loc.GetString("tab-replays-filter-all-gamemodes");
        var allServersStr = _loc.GetString("tab-replays-filter-all-servers");

        var matches = AllReplays.AsEnumerable();

        if (OnlyFavorites)
        {
            matches = matches.Where(r => r.IsFavorite);
        }

        if (!string.IsNullOrWhiteSpace(SelectedGamemode) && !SelectedGamemode.Equals(allModesStr, StringComparison.OrdinalIgnoreCase))
        {
            matches = matches.Where(r => r.Gamemode.Equals(SelectedGamemode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedServer) && !SelectedServer.Equals(allServersStr, StringComparison.OrdinalIgnoreCase))
        {
            matches = matches.Where(r => r.ServerName.Equals(SelectedServer, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            matches = matches.Where(r => r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         r.DateFormatted.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         r.MapName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         r.ServerName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         r.Gamemode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         r.RoundId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                         (r.Note != null && r.Note.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        // Always pin favorites to top, then apply selected sort order
        matches = SelectedSortIndex switch
        {
            1 => matches.OrderByDescending(r => r.IsFavorite).ThenBy(r => r.DateModified),
            2 => matches.OrderByDescending(r => r.IsFavorite).ThenByDescending(r => r.FileSize),
            3 => matches.OrderByDescending(r => r.IsFavorite).ThenBy(r => r.Title),
            4 => matches.OrderByDescending(r => r.IsFavorite).ThenByDescending(r => ReplayMetadataCache.ParseDuration(r.DurationInfo) ?? TimeSpan.Zero),
            5 => matches.OrderByDescending(r => r.IsFavorite).ThenBy(r => ReplayMetadataCache.ParseDuration(r.DurationInfo) ?? TimeSpan.MaxValue),
            _ => matches.OrderByDescending(r => r.IsFavorite).ThenByDescending(r => r.DateModified),
        };

        foreach (var item in matches)
        {
            FilteredReplays.Add(item);
        }
    }

    public void ToggleBatchMode()
    {
        IsBatchMode = !IsBatchMode;
    }

    public void SelectAll()
    {
        foreach (var r in FilteredReplays)
        {
            r.IsSelected = true;
        }
    }

    public void DeselectAll()
    {
        foreach (var r in AllReplays)
        {
            r.IsSelected = false;
        }
    }

    public void DeleteSelected()
    {
        var selected = AllReplays.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
            return;

        foreach (var r in selected)
        {
            try
            {
                if (File.Exists(r.FilePath))
                {
                    File.Delete(r.FilePath);
                }
                _cache.Remove(r.FilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete selected replay {Path}", r.FilePath);
            }
        }

        IsBatchMode = false;
        _ = RefreshReplays();
    }

    public void OpenSmartCleaner()
    {
        var window = Control?.GetVisualRoot() as Window;
        var dialog = new Views.SmartReplayCleanerDialog(_cache.GetAllCached().Values, () =>
        {
            Dispatcher.UIThread.Post(() => _ = RefreshReplays());
        });

        if (window != null)
            dialog.ShowDialog(window);
        else
            dialog.Show();
    }

    public async Task OpenFilePickerAndPlay()
    {
        if (Control?.GetVisualRoot() is not Window window)
            return;

        var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = LocalizationManager.Instance.GetString("replays-picker-title"),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("SS14 Replay Files (*.zip)")
                {
                    Patterns = ["*.zip"],
                    MimeTypes = ["application/zip"],
                    AppleUniformTypeIdentifiers = ["zip"]
                }
            ]
        });

        if (result.Count == 0)
            return;

        using var file = result[0];
        if (!_windowVm.IsContentBundleDropValid(file))
            return;

        ConnectingViewModel.StartContentBundle(_windowVm, file);
    }

    public async Task LaunchReplay(string filePath)
    {
        if (Control?.GetVisualRoot() is not Window window)
            return;

        try
        {
            var storageFile = await window.StorageProvider.TryGetFileFromPathAsync(filePath);
            if (storageFile != null && _windowVm.IsContentBundleDropValid(storageFile))
            {
                ConnectingViewModel.StartContentBundle(_windowVm, storageFile);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to launch replay {Path}", filePath);
        }
    }

    public void OpenReplaysFolder()
    {
        try
        {
            Directory.CreateDirectory(ReplaysDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = ReplaysDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open replays folder");
        }
    }
}
