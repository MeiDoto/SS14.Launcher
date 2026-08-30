using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public sealed class ReplayItemViewModel : ViewModelBase
{
    private readonly ReplaysTabViewModel _parent;

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

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize >= 1024 * 1024 * 1024)
                return $"{(FileSize / (1024.0 * 1024.0 * 1024.0)):F2} GB";
            if (FileSize >= 1024 * 1024)
                return $"{(FileSize / (1024.0 * 1024.0)):F1} MB";
            if (FileSize >= 1024)
                return $"{(FileSize / 1024.0):F0} KB";
            return $"{FileSize} B";
        }
    }

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

        // Load metadata
        _ = LoadMetadataAsync();
    }

    private async Task LoadMetadataAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read, true);

                var entry = archive.GetEntry("replay_final.txt")
                         ?? archive.GetEntry("replay_final.yml")
                         ?? archive.GetEntry("manifest.yml");

                if (entry != null)
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();

                    string? mapName = null, serverName = null, durationInfo = null;

                    var lines = content.Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("map:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("map_name:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) mapName = parts[1].Trim(' ', '"', '\'');
                        }
                        else if (trimmed.StartsWith("server:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("server_name:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) serverName = parts[1].Trim(' ', '"', '\'');
                        }
                        else if (trimmed.StartsWith("duration:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("ticks:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) durationInfo = parts[1].Trim(' ', '"', '\'');
                        }
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        if (mapName != null) MapName = mapName;
                        if (serverName != null) ServerName = serverName;
                        if (durationInfo != null) DurationInfo = durationInfo;
                    });
                }
            }
            catch
            {
            }
        });
    }

    public void Play()
    {
        _parent.LaunchReplay(FilePath);
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            _parent.RefreshReplays();
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
        _loc.GetString("tab-replays-sort-name-asc")
    ];

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ReplaysDirectory => Path.Combine(LauncherPaths.DirUserData, "replays");

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
        };

        SetupDirectoryWatcher();
        RefreshReplays();
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
                RefreshReplays();
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
        RefreshReplays();
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

    public async void RefreshReplays()
    {
        try
        {
            if (!Directory.Exists(ReplaysDirectory))
            {
                Directory.CreateDirectory(ReplaysDirectory);
            }

            var replayFiles = await Task.Run(() =>
            {
                try
                {
                    return Directory.GetFiles(ReplaysDirectory, "*.zip", SearchOption.AllDirectories)
                        .Select(f => new ReplayItemViewModel(f, this))
                        .ToList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error reading replay files");
                    return new List<ReplayItemViewModel>();
                }
            });

            AllReplays.Clear();
            foreach (var replay in replayFiles)
            {
                AllReplays.Add(replay);
            }

            ApplyFilter();
            StatusMessage = "";
        }
        catch (Exception e)
        {
            Log.Error(e, "Error loading replays");
            StatusMessage = $"{e.Message}";
        }
    }

    private void ApplyFilter()
    {
        FilteredReplays.Clear();

        var query = SearchString.Trim();
        var matches = string.IsNullOrWhiteSpace(query)
            ? AllReplays.AsEnumerable()
            : AllReplays.Where(r => r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   r.DateFormatted.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   r.MapName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   r.ServerName.Contains(query, StringComparison.OrdinalIgnoreCase));

        matches = SelectedSortIndex switch
        {
            1 => matches.OrderBy(r => r.DateModified),
            2 => matches.OrderByDescending(r => r.FileSize),
            3 => matches.OrderBy(r => r.Title),
            _ => matches.OrderByDescending(r => r.DateModified),
        };

        foreach (var item in matches)
        {
            FilteredReplays.Add(item);
        }
    }

    public async void OpenFilePickerAndPlay()
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

    public async void LaunchReplay(string filePath)
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
