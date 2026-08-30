using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class ServerHistoryViewModel : ViewModelBase
{
    private readonly DataManager _cfg;
    private List<ServerHistoryEntry> _allEntries = new();
    private string _searchFilter = "";
    private bool _hasEntries;
    private bool _isEmpty;

    public ObservableCollection<ServerHistoryItemViewModel> Entries { get; } = new();

    public event Action? RequestClose;

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

    public bool HasEntries
    {
        get => _hasEntries;
        set => SetProperty(ref _hasEntries, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public ServerHistoryViewModel()
    {
        _cfg = Locator.Current.GetRequiredService<DataManager>();
        Reload();
    }

    public void Reload()
    {
        _allEntries = _cfg.GetServerHistory();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Entries.Clear();
        var query = SearchFilter.Trim();

        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allEntries
            : _allEntries.Where(x =>
                x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Address.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var entry in filtered)
        {
            Entries.Add(new ServerHistoryItemViewModel(entry, this));
        }

        HasEntries = Entries.Count > 0;
        IsEmpty = Entries.Count == 0;
    }

    public void Connect(ServerHistoryItemViewModel item)
    {
        var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.DataContext as MainWindowViewModel;
        if (mainVm != null)
        {
            ConnectingViewModel.StartConnect(mainVm, item.Address);
            RequestClose?.Invoke();
        }
    }

    public void Remove(ServerHistoryItemViewModel item)
    {
        _cfg.RemoveServerHistoryEntry(item.Address);
        _allEntries.RemoveAll(x => string.Equals(x.Address, item.Address, StringComparison.OrdinalIgnoreCase));
        Entries.Remove(item);
        HasEntries = Entries.Count > 0;
        IsEmpty = Entries.Count == 0;
    }

    public void ClearAll()
    {
        _cfg.ClearServerHistory();
        _allEntries.Clear();
        Entries.Clear();
        HasEntries = false;
        IsEmpty = true;
    }

    public async void CopyAddress(ServerHistoryItemViewModel item)
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window?.Clipboard != null)
        {
            await window.Clipboard.SetTextAsync(item.Address);
        }
    }

    public void AddFavorite(ServerHistoryItemViewModel item)
    {
        try
        {
            _cfg.AddFavoriteServer(new FavoriteServer(item.Name, item.Address));
            _cfg.CommitConfig();
        }
        catch
        {
            // Already favorited
        }
    }
}

public sealed class ServerHistoryItemViewModel : ViewModelBase
{
    private readonly ServerHistoryEntry _entry;
    private readonly ServerHistoryViewModel _parent;

    public string Address => _entry.Address;
    public string Name => !string.IsNullOrWhiteSpace(_entry.Name) ? _entry.Name : _entry.Address;
    public DateTime TimeUtc => _entry.TimeUtc;
    public string FormattedTime => _entry.TimeUtc.ToLocalTime().ToString("g");

    public ServerHistoryItemViewModel(ServerHistoryEntry entry, ServerHistoryViewModel parent)
    {
        _entry = entry;
        _parent = parent;
    }

    public void Connect() => _parent.Connect(this);
    public void Remove() => _parent.Remove(this);
    public void CopyAddress() => _parent.CopyAddress(this);
    public void AddFavorite() => _parent.AddFavorite(this);
}
