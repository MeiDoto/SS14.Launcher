using System;

namespace SS14.Launcher.Utility;

/// <summary>
/// Centralized constants for all network, socket, debounce, and process timeouts.
/// Eliminates magic numbers across network, ping, and UI layers.
/// </summary>
public static class LauncherTimeouts
{
    /// <summary>
    /// Default timeout for checking server status / info.
    /// </summary>
    public static readonly TimeSpan ServerStatusTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Aggressive fast socket connect timeout for latency probing in ms.
    /// </summary>
    public const int FastPingSocketTimeoutMs = 600;

    /// <summary>
    /// Standard socket connect timeout for latency probing in ms.
    /// </summary>
    public const int StandardPingSocketTimeoutMs = 1200;

    /// <summary>
    /// Timeout for network diagnostics TCP ping to hub endpoints.
    /// </summary>
    public static readonly TimeSpan NetworkDiagnosticsTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Debounce duration for server list search typing.
    /// </summary>
    public static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Debounce duration for replays filesystem watcher.
    /// </summary>
    public static readonly TimeSpan ReplaysWatcherDebounce = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Delay between HTTP download retry attempts with exponential backoff.
    /// </summary>
    public static readonly TimeSpan HttpRetryBaseDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Maximum HTTP download retry attempts.
    /// </summary>
    public const int MaxDownloadRetries = 4;
}
