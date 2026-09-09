using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Serilog;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class ReplayDetailsViewModel : ViewModelBase
{
    private readonly Action? _onPlay;
    private readonly ReplayMetadataCache _cache = ReplayMetadataCache.Instance;

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

    private string _compressionRatioFormatted = "";
    public string CompressionRatioFormatted
    {
        get => _compressionRatioFormatted;
        private set => SetProperty(ref _compressionRatioFormatted, value);
    }

    private string _sha256 = "";
    public string Sha256
    {
        get => _sha256;
        private set => SetProperty(ref _sha256, value);
    }

    private bool _isSha256Loading = true;
    public bool IsSha256Loading
    {
        get => _isSha256Loading;
        private set => SetProperty(ref _isSha256Loading, value);
    }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
            {
                _cache.SetFavorite(FilePath, value);
            }
        }
    }

    private string _note = "";
    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    private string _noteFeedback = "";
    public string NoteFeedback
    {
        get => _noteFeedback;
        set => SetProperty(ref _noteFeedback, value);
    }

    private string _copyFeedback = "";
    public string CopyFeedback
    {
        get => _copyFeedback;
        set => SetProperty(ref _copyFeedback, value);
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

        if (_cache.TryGet(filePath, out var cached) && cached != null)
        {
            MapName = cached.MapName;
            ServerName = cached.ServerName;
            Gamemode = cached.Gamemode;
            RoundId = cached.RoundId;
            Duration = cached.Duration;
            IsFavorite = cached.IsFavorite;
            Note = cached.Note ?? "";
            if (!string.IsNullOrEmpty(cached.Sha256))
            {
                Sha256 = cached.Sha256;
                IsSha256Loading = false;
            }
            if (cached.UncompressedBytes > 0)
            {
                UncompressedSizeFormatted = StorageAnalyzer.FormatBytes(cached.UncompressedBytes);
                var ratio = (1.0 - (double)cached.FileSize / cached.UncompressedBytes) * 100.0;
                CompressionRatioFormatted = $"{ratio:F1}%";
            }
        }

        _ = LoadDetailsAsync();
        _ = LoadSha256Async();
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
                    string? line;
                    while ((line = reader.ReadLine()) != null)
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

                var fi = new FileInfo(FilePath);
                var compressedBytes = fi.Exists ? fi.Length : 0;
                var ratioStr = uncompressedBytes > 0 && compressedBytes > 0
                    ? $"{((1.0 - (double)compressedBytes / uncompressedBytes) * 100.0):F1}%"
                    : "";

                Dispatcher.UIThread.Post(() =>
                {
                    if (map != null) MapName = map;
                    if (server != null) ServerName = server;
                    if (duration != null) Duration = duration;
                    if (gamemode != null) Gamemode = gamemode;
                    if (roundId != null) RoundId = roundId;

                    EntriesCount = archive.Entries.Count;
                    UncompressedSizeFormatted = StorageAnalyzer.FormatBytes(uncompressedBytes);
                    if (!string.IsNullOrEmpty(ratioStr))
                    {
                        CompressionRatioFormatted = ratioStr;
                    }

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

    private async Task LoadSha256Async()
    {
        try
        {
            var hash = await _cache.ComputeOrGetSha256Async(FilePath);
            Dispatcher.UIThread.Post(() =>
            {
                Sha256 = hash;
                IsSha256Loading = false;
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to compute SHA256 for replay {Path}", FilePath);
            Dispatcher.UIThread.Post(() =>
            {
                IsSha256Loading = false;
            });
        }
    }

    public void SaveNote()
    {
        _cache.SetNote(FilePath, Note);
        _ = ClipboardHelper.CopyWithFeedbackAsync(Note, s => NoteFeedback = s,
            LocalizationManager.Instance.GetString("account-info-copied"), 2);
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
        _ = ClipboardHelper.CopyWithFeedbackAsync(FilePath, s => CopyFeedback = s);
        await Task.CompletedTask;
    }

    public async Task CopySha256ToClipboard()
    {
        if (!string.IsNullOrEmpty(Sha256))
        {
            _ = ClipboardHelper.CopyWithFeedbackAsync(Sha256, s => CopyFeedback = s,
                LocalizationManager.Instance.GetString("replay-dialog-hash-copied"));
        }
        await Task.CompletedTask;
    }

    public async Task CopyDiscordSummary()
    {
        var summary = $"**Space Station 14 Replay**\n" +
                      $"🪐 **Server:** {(string.IsNullOrWhiteSpace(ServerName) ? "Unknown" : ServerName)}\n" +
                      $"🗺️ **Map:** {(string.IsNullOrWhiteSpace(MapName) ? "Unknown" : MapName)}\n" +
                      $"🎮 **Mode:** {(string.IsNullOrWhiteSpace(Gamemode) ? "Standard" : Gamemode)}" +
                      (string.IsNullOrWhiteSpace(RoundId) ? "" : $" (Round #{RoundId})") + "\n" +
                      $"⏱️ **Duration:** {(string.IsNullOrWhiteSpace(Duration) ? "Unknown" : Duration)}\n" +
                      $"📅 **Date:** {DateFormatted}\n" +
                      $"💾 **Size:** {FileSizeFormatted}" +
                      (string.IsNullOrWhiteSpace(Note) ? "" : $"\n📝 **Note:** {Note}");

        _ = ClipboardHelper.CopyWithFeedbackAsync(summary, s => CopyFeedback = s,
            LocalizationManager.Instance.GetString("tab-replays-copied-summary"));
        await Task.CompletedTask;
    }

    public async Task ExportReplayAsync(Window window)
    {
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
}
