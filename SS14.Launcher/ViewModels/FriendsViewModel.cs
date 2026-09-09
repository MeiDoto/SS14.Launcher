using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Friends;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class FriendItemViewModel : ViewModelBase
{
    private readonly FriendEntry _entry;
    private readonly FriendsViewModel _parent;
    private readonly LocalizationManager _loc = LocalizationManager.Instance;

    public string Username => _entry.Username;
    public string? Note => _entry.Note;
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public string? ServerAddress => _entry.FavoriteServerAddress ?? _entry.LastSeenServerAddress;
    public string? ServerName => _entry.LastSeenServerName ?? ServerAddress;
    public bool HasServer => !string.IsNullOrWhiteSpace(ServerAddress);

    public string StatusText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_entry.LastSeenServerName))
            {
                var time = _entry.LastSeenTime.HasValue
                    ? _entry.LastSeenTime.Value.ToLocalTime().ToString("dd.MM HH:mm")
                    : "";
                return $"{_entry.LastSeenServerName} ({time})";
            }

            if (!string.IsNullOrWhiteSpace(_entry.FavoriteServerAddress))
            {
                return _entry.FavoriteServerAddress;
            }

            return _loc.GetString("friends-status-offline");
        }
    }

    public FriendItemViewModel(FriendEntry entry, FriendsViewModel parent)
    {
        _entry = entry;
        _parent = parent;
    }

    public void Connect()
    {
        if (!string.IsNullOrWhiteSpace(ServerAddress))
        {
            _parent.ConnectToServer(ServerAddress);
        }
    }

    public void Delete()
    {
        _parent.RemoveFriend(Username);
    }
}

public sealed class FriendsViewModel : ViewModelBase
{
    private readonly FriendManager _friendManager = FriendManager.Instance;
    private readonly Action<string>? _onConnect;

    public ObservableCollection<FriendItemViewModel> AllFriends { get; } = new();
    public ObservableCollection<FriendItemViewModel> FilteredFriends { get; } = new();

    public event Action? RequestClose;

    private string _newUsername = "";
    public string NewUsername
    {
        get => _newUsername;
        set
        {
            if (SetProperty(ref _newUsername, value))
            {
                OnPropertyChanged(nameof(CanAdd));
            }
        }
    }

    private string _newNote = "";
    public string NewNote
    {
        get => _newNote;
        set => SetProperty(ref _newNote, value);
    }

    private string _newServerAddress = "";
    public string NewServerAddress
    {
        get => _newServerAddress;
        set => SetProperty(ref _newServerAddress, value);
    }

    private string _searchFilter = "";
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

    public bool CanAdd => !string.IsNullOrWhiteSpace(NewUsername);
    public bool HasFriends => AllFriends.Count > 0;
    public bool IsEmpty => AllFriends.Count == 0;

    public FriendsViewModel(Action<string>? onConnect = null)
    {
        _onConnect = onConnect;
        _friendManager.FriendsChanged += RefreshList;
        RefreshList();
    }

    public void AddFriend()
    {
        if (!CanAdd)
            return;

        _friendManager.AddOrUpdateFriend(NewUsername, NewNote, NewServerAddress);
        NewUsername = "";
        NewNote = "";
        NewServerAddress = "";
        RefreshList();
    }

    public void RemoveFriend(string username)
    {
        _friendManager.RemoveFriend(username);
        RefreshList();
    }

    public void ConnectToServer(string address)
    {
        if (_onConnect != null)
        {
            _onConnect(address);
            RequestClose?.Invoke();
            return;
        }

        var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.DataContext as MainWindowViewModel;
        if (mainVm != null)
        {
            ConnectingViewModel.StartConnect(mainVm, address);
            RequestClose?.Invoke();
        }
    }

    public void RefreshList()
    {
        AllFriends.Clear();
        foreach (var friend in _friendManager.GetAllFriends())
        {
            AllFriends.Add(new FriendItemViewModel(friend, this));
        }

        ApplyFilter();
        OnPropertyChanged(nameof(HasFriends));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void ApplyFilter()
    {
        FilteredFriends.Clear();
        var filter = SearchFilter?.Trim();

        foreach (var item in AllFriends)
        {
            if (string.IsNullOrWhiteSpace(filter) ||
                item.Username.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (item.Note != null && item.Note.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                (item.ServerName != null && item.ServerName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            {
                FilteredFriends.Add(item);
            }
        }
    }
}
