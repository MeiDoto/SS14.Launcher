using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.ServerStatus;
using SS14.Launcher.Utility;
using static SS14.Launcher.Utility.HubUtility;

using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.Input;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public sealed class ServerEntryViewModel : ObservableRecipient, IRecipient<FavoritesChanged>, IViewModelBase
{
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly ServerStatusData _cacheData;
    private readonly IServerSource _serverSource;
    private readonly DataManager _cfg;
    private readonly MainWindowViewModel _windowVm;
    private string Address => _cacheData.Address;
    private string _fallbackName = string.Empty;
    private bool _isExpanded;

    public ICommand ConnectCommand { get; }
    public ICommand FavoriteButtonCommand { get; }
    public ICommand FavoriteRaiseButtonCommand { get; }
    public ICommand ToggleSlotWatcherCommand { get; }

    public ServerEntryViewModel(MainWindowViewModel windowVm, ServerStatusData cacheData, IServerSource serverSource,
        DataManager cfg)
    {
        _cfg = cfg;
        _windowVm = windowVm;
        _cacheData = cacheData;
        _serverSource = serverSource;
        ConnectCommand = new RelayCommand(ConnectPressed);
        FavoriteButtonCommand = new RelayCommand(FavoriteButtonPressed);
        FavoriteRaiseButtonCommand = new RelayCommand(FavoriteRaiseButtonPressed);
        ToggleSlotWatcherCommand = new RelayCommand(ToggleSlotWatcher);

        _cacheData.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ServerStatusData.PanicBunker))
            {
                OnPropertyChanged(nameof(HasPanicBunker));
                OnPropertyChanged(nameof(PanicBunkerToolTip));
                OnPropertyChanged(nameof(PanicBunkerDetailedDescription));
            }
        };
    }

    public ServerEntryViewModel(
        MainWindowViewModel windowVm,
        ServerStatusData cacheData,
        FavoriteServer favorite,
        IServerSource serverSource,
        DataManager cfg)
        : this(windowVm, cacheData, serverSource, cfg)
    {
        Favorite = favorite;
    }

    public ServerEntryViewModel(
        MainWindowViewModel windowVm,
        ServerStatusDataWithFallbackName ssdfb,
        IServerSource serverSource,
        DataManager cfg)
        : this(windowVm, ssdfb.Data, serverSource, cfg)
    {
        FallbackName = ssdfb.FallbackName ?? "";
    }

    public void Tick()
    {
        OnPropertyChanged(nameof(RoundStartTime));
    }

    public void ConnectPressed()
    {
        ConnectingViewModel.StartConnect(_windowVm, Address);
    }

    private string _copyAddressButtonText = "";
    public string CopyAddressButtonText
    {
        get => string.IsNullOrEmpty(_copyAddressButtonText) ? _loc.GetString("server-entry-copy-address") : _copyAddressButtonText;
        set => SetProperty(ref _copyAddressButtonText, value);
    }

    public void CopyAddress()
    {
        _ = ClipboardHelper.CopyWithFeedbackAsync(Address, s => CopyAddressButtonText = s);
    }

    public FavoriteServer? Favorite { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            CheckUpdateInfo();
        }
    }

    public string Name => Favorite?.Name ?? _cacheData.Name ?? _fallbackName;

    private string FavoriteButtonText => IsFavorite
        ? _loc.GetString("server-entry-remove-favorite")
        : _loc.GetString("server-entry-add-favorite");

    public bool IsFavorite => _cfg.FavoriteServers.Lookup(Address).HasValue;

    public bool ViewedInFavoritesPane { get; set; }

    public bool HaveData => _cacheData.Status == ServerStatusCode.Online;

    public string ServerStatusString
    {
        get
        {
            switch (_cacheData.Status)
            {
                case ServerStatusCode.Offline:
                    return _loc.GetString("server-entry-offline");
                case ServerStatusCode.FetchingStatus:
                case ServerStatusCode.Online:
                    return _loc.GetString("server-entry-fetching");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    // Give a ratio for servers with a defined player count, or just a current number for those without.
    public string PlayerCountString =>
        _loc.GetString("server-entry-player-count",
            ("players", _cacheData.PlayerCount), ("max", _cacheData.SoftMaxPlayerCount));


    public DateTime? RoundStartTime => _cacheData.RoundStartTime;

    public string RoundStatusString =>
        _cacheData.RoundStatus == GameRoundStatus.InLobby
            ? _loc.GetString("server-entry-status-lobby")
            : "";

    public string Description
    {
        get
        {
            switch (_cacheData.Status)
            {
                case ServerStatusCode.Offline:
                    return _loc.GetString("server-entry-description-offline");
                case ServerStatusCode.FetchingStatus:
                    return _loc.GetString("server-entry-description-fetching");
            }

            return _cacheData.StatusInfo switch
            {
                ServerStatusInfoCode.NotFetched => _loc.GetString("server-entry-description-fetching"),
                ServerStatusInfoCode.Fetching => _loc.GetString("server-entry-description-fetching"),
                ServerStatusInfoCode.Error => _loc.GetString("server-entry-description-error"),
                ServerStatusInfoCode.Fetched => _cacheData.Description ??
                                                _loc.GetString("server-entry-description-none"),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public bool IsOnline => _cacheData.Status == ServerStatusCode.Online;
    public bool HasPanicBunker => _cacheData.PanicBunker;

    public string PanicBunkerToolTip
    {
        get
        {
            if (!_cacheData.PanicBunker)
                return "";

            var parts = new System.Collections.Generic.List<string> { _loc.GetString("server-entry-panic-bunker-active") };
            if (_cacheData.PanicBunkerMinAccountAge is { } age and > 0)
            {
                var days = age / 1440;
                if (days > 0)
                    parts.Add(_loc.GetString("server-entry-panic-bunker-account-age-days", ("days", days)));
                else
                    parts.Add(_loc.GetString("server-entry-panic-bunker-account-age-mins", ("mins", age)));
            }
            if (_cacheData.PanicBunkerMinOverallHours is { } hours and > 0)
            {
                parts.Add(_loc.GetString("server-entry-panic-bunker-overall-hours", ("hours", (int)hours)));
            }

            return string.Join(" • ", parts);
        }
    }

    public string PanicBunkerDetailedDescription
    {
        get
        {
            if (!_cacheData.PanicBunker)
                return "";

            var parts = new System.Collections.Generic.List<string>
            {
                _loc.GetString("server-entry-panic-bunker-details-title")
            };

            if (_cacheData.PanicBunkerMinAccountAge is { } age and > 0)
            {
                var days = age / 1440;
                parts.Add(days > 0
                    ? _loc.GetString("server-entry-panic-bunker-req-age-days", ("days", days))
                    : _loc.GetString("server-entry-panic-bunker-req-age-mins", ("mins", age)));
            }

            if (_cacheData.PanicBunkerMinOverallHours is { } hours and > 0)
            {
                parts.Add(_loc.GetString("server-entry-panic-bunker-req-playtime", ("hours", (int)hours)));
            }

            return string.Join("\n• ", parts);
        }
    }

    public bool IsSlotWatcherActive => _cfg.IsSlotServerWatched(Address);

    public bool IsSlotWatcherEnabled => _cfg.GetCVar(CVars.EnableSlotNotifier);

    public string SlotWatcherButtonText => IsSlotWatcherActive ? "🔔" : "🔕";

    public string SlotWatcherToolTip => IsSlotWatcherActive
        ? _loc.GetString("server-entry-slot-watcher-active")
        : _loc.GetString("server-entry-slot-watcher-inactive");

    public void ToggleSlotWatcher()
    {
        _cfg.ToggleWatchedSlotServer(Address);
        OnPropertyChanged(nameof(IsSlotWatcherActive));
        OnPropertyChanged(nameof(SlotWatcherButtonText));
        OnPropertyChanged(nameof(SlotWatcherToolTip));
    }

    public bool HasPlaytime => _cfg.GetCVar(CVars.TrackPlaytime) && PlaytimeSeconds > 0;

    public long PlaytimeSeconds => _cfg.GetPlaytimeForServer(Address);

    public string PlaytimeString => PlaytimeFormatter.Format(PlaytimeSeconds);

    public string PlaytimeToolTip => _loc.GetString("server-entry-playtime-tooltip", ("time", PlaytimeString));

    public string PlaytimeBottomText => _loc.GetString("server-entry-playtime-bottom", ("time", PlaytimeString));

    public string FallbackName
    {
        get => _fallbackName;
        set
        {
            SetProperty(ref _fallbackName, value);
            OnPropertyChanged(nameof(Name));
        }
    }

    public ServerStatusData CacheData => _cacheData;

    public string? FetchedFrom
    {
        get
        {
            if (_cfg.HasCustomHubs)
            {
                return _cacheData.HubAddress == null
                    ? null
                    : _loc.GetString("server-fetched-from-hub", ("hub", GetHubShortName(_cacheData.HubAddress)));
            }

            return null;
        }
    }

    public bool ShowFetchedFrom => _cfg.HasCustomHubs && !ViewedInFavoritesPane;

    public void FavoriteButtonPressed()
    {
        if (IsFavorite)
        {
            // Remove favorite.
            _cfg.RemoveFavoriteServer(_cfg.FavoriteServers.Lookup(Address).Value);
        }
        else
        {
            var fav = new FavoriteServer(_cacheData.Name ?? FallbackName, Address);
            _cfg.AddFavoriteServer(fav);
        }

        _cfg.CommitConfig();
    }

    public void FavoriteRaiseButtonPressed()
    {
        if (IsFavorite)
        {
            // Usual business, raise priority
            _cfg.RaiseFavoriteServer(_cfg.FavoriteServers.Lookup(Address).Value);
        }

        _cfg.CommitConfig();
    }

    public void Receive(FavoritesChanged message)
    {
        OnPropertyChanged(nameof(IsFavorite));
        OnPropertyChanged(nameof(FavoriteButtonText));
    }

    private void CheckUpdateInfo()
    {
        if (!IsExpanded || _cacheData.Status != ServerStatusCode.Online)
            return;

        if (_cacheData.StatusInfo is not (ServerStatusInfoCode.NotFetched or ServerStatusInfoCode.Error))
            return;

        _serverSource.UpdateInfoFor(_cacheData);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        _cacheData.PropertyChanged += OnCacheDataOnPropertyChanged;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        _cacheData.PropertyChanged -= OnCacheDataOnPropertyChanged;
    }

    private void OnCacheDataOnPropertyChanged(object? _, PropertyChangedEventArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(IServerStatusData.PlayerCount):
            case nameof(IServerStatusData.SoftMaxPlayerCount):
                OnPropertyChanged(nameof(ServerStatusString));
                OnPropertyChanged(nameof(PlayerCountString));
                break;

            case nameof(IServerStatusData.RoundStartTime):
                OnPropertyChanged(nameof(RoundStartTime));
                break;

            case nameof(IServerStatusData.RoundStatus):
                OnPropertyChanged(nameof(RoundStatusString));
                break;

            case nameof(IServerStatusData.Status):
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(ServerStatusString));
                OnPropertyChanged(nameof(PlayerCountString));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(HaveData));
                CheckUpdateInfo();
                break;

            case nameof(IServerStatusData.Name):
                OnPropertyChanged(nameof(Name));
                break;

            case nameof(IServerStatusData.Description):
            case nameof(IServerStatusData.StatusInfo):
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(HaveData));
                break;
        }
    }
}
