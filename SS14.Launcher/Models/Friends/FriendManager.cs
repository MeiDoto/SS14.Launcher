using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Splat;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Models.Friends;

public sealed class FriendEntry
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("favorite_server")]
    public string? FavoriteServerAddress { get; set; }

    [JsonPropertyName("last_seen_server")]
    public string? LastSeenServerAddress { get; set; }

    [JsonPropertyName("last_seen_server_name")]
    public string? LastSeenServerName { get; set; }

    [JsonPropertyName("last_seen_time")]
    public DateTime? LastSeenTime { get; set; }
}

public sealed class FriendManager
{
    private static readonly Lazy<FriendManager> _instance = new(() => new FriendManager());
    public static FriendManager Instance => _instance.Value;

    private readonly object _lock = new();
    private readonly Dictionary<string, FriendEntry> _friends = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public event Action? FriendsChanged;

    public FriendManager()
    {
        Load();
    }

    public void Load()
    {
        lock (_lock)
        {
            _friends.Clear();
            _initialized = true;
            var cfg = Locator.Current.GetService<DataManager>();
            if (cfg == null)
                return;

            try
            {
                var json = cfg.GetCVar(CVars.FriendsList);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var list = JsonSerializer.Deserialize<List<FriendEntry>>(json);
                    if (list != null)
                    {
                        foreach (var entry in list)
                        {
                            if (!string.IsNullOrWhiteSpace(entry.Username))
                            {
                                _friends[entry.Username.Trim()] = entry;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load friends list from configuration");
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            var cfg = Locator.Current.GetService<DataManager>();
            if (cfg == null)
                return;

            try
            {
                var list = _friends.Values.ToList();
                var json = JsonSerializer.Serialize(list);
                cfg.SetCVar(CVars.FriendsList, json);
                _ = cfg.CommitConfig();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save friends list to configuration");
            }
        }

        FriendsChanged?.Invoke();
    }

    public IReadOnlyList<FriendEntry> GetAllFriends()
    {
        lock (_lock)
        {
            if (!_initialized)
                Load();

            return _friends.Values.OrderBy(f => f.Username, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public bool TryGetFriend(string username, [NotNullWhen(true)] out FriendEntry? friend)
    {
        lock (_lock)
        {
            if (!_initialized)
                Load();

            return _friends.TryGetValue(username.Trim(), out friend);
        }
    }

    public bool AddOrUpdateFriend(string username, string? note = null, string? favoriteServer = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var cleanUser = username.Trim();
        lock (_lock)
        {
            if (!_initialized)
                Load();

            if (_friends.TryGetValue(cleanUser, out var existing))
            {
                if (note != null) existing.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                if (favoriteServer != null) existing.FavoriteServerAddress = string.IsNullOrWhiteSpace(favoriteServer) ? null : favoriteServer.Trim();
            }
            else
            {
                _friends[cleanUser] = new FriendEntry
                {
                    Username = cleanUser,
                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                    FavoriteServerAddress = string.IsNullOrWhiteSpace(favoriteServer) ? null : favoriteServer.Trim(),
                    AddedAt = DateTime.UtcNow
                };
            }
        }

        Save();
        return true;
    }

    public bool RemoveFriend(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        bool removed;
        lock (_lock)
        {
            if (!_initialized)
                Load();

            removed = _friends.Remove(username.Trim());
        }

        if (removed)
        {
            Save();
        }

        return removed;
    }

    public void RecordFriendSeen(string username, string serverAddress, string? serverName = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(serverAddress))
            return;

        lock (_lock)
        {
            if (!_initialized)
                Load();

            if (_friends.TryGetValue(username.Trim(), out var entry))
            {
                entry.LastSeenServerAddress = serverAddress.Trim();
                if (!string.IsNullOrWhiteSpace(serverName))
                {
                    entry.LastSeenServerName = serverName.Trim();
                }
                entry.LastSeenTime = DateTime.UtcNow;
            }
            else
            {
                return;
            }
        }

        Save();
    }

    public List<FriendEntry> GetFriendsOnServer(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
            return new List<FriendEntry>();

        var cleanAddress = serverAddress.Trim();
        lock (_lock)
        {
            if (!_initialized)
                Load();

            return _friends.Values
                .Where(f =>
                    string.Equals(f.FavoriteServerAddress, cleanAddress, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.LastSeenServerAddress, cleanAddress, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public bool HasFriendsOnServer(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
            return false;

        var cleanAddress = serverAddress.Trim();
        lock (_lock)
        {
            if (!_initialized)
                Load();

            return _friends.Values.Any(f =>
                string.Equals(f.FavoriteServerAddress, cleanAddress, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.LastSeenServerAddress, cleanAddress, StringComparison.OrdinalIgnoreCase));
        }
    }
}
