using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public class LocalBuildsViewModel : ViewModelBase
{
    private readonly DataManager _cfg;
    private ObservableCollection<LocalBuildItemViewModel> _builds = new();
    private string _newBuildName = "";
    private string _newBuildPath = "";
    private string _statusMessage = "";

    public ObservableCollection<LocalBuildItemViewModel> Builds
    {
        get => _builds;
        set => SetProperty(ref _builds, value);
    }

    public string NewBuildName
    {
        get => _newBuildName;
        set => SetProperty(ref _newBuildName, value);
    }

    public string NewBuildPath
    {
        get => _newBuildPath;
        set => SetProperty(ref _newBuildPath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public LocalBuildsViewModel()
    {
        _cfg = Locator.Current.GetRequiredService<DataManager>();
        LoadBuilds();
    }

    public void LoadBuilds()
    {
        try
        {
            var json = _cfg.GetCVar(CVars.LocalBuilds);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var list = JsonSerializer.Deserialize<LocalBuildEntry[]>(json);
                if (list != null)
                {
                    Builds = new ObservableCollection<LocalBuildItemViewModel>(
                        list.Select(e => new LocalBuildItemViewModel(e, this)));
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-error-load", ("error", ex.Message));
        }

        Builds = new ObservableCollection<LocalBuildItemViewModel>();
    }

    public bool HasBuilds => Builds.Count > 0;
    public bool HasNoBuilds => Builds.Count == 0;

    public void SaveBuilds()
    {
        try
        {
            var list = Builds.Select(b => b.Entry).ToArray();
            var json = JsonSerializer.Serialize(list);
            _cfg.SetCVar(CVars.LocalBuilds, json);
            _cfg.CommitConfig();
            OnPropertyChanged(nameof(HasBuilds));
            OnPropertyChanged(nameof(HasNoBuilds));
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-error-save", ("error", ex.Message));
        }
    }

    public async void BrowseFile(Window parent)
    {
        try
        {
            var files = await parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LocalizationManager.Instance.GetString("local-builds-picker-title"),
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(LocalizationManager.Instance.GetString("local-builds-picker-all"))
                    {
                        Patterns = new[] { "*.zip", "*.exe", "*.dll", "*", "*.*" }
                    },
                    new FilePickerFileType(LocalizationManager.Instance.GetString("local-builds-picker-bundles"))
                    {
                        Patterns = new[] { "*.zip" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                NewBuildPath = file.Path.LocalPath;
                if (string.IsNullOrWhiteSpace(NewBuildName))
                {
                    NewBuildName = Path.GetFileNameWithoutExtension(NewBuildPath);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-error-pick", ("error", ex.Message));
        }
    }

    public void AddBuild()
    {
        if (string.IsNullOrWhiteSpace(NewBuildName) || string.IsNullOrWhiteSpace(NewBuildPath))
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-validation-empty");
            return;
        }

        if (!File.Exists(NewBuildPath) && !Directory.Exists(NewBuildPath))
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-validation-not-found");
            return;
        }

        var entry = new LocalBuildEntry
        {
            Id = Guid.NewGuid().ToString(),
            Name = NewBuildName.Trim(),
            Path = NewBuildPath.Trim(),
            AddedDate = DateTime.UtcNow
        };

        Builds.Insert(0, new LocalBuildItemViewModel(entry, this));
        SaveBuilds();

        NewBuildName = "";
        NewBuildPath = "";
        StatusMessage = LocalizationManager.Instance.GetString("local-builds-added", ("name", entry.Name));
    }

    public void RemoveBuild(LocalBuildItemViewModel item)
    {
        Builds.Remove(item);
        SaveBuilds();
        StatusMessage = LocalizationManager.Instance.GetString("local-builds-removed", ("name", item.Name));
    }

    public async void LaunchBuild(LocalBuildItemViewModel item, Window? dialogWindow)
    {
        try
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-launching", ("name", item.Name));

            if (item.Entry.Path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (dialogWindow != null)
                {
                    var storageFile = await dialogWindow.StorageProvider.TryGetFileFromPathAsync(item.Entry.Path);
                    if (storageFile != null)
                    {
                        var mainVm = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.DataContext as MainWindowViewModel;
                        if (mainVm != null)
                        {
                            dialogWindow.Close();
                            ConnectingViewModel.StartContentBundle(mainVm, storageFile);
                            return;
                        }
                    }
                }
            }

            // Launch executable or custom command
            if (File.Exists(item.Entry.Path))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = item.Entry.Path,
                    WorkingDirectory = Path.GetDirectoryName(item.Entry.Path) ?? "",
                    UseShellExecute = true
                };
                Process.Start(psi);
                StatusMessage = LocalizationManager.Instance.GetString("local-builds-launched", ("name", item.Name));
            }
            else
            {
                StatusMessage = LocalizationManager.Instance.GetString("local-builds-not-found");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationManager.Instance.GetString("local-builds-error-launch", ("error", ex.Message));
        }
    }
}

public class LocalBuildItemViewModel : ViewModelBase
{
    public LocalBuildEntry Entry { get; }
    private readonly LocalBuildsViewModel _parent;

    public string Name => Entry.Name;
    public string Path => Entry.Path;
    public string TypeBadge => Entry.Path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        ? LocalizationManager.Instance.GetString("local-builds-badge-zip")
        : LocalizationManager.Instance.GetString("local-builds-badge-exe");
    public string AddedDateText => LocalizationManager.Instance.GetString("local-builds-added-date", ("date", $"{Entry.AddedDate:dd.MM.yyyy HH:mm}"));

    public LocalBuildItemViewModel(LocalBuildEntry entry, LocalBuildsViewModel parent)
    {
        Entry = entry;
        _parent = parent;
    }

    public void OpenFolder()
    {
        try
        {
            var dir = Directory.Exists(Entry.Path) ? Entry.Path : System.IO.Path.GetDirectoryName(Entry.Path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to open folder for local build {Path}", Entry.Path);
        }
    }

    public void Delete()
    {
        _parent.RemoveBuild(this);
    }
}

public class LocalBuildEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}
