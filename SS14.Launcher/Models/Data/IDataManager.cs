using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DynamicData;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.Data;

/// <summary>
/// Service interface for persistent configuration, CVars, favorites, and history storage.
/// </summary>
public interface IDataManager
{
    Guid Fingerprint { get; }
    Guid? SelectedLoginId { get; set; }
    IObservableCache<FavoriteServer, string> FavoriteServers { get; }
    IObservableCache<LoginInfo, Guid> Logins { get; }
    IObservableCache<InstalledEngineVersion, string> EngineInstallations { get; }
    IEnumerable<InstalledEngineModule> EngineModules { get; }
    ICollection<ServerFilter> Filters { get; }
    ICollection<Hub> Hubs { get; }
    bool HasCustomHubs { get; }
    bool ActuallyMultiAccounts { get; }

    T GetCVar<T>(CVarDef<T> cVar);
    void SetCVar<T>(CVarDef<T> cVar, T value);
    ICVarEntry<T> GetCVarEntry<T>(CVarDef<T> cVar);
    void ResetAllCVarsToDefault();
    Task CommitConfig();

    void AddFavoriteServer(FavoriteServer server);
    void RemoveFavoriteServer(FavoriteServer server);
    void RaiseFavoriteServer(FavoriteServer server);

    List<ServerHistoryEntry> GetServerHistory();
    void AddServerHistoryEntry(string address, string? name = null);
    void RemoveServerHistoryEntry(string address);
    void ClearServerHistory();

    Dictionary<string, long> GetServerPlaytime();
    long GetPlaytimeForServer(string address);
    void AddServerPlaytime(string address, long seconds);

    HashSet<string> GetWatchedSlotServers();
    bool IsSlotServerWatched(string address);
    void ToggleWatchedSlotServer(string address);

    bool HasAcceptedPrivacyPolicy(string privacyPolicy, [NotNullWhen(true)] out string? version);
    void AcceptPrivacyPolicy(string privacyPolicy, string version);
    void UpdateConnectedToPrivacyPolicy(string privacyPolicy);
}
