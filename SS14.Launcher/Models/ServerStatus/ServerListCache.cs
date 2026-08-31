using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Serilog;
using Splat;
using SS14.Launcher.Utility;
using SS14.Launcher.Api;
using SS14.Launcher.Models.Data;
using static SS14.Launcher.Api.HubApi;

namespace SS14.Launcher.Models.ServerStatus;

/// <summary>
///     Caches the Hub's server list.
/// </summary>
public sealed partial class ServerListCache : ObservableObject, IServerSource
{
    private readonly HubApi _hubApi = Locator.Current.GetRequiredService<HubApi>();
    private readonly DataManager _dataManager = Locator.Current.GetRequiredService<DataManager>();
    private readonly System.Threading.Timer _slotCheckTimer;

    private CancellationTokenSource? _refreshCancel;

    public ObservableList<ServerStatusData> AllServers { get; } = [];

    [ObservableProperty] private RefreshListStatus _status = RefreshListStatus.NotUpdated;

    public ServerListCache()
    {
        _slotCheckTimer = new System.Threading.Timer(OnSlotCheckTimerTick, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void OnSlotCheckTimerTick(object? state)
    {
        CheckWatchedSlots();
    }

    /// <summary>
    /// This function requests the initial update from the server if one hasn't already been requested.
    /// </summary>
    public void RequestInitialUpdate()
    {
        if (Status == RefreshListStatus.NotUpdated || Status == RefreshListStatus.Error)
        {
            RequestRefresh();
        }
    }

    /// <summary>
    /// This function performs a refresh.
    /// </summary>
    public void RequestRefresh()
    {
        _refreshCancel?.Cancel();
        _refreshCancel = new CancellationTokenSource(15000);
        RefreshServerList(_refreshCancel.Token);
    }

    /// <summary>
    /// Refreshes the server list asynchronously by querying all configured SS14 hub master servers,
    /// deduplicating entries by priority, and initiating background TCP latency measurements.
    /// In case of connection drops or errors, preserves existing cached servers (graceful degradation).
    /// </summary>
    /// <param name="cancel">Cancellation token to abort the network queries.</param>
    public async void RefreshServerList(CancellationToken cancel)
    {
        Status = RefreshListStatus.UpdatingMaster;

        try
        {
            var entries = new HashSet<HubServerListEntry>();
            var requests = new List<(Task<ServerListEntry[]> Request, Uri Hub)>();
            var allSucceeded = true;

            // Queue requests
            foreach (var hub in ConfigConstants.DefaultHubUrls)
            {
                requests.Add((_hubApi.GetServers(hub, cancel), new Uri(hub.Urls[0])));
            }

            foreach (var hub in _dataManager.Hubs.OrderBy(h => h.Priority))
            {
                requests.Add((_hubApi.GetServers(UrlFallbackSet.FromSingle(hub.Address), cancel), hub.Address));
            }

            // Await all requests
            try
            {
                await Task.WhenAll(requests.Select(t => t.Request));
            }
            catch
            {
                // Exceptions inspected per-task below
            }

            if (cancel.IsCancellationRequested)
            {
                Status = AllServers.Count > 0 ? RefreshListStatus.Updated : RefreshListStatus.NotUpdated;
                return;
            }

            // Process responses
            foreach (var (request, hub) in requests)
            {
                if (!request.IsCompletedSuccessfully)
                {
                    if (request.IsFaulted)
                    {
                        foreach (var ex in request.Exception!.InnerExceptions)
                        {
                            Log.Debug("Request to hub {HubAddress} failed: {Message}", hub, ex.Message);
                        }
                    }
                    else if (request.IsCanceled)
                    {
                        Log.Debug("Request to hub {HubAddress} failed: canceled", hub);
                    }

                    allSucceeded = false;
                    continue;
                }

                foreach (var entry in request.Result)
                {
                    // Don't add server if it was already provided by another hub with higher priority
                    var maybeNewEntry = new HubServerListEntry(entry.Address, hub.AbsoluteUri, entry.StatusData);
                    if (!entries.Add(maybeNewEntry))
                    {
                        Log.Verbose("Not adding {Entry} from {ThisHub} because it was already provided by {PreviousHub}",
                            entry.Address,
                            hub.AbsoluteUri,
                            maybeNewEntry.HubAddress);
                    }
                }
            }

            if (entries.Count == 0 && !allSucceeded && AllServers.Count > 0)
            {
                Log.Warning("Failed to fetch server list from hubs, preserving existing cached list ({Count} servers)", AllServers.Count);
                Status = RefreshListStatus.Error;
                return;
            }

            var newServerList = entries.Select(entry =>
            {
                var statusData = new ServerStatusData(entry.Address, entry.HubAddress);
                ServerStatusCache.ApplyStatus(statusData, entry.StatusData);
                return statusData;
            }).ToList();

            AllServers.Clear();
            AllServers.AddRange(newServerList);

            // Launch fast background ping measurement
            var serverSnapshot = AllServers.ToArray();
            _ = Task.Run(async () =>
            {
                using var sem = new SemaphoreSlim(25);
                var pingTasks = serverSnapshot.Select(async s =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        await ServerStatusCache.MeasurePingAsync(s);
                    }
                    finally
                    {
                        sem.Release();
                    }
                });
                await Task.WhenAll(pingTasks);
            });

            // Background preload for favorite servers
            if (_dataManager.GetCVar(CVars.FastLaunchPreload))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var http = Locator.Current.GetService<System.Net.Http.HttpClient>();
                        if (http != null)
                        {
                            foreach (var fav in _dataManager.FavoriteServers.Items)
                            {
                                try
                                {
                                    if (Uri.TryCreate(fav.Address, UriKind.Absolute, out var uri))
                                    {
                                        var infoUri = new Uri(uri, "/info");
                                        using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, infoUri);
                                        await http.SendAsync(req, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                                    }
                                }
                                catch
                                {
                                    // Ignore individual prefetch fails
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore background prefetch fails
                    }
                });
            }

            CheckWatchedSlots();

            if (AllServers.Count == 0)
                Status = RefreshListStatus.Error;
            else if (!allSucceeded && _dataManager.Hubs.Count > 0)
                Status = RefreshListStatus.PartialError;
            else
                Status = RefreshListStatus.Updated;
        }
        catch (OperationCanceledException)
        {
            Status = AllServers.Count > 0 ? RefreshListStatus.Updated : RefreshListStatus.NotUpdated;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to fetch server list due to exception");
            Status = AllServers.Count > 0 ? RefreshListStatus.Updated : RefreshListStatus.Error;
        }
    }

    private void CheckWatchedSlots()
    {
        if (!_dataManager.GetCVar(CVars.EnableSlotNotifier))
            return;

        var watched = _dataManager.GetWatchedSlotServers();
        if (watched.Count == 0 || AllServers.Count == 0)
            return;

        var loc = Localization.LocalizationManager.Instance;
        var snapshot = AllServers.ToArray();
        foreach (var s in snapshot)
        {
            if (watched.Contains(s.Address) && s.SoftMaxPlayerCount > 0 && s.PlayerCount < s.SoftMaxPlayerCount)
            {
                var title = loc.GetString("notification-slot-available-title");
                var msg = loc.GetString("notification-slot-available-desc",
                    ("server", s.Name ?? s.Address),
                    ("players", s.PlayerCount),
                    ("max", s.SoftMaxPlayerCount));

                Log.Information("Free player slot available on watched server: {Server} ({Players}/{Max})", s.Name ?? s.Address, s.PlayerCount, s.SoftMaxPlayerCount);
                DesktopNotificationManager.Notify(title, msg, s.Address);
            }
        }
    }

    void IServerSource.UpdateInfoFor(ServerStatusData statusData)
    {
        if (statusData.HubAddress == null)
        {
            Log.Error("Tried to get server info for hubbed server {Name} without HubAddress set", statusData.Name);
            return;
        }

        ServerStatusCache.UpdateInfoForCore(
            statusData,
            async token => await _hubApi.GetServerInfo(statusData.Address, statusData.HubAddress, token));
    }
}

public class ServerStatusDataWithFallbackName
{
    public readonly ServerStatusData Data;
    public readonly string? FallbackName;

    public ServerStatusDataWithFallbackName(ServerStatusData data, string? name)
    {
        Data = data;
        FallbackName = name;
    }
}

public enum RefreshListStatus
{
    /// <summary>
    /// Hasn't started updating yet?
    /// </summary>
    NotUpdated,

    /// <summary>
    /// Fetching master server list.
    /// </summary>
    UpdatingMaster,

    /// <summary>
    /// Fetched information from ALL servers from the hub.
    /// </summary>
    Updated,

    /// <summary>
    /// A connection error occured when fetching from at least one hub.
    /// </summary>
    PartialError,

    /// <summary>
    /// An error occured.
    /// </summary>
    Error,
}

public sealed record HubServerListEntry(string Address, string HubAddress, ServerApi.ServerStatus StatusData);
