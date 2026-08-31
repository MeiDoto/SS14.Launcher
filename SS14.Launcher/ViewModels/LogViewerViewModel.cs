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

    public enum LogFilterMode { All, Errors, Warnings, Info, Debug }
    private LogFilterMode _currentFilterMode = LogFilterMode.All;
    public LogFilterMode CurrentFilterMode
    {
        get => _currentFilterMode;
        set
        {
            if (SetProperty(ref _currentFilterMode, value))
            {
                ApplyFilter();
                OnPropertyChanged(nameof(IsFilterAll));
                OnPropertyChanged(nameof(IsFilterErrors));
                OnPropertyChanged(nameof(IsFilterWarnings));
                OnPropertyChanged(nameof(IsFilterInfo));
                OnPropertyChanged(nameof(IsFilterDebug));
            }
        }
    }

    public bool IsFilterAll => _currentFilterMode == LogFilterMode.All;
    public bool IsFilterErrors => _currentFilterMode == LogFilterMode.Errors;
    public bool IsFilterWarnings => _currentFilterMode == LogFilterMode.Warnings;
    public bool IsFilterInfo => _currentFilterMode == LogFilterMode.Info;
    public bool IsFilterDebug => _currentFilterMode == LogFilterMode.Debug;

    public void SetFilterAll() => CurrentFilterMode = LogFilterMode.All;
    public void SetFilterErrors() => CurrentFilterMode = LogFilterMode.Errors;
    public void SetFilterWarnings() => CurrentFilterMode = LogFilterMode.Warnings;
    public void SetFilterInfo() => CurrentFilterMode = LogFilterMode.Info;
    public void SetFilterDebug() => CurrentFilterMode = LogFilterMode.Debug;

    private string _copyButtonText = "";
    public string CopyButtonText
    {
        get => string.IsNullOrEmpty(_copyButtonText) ? LocalizationManager.Instance.GetString("log-viewer-copy") : _copyButtonText;
        set => SetProperty(ref _copyButtonText, value);
    }

    private void ApplyFilter()
    {
        var filter = SearchFilter?.Trim();
        var items = new ObservableCollection<LogLineItem>();

        foreach (var line in _allLines)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            var lower = line.ToLowerInvariant();

            // Apply Level Filter
            if (_currentFilterMode == LogFilterMode.Errors &&
                !lower.Contains("[err]") && !lower.Contains("[fatal]") && !lower.Contains("exception:") && !lower.Contains("error:") && !lower.Contains("crash"))
                continue;

            if (_currentFilterMode == LogFilterMode.Warnings &&
                !lower.Contains("[wrn]") && !lower.Contains("[warn]") && !lower.Contains("warning:"))
                continue;

            if (_currentFilterMode == LogFilterMode.Info &&
                !lower.Contains("[inf]") && !lower.Contains("[info]"))
                continue;

            if (_currentFilterMode == LogFilterMode.Debug &&
                !lower.Contains("[dbg]") && !lower.Contains("[debug]") && !lower.Contains("[vrb]"))
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
            return SolidColorBrush.Parse("#FF6B6B");
        }
        if (lower.Contains("[wrn]") || lower.Contains("[warn]") || lower.Contains("warning:"))
        {
            return SolidColorBrush.Parse("#F1C40F");
        }
        if (lower.Contains("[dbg]") || lower.Contains("[debug]") || lower.Contains("[vrb]"))
        {
            return SolidColorBrush.Parse("#7F8C8D");
        }
        if (lower.Contains("[inf]") || lower.Contains("[info]"))
        {
            return SolidColorBrush.Parse("#E0E6ED");
        }
        return SolidColorBrush.Parse("#BDC3C7");
    }

    public void CopyLogToClipboard()
    {
        var text = string.Join(Environment.NewLine, FilteredLines.Select(l => l.Text));
        _ = ClipboardHelper.CopyWithFeedbackAsync(text, s => CopyButtonText = s);
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
