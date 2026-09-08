using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class ReplayDetailsViewModel : ViewModelBase
{
    private readonly Action? _onPlay;

    public string FilePath { get; }
    public string FileName { get; }
    public string Title { get; }

    private string _serverName = "";
    public string ServerName
    {
        get => _serverName;
        private set => SetProperty(ref _serverName, value);
    }

    private string _mapName = "";
    public string MapName
    {
        get => _mapName;
        private set => SetProperty(ref _mapName, value);
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

    private string _duration = "";
    public string Duration
    {
        get => _duration;
        private set => SetProperty(ref _duration, value);
    }

    private string _dateFormatted = "";
    public string DateFormatted
    {
        get => _dateFormatted;
        private set => SetProperty(ref _dateFormatted, value);
    }

    private string _fileSizeFormatted = "";
    public string FileSizeFormatted
    {
        get => _fileSizeFormatted;
        private set => SetProperty(ref _fileSizeFormatted, value);
    }

    private string _uncompressedSizeFormatted = "";
    public string UncompressedSizeFormatted
    {
        get => _uncompressedSizeFormatted;
        private set => SetProperty(ref _uncompressedSizeFormatted, value);
    }

    private int _entriesCount;
    public int EntriesCount
    {
        get => _entriesCount;
        private set => SetProperty(ref _entriesCount, value);
    }

    public ObservableCollection<string> FileEntries { get; } = new();

    public ReplayDetailsViewModel() : this("")
    {
    }

    public ReplayDetailsViewModel(string filePath, Action? onPlay = null)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Title = Path.GetFileNameWithoutExtension(filePath);
        _onPlay = onPlay;

        var fi = new FileInfo(filePath);
        if (fi.Exists)
        {
            DateFormatted = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            FileSizeFormatted = StorageAnalyzer.FormatBytes(fi.Length);
        }

        _ = LoadDetailsAsync();
    }

    private async Task LoadDetailsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read, true);

                long uncompressedBytes = 0;
                var entryNames = new System.Collections.Generic.List<string>();

                foreach (var entry in archive.Entries)
                {
                    uncompressedBytes += entry.Length;
                    entryNames.Add($"{entry.FullName} ({StorageAnalyzer.FormatBytes(entry.Length)})");
                }

                string? map = null, server = null, duration = null, gamemode = null, roundId = null;

                var metaEntry = archive.GetEntry("replay_final.txt")
                             ?? archive.GetEntry("replay_final.yml")
                             ?? archive.GetEntry("manifest.yml");

                if (metaEntry != null)
                {
                    using var stream = metaEntry.Open();
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();

                    var lines = content.Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("map:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("map_name:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) map = parts[1].Trim(' ', '"', '\'');
                        }
                        else if (trimmed.StartsWith("server:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("server_name:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) server = parts[1].Trim(' ', '"', '\'');
                        }
                        else if (trimmed.StartsWith("duration:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("ticks:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) duration = parts[1].Trim(' ', '"', '\'');
                        }
                        else if (trimmed.StartsWith("gamemode:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("mode:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) gamemode = parts[1].Trim(' ', '"', '\'');
                        }
                        else if (trimmed.StartsWith("round_id:", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("round:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = trimmed.Split(':', 2);
                            if (parts.Length > 1) roundId = parts[1].Trim(' ', '"', '\'');
                        }
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (map != null) MapName = map;
                    if (server != null) ServerName = server;
                    if (duration != null) Duration = duration;
                    if (gamemode != null) Gamemode = gamemode;
                    if (roundId != null) RoundId = roundId;

                    EntriesCount = archive.Entries.Count;
                    UncompressedSizeFormatted = StorageAnalyzer.FormatBytes(uncompressedBytes);

                    FileEntries.Clear();
                    foreach (var name in entryNames)
                    {
                        FileEntries.Add(name);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load detailed metadata for replay {Path}", FilePath);
            }
        });
    }

    public void Play()
    {
        _onPlay?.Invoke();
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

    public async Task CopyPathToClipboard()
    {
        await ClipboardHelper.SetTextAsync(FilePath);
    }
}
