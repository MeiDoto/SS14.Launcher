using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public class LogViewerViewModel : ViewModelBase
{
    private string _selectedLogFile = "";
    private string _searchFilter = "";
    private ObservableCollection<string> _availableFiles = new();
    private ObservableCollection<LogLineItem> _filteredLines = new();
    private string[] _allLines = Array.Empty<string>();
    private string _statusText = "";

    public ObservableCollection<string> AvailableFiles
    {
        get => _availableFiles;
        set => SetProperty(ref _availableFiles, value);
    }

    public string SelectedLogFile
    {
        get => _selectedLogFile;
        set
        {
            if (SetProperty(ref _selectedLogFile, value))
            {
                LoadSelectedLog();
            }
        }
    }

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<LogLineItem> FilteredLines
    {
        get => _filteredLines;
        set => SetProperty(ref _filteredLines, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public LogViewerViewModel()
    {
        RefreshFiles();
    }

    public void RefreshFiles()
    {
        try
        {
            var logDir = LauncherPaths.DirLogs;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var files = Directory.GetFiles(logDir, "*.log")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .OrderByDescending(f => File.GetLastWriteTime(Path.Combine(logDir, f!)))
                .ToList();

            AvailableFiles = new ObservableCollection<string>(files!);
            if (files.Count > 0)
            {
                SelectedLogFile = files[0]!;
            }
            else
            {
                StatusText = LocalizationManager.Instance.GetString("log-viewer-no-files");
            }
        }
        catch (Exception ex)
        {
            StatusText = LocalizationManager.Instance.GetString("log-viewer-error-read-dir", ("error", ex.Message));
        }
    }

    private void LoadSelectedLog()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFile))
            return;

        try
        {
            var path = Path.Combine(LauncherPaths.DirLogs, SelectedLogFile);
            if (File.Exists(path))
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var content = sr.ReadToEnd();
                _allLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                ApplyFilter();
                StatusText = LocalizationManager.Instance.GetString("log-viewer-loaded", ("lines", _allLines.Length.ToString()), ("file", SelectedLogFile));
            }
        }
        catch (Exception ex)
        {
            StatusText = LocalizationManager.Instance.GetString("log-viewer-error-read-file", ("file", SelectedLogFile), ("error", ex.Message));
        }
    }

    private void ApplyFilter()
    {
        var filter = SearchFilter?.Trim();
        var items = new ObservableCollection<LogLineItem>();

        foreach (var line in _allLines)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            if (!string.IsNullOrEmpty(filter) && !line.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;

            var brush = GetLineColor(line);
            items.Add(new LogLineItem(line, brush));
        }

        FilteredLines = items;
    }

    private static IBrush GetLineColor(string line)
    {
        var lower = line.ToLowerInvariant();
        if (lower.Contains("[err]") || lower.Contains("[fatal]") || lower.Contains("exception:") || lower.Contains("error:") || lower.Contains("crash"))
        {
            return SolidColorBrush.Parse("#FF5555");
        }
        if (lower.Contains("[wrn]") || lower.Contains("[warn]") || lower.Contains("warning:"))
        {
            return SolidColorBrush.Parse("#FFB86C");
        }
        if (lower.Contains("[dbg]") || lower.Contains("[debug]") || lower.Contains("[vrb]"))
        {
            return SolidColorBrush.Parse("#6272A4");
        }
        return SolidColorBrush.Parse("#F8F8F2");
    }

    public async void CopyLogToClipboard()
    {
        try
        {
            var text = string.Join(Environment.NewLine, FilteredLines.Select(l => l.Text));
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var top = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                if (top?.Clipboard != null)
                {
                    await top.Clipboard.SetTextAsync(text);
                    StatusText = LocalizationManager.Instance.GetString("log-viewer-copied");
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = LocalizationManager.Instance.GetString("log-viewer-error-copy", ("error", ex.Message));
        }
    }

    public void OpenLogsDirectory()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = LauncherPaths.DirLogs
            });
        }
        catch (Exception ex)
        {
            StatusText = LocalizationManager.Instance.GetString("log-viewer-error-open-dir", ("error", ex.Message));
        }
    }
}

public record LogLineItem(string Text, IBrush Color);
