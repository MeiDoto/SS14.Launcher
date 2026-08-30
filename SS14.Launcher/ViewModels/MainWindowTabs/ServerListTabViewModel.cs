using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ServerStatus;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public partial class ServerListTabViewModel : MainWindowTabViewModel
{
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly MainWindowViewModel _windowVm;
    private readonly ServerListCache _serverListCache;

    public ObservableList<ServerEntryViewModel> SearchedServers { get; } = [];
    private readonly Dictionary<string, ServerEntryViewModel> _serverViewModels = new();

    private string? _searchString;
    private readonly DispatcherTimer _searchThrottle = new() { Interval = TimeSpan.FromMilliseconds(200) };

    public override string Name
    {
        get
        {
            var cfg = Splat.Locator.Current.GetRequiredService<SS14.Launcher.Models.Data.DataManager>();
            var custom = cfg.GetCVar(SS14.Launcher.Models.Data.CVars.CustomServersTabName);
            return !string.IsNullOrWhiteSpace(custom) ? custom : _loc.GetString("tab-servers-title");
        }
    }

    public string? SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString == value)
                return;

            OnPropertyChanging();
            _searchString = value;
            OnPropertyChanged();

            _searchThrottle.Stop();
            if (string.IsNullOrEmpty(value))
            {
                UpdateSearchedList();
            }
            else
            {
                _searchThrottle.Start();
            }
        }
    }

    public bool SpinnerVisible => _serverListCache.Status < RefreshListStatus.Updated;
    public bool RefreshEnabled => _serverListCache.Status != RefreshListStatus.UpdatingMaster;

    public string ListText
    {
        get
        {
            var status = _serverListCache.Status;
            switch (status)
            {
                case RefreshListStatus.Error:
                    return _loc.GetString("tab-servers-list-status-error");
                case RefreshListStatus.PartialError:
                    return _loc.GetString("tab-servers-list-status-partial-error");
                case RefreshListStatus.UpdatingMaster:
                    return _loc.GetString("tab-servers-list-status-updating-master");
                case RefreshListStatus.NotUpdated:
                    return "";
                case RefreshListStatus.Updated:
                default:
                    if (SearchedServers.Count == 0 && _serverListCache.AllServers.Count != 0)
                        return _loc.GetString("tab-servers-list-status-none-filtered");

                    if (_serverListCache.AllServers.Count == 0)
                        return _loc.GetString("tab-servers-list-status-none");

                    return "";
            }
        }
    }

    [ObservableProperty] private bool _filtersVisible;

    public ServerListFiltersViewModel Filters { get; }

    public ServerListTabViewModel(MainWindowViewModel windowVm)
    {
        Filters = new ServerListFiltersViewModel(windowVm.Cfg, _loc);
        Filters.FiltersUpdated += FiltersOnFiltersUpdated;

        _windowVm = windowVm;
        _serverListCache = Locator.Current.GetRequiredService<ServerListCache>();

        _serverListCache.AllServers.CollectionChanged += ServerListUpdated;

        _serverListCache.PropertyChanged += (_, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(ServerListCache.Status):
                    OnPropertyChanged(nameof(ListText));
                    OnPropertyChanged(nameof(SpinnerVisible));
                    OnPropertyChanged(nameof(RefreshEnabled));
                    break;
            }
        };

        _searchThrottle.Tick += (_, _) =>
        {
            _searchThrottle.Stop();
            UpdateSearchedList();
        };

        _loc.LanguageSwitched += () => Filters.UpdatePresentFilters(_serverListCache.AllServers);
    }

    private void FiltersOnFiltersUpdated()
    {
        UpdateSearchedList();
    }

    public override void Selected()
    {
        _serverListCache.RequestInitialUpdate();
    }

    public void RefreshPressed()
    {
        if (!RefreshEnabled)
            return;

        _serverListCache.RequestRefresh();
    }

    private void ServerListUpdated(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.Action == NotifyCollectionChangedAction.Reset)
            _serverViewModels.Clear();

        Filters.UpdatePresentFilters(_serverListCache.AllServers);

        UpdateSearchedList();
    }

    private void UpdateSearchedList()
    {
        var favorites = new HashSet<string>(_windowVm.Cfg.FavoriteServers.Keys);
        var searchActive = !string.IsNullOrWhiteSpace(SearchString);

        var scoredList = new List<(ServerStatusData Server, int SearchScore, double QualityScore)>();
        var serverList = _serverListCache.AllServers.ToArray();

        foreach (var server in serverList)
        {
            var isFav = favorites.Contains(server.Address);
            var searchScore = CalculateSearchScore(server);

            if (searchActive && searchScore <= 0)
                continue;

            var qualityScore = SearchAlgorithm.CalculateQualityScore(server, isFav);
            scoredList.Add((server, searchScore, qualityScore));
        }

        var filteredServers = scoredList.Select(x => x.Server).ToList();
        Filters.ApplyFilters(filteredServers);

        // Filter out items that were removed by Filters
        var filteredSet = new HashSet<ServerStatusData>(filteredServers);
        scoredList.RemoveAll(x => !filteredSet.Contains(x.Server));

        if (searchActive)
        {
            scoredList.Sort((a, b) =>
            {
                var cmp = b.SearchScore.CompareTo(a.SearchScore);
                if (cmp != 0) return cmp;
                return b.Server.PlayerCount.CompareTo(a.Server.PlayerCount);
            });
        }
        else
        {
            scoredList.Sort((a, b) =>
            {
                var cmp = b.QualityScore.CompareTo(a.QualityScore);
                if (cmp != 0) return cmp;
                return b.Server.PlayerCount.CompareTo(a.Server.PlayerCount);
            });
        }

        var searchedServers = new List<ServerEntryViewModel>(scoredList.Count);
        foreach (var item in scoredList)
        {
            if (!_serverViewModels.TryGetValue(item.Server.Address, out var vm))
            {
                vm = new ServerEntryViewModel(_windowVm, item.Server, _serverListCache, _windowVm.Cfg);
                _serverViewModels.Add(item.Server.Address, vm);
            }

            searchedServers.Add(vm);
        }

        SearchedServers.SetItems(searchedServers);

        OnPropertyChanged(nameof(ListText));
    }

    private int CalculateSearchScore(ServerStatusData data)
    {
        if (string.IsNullOrWhiteSpace(SearchString))
            return 100;

        var terms = SearchString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nameScore = SearchAlgorithm.GetMatchScore(SearchString, data.Name);
        var totalScore = nameScore * 3;

        if (data.Tags != null)
        {
            foreach (var tag in data.Tags)
            {
                var tagScore = SearchAlgorithm.GetMatchScore(SearchString, tag);
                if (tagScore > 0)
                {
                    totalScore = Math.Max(totalScore, tagScore * 2);
                }
            }
        }

        var descScore = SearchAlgorithm.GetMatchScore(SearchString, data.Description);
        if (descScore > 0)
        {
            totalScore = Math.Max(totalScore, (int)(descScore * 0.8));
        }

        var addrScore = SearchAlgorithm.GetMatchScore(SearchString, data.Address);
        if (addrScore > 0)
        {
            totalScore = Math.Max(totalScore, (int)(addrScore * 0.5));
        }

        if (terms.Length > 1 && !string.IsNullOrEmpty(data.Name))
        {
            var bm25 = AdvancedAlgorithms.OkapiBM25Score(terms, $"{data.Name} {data.Description} {string.Join(' ', data.Tags ?? Array.Empty<string>())}");
            if (bm25 > 0.0)
            {
                totalScore += (int)(bm25 * 50);
            }
        }

        return totalScore;
    }
}
