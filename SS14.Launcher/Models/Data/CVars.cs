using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.Data;

/// <summary>
/// Contains definitions for all launcher configuration values.
/// </summary>
/// <remarks>
/// The fields of this class are automatically searched for all CVar definitions.
/// </remarks>
/// <see cref="DataManager"/>
[UsedImplicitly]
public static class CVars
{
    /// <summary>
    /// Default to using compatibility options for rendering etc,
    /// that are less likely to immediately crash on buggy drivers.
    /// </summary>
    public static readonly CVarDef<bool> CompatMode = CVarDef.Create("CompatMode", false);

    /// <summary>
    /// On first launch, the launcher tells you that SS14 is EARLY ACCESS.
    /// This stores whether they dismissed that, though people will insist on pretending it defaults to true.
    /// </summary>
    public static readonly CVarDef<bool> HasDismissedEarlyAccessWarning
        = CVarDef.Create("HasDismissedEarlyAccessWarning", false);

    /// <summary>
    /// Used to warn users about the degradation of the Intel 13th and 14th generation CPUs
    /// This has proven multiple times to cause issues with game startup due to some memory access issue after enough degradation.
    /// <see href="https://www.reddit.com/r/intel/comments/1egthzw/megathread_for_intel_core_13th_14th_gen_cpu/"/>
    /// </summary>
    public static readonly CVarDef<bool> HasDismissedIntelDegradation
        = CVarDef.Create("HasDismissedIntelDegradation", false);

    /// <summary>
    /// Used to warn Apple Silicon users who are running the game under Rosetta 2 when they could be running the native build.
    /// </summary>
    public static readonly CVarDef<bool> HasDismissedRosettaWarning
        = CVarDef.Create("HasDismissedRosettaWarning", false);

    /// <summary>
    /// Disable checking engine build signatures when launching game.
    /// Only enable if you know what you're doing.
    /// </summary>
    /// <remarks>
    /// This is ignored on release builds, for security reasons.
    /// </remarks>
    public static readonly CVarDef<bool> DisableSigning = CVarDef.Create("DisableSigning", false);

    /// <summary>
    /// Enable local overriding of engine versions.
    /// </summary>
    /// <remarks>
    /// If enabled and on a development build,
    /// the launcher will pull all engine versions and modules from <see cref="EngineOverridePath"/>.
    /// This can be set to <c>RobustToolbox/release/</c> to instantly pull in packaged engine builds.
    /// </remarks>
    public static readonly CVarDef<bool> EngineOverrideEnabled = CVarDef.Create("EngineOverrideEnabled", false);

    /// <summary>
    /// Path to load engines from when using <see cref="EngineOverrideEnabled"/>.
    /// </summary>
    public static readonly CVarDef<string> EngineOverridePath = CVarDef.Create("EngineOverridePath", "");

    /// <summary>
    /// Verbose logging of launcher logs.
    /// </summary>
    public static readonly CVarDef<bool> LogLauncherVerbose = CVarDef.Create("LogLauncherVerbose", false);

    /// <summary>
    /// Enable multi-account support on release builds.
    /// </summary>
    public static readonly CVarDef<bool> MultiAccounts = CVarDef.Create("MultiAccounts", false);

    /// <summary>
    /// Enable high performance Tiered PGO runtime optimizations.
    /// </summary>
    public static readonly CVarDef<bool> EnableTieredPGO = CVarDef.Create("EnableTieredPGO", true);

    /// <summary>
    /// Force Server Garbage Collector for lower frame lag on multi-core systems.
    /// </summary>
    public static readonly CVarDef<bool> ForceServerGC = CVarDef.Create("ForceServerGC", true);

    /// <summary>
    /// Enable fast real-time TCP ping measurement in server list.
    /// </summary>
    public static readonly CVarDef<bool> EnableFastPing = CVarDef.Create("EnableFastPing", true);

    /// <summary>
    /// Custom background image path for the launcher.
    /// </summary>
    public static readonly CVarDef<string> CustomBackgroundImagePath = CVarDef.Create("CustomBackgroundImagePath", "");

    /// <summary>
    /// Custom background overlay opacity (0.1 to 1.0).
    /// </summary>
    public static readonly CVarDef<float> CustomBackgroundOpacity = CVarDef.Create("CustomBackgroundOpacity", 0.85f);

    /// <summary>
    /// Custom logo image path for the launcher.
    /// </summary>
    public static readonly CVarDef<string> CustomLogoImagePath = CVarDef.Create("CustomLogoImagePath", "");

    /// <summary>
    /// Custom accent color (Hex string like #ADA24B).
    /// </summary>
    public static readonly CVarDef<string> CustomAccentColor = CVarDef.Create("CustomAccentColor", "");

    /// <summary>
    /// Custom tab name for Home tab.
    /// </summary>
    public static readonly CVarDef<string> CustomHomeTabName = CVarDef.Create("CustomHomeTabName", "");

    /// <summary>
    /// Custom tab name for Servers tab.
    /// </summary>
    public static readonly CVarDef<string> CustomServersTabName = CVarDef.Create("CustomServersTabName", "");

    /// <summary>
    /// Custom tab name for Replays tab.
    /// </summary>
    public static readonly CVarDef<string> CustomReplaysTabName = CVarDef.Create("CustomReplaysTabName", "");

    /// <summary>
    /// Placement / Docking position of main launcher tabs (Top, Bottom, Left, Right).
    /// </summary>
    public static readonly CVarDef<string> CustomTabPlacement = CVarDef.Create("CustomTabPlacement", "Top");

    /// <summary>
    /// Custom ordering of main launcher tabs.
    /// </summary>
    public static readonly CVarDef<string> CustomTabOrder = CVarDef.Create("CustomTabOrder", "");

    /// <summary>
    /// Show or hide the Replays tab.
    /// </summary>
    public static readonly CVarDef<bool> ShowReplaysTab = CVarDef.Create("ShowReplaysTab", false);

    /// <summary>
    /// Custom button background color (Hex string like #464966).
    /// </summary>
    public static readonly CVarDef<string> CustomButtonColor = CVarDef.Create("CustomButtonColor", "");

    /// <summary>
    /// Custom tab selected color (Hex string like #3E6C45).
    /// </summary>
    public static readonly CVarDef<string> CustomTabSelectedColor = CVarDef.Create("CustomTabSelectedColor", "");

    /// <summary>
    /// Custom header background color (Hex string like #212126).
    /// </summary>
    public static readonly CVarDef<string> CustomHeaderColor = CVarDef.Create("CustomHeaderColor", "");

    /// <summary>
    /// Enable tactile VFX click animations on buttons.
    /// </summary>
    public static readonly CVarDef<bool> EnableClickVfx = CVarDef.Create("EnableClickVfx", true);

    /// <summary>
    /// Custom user CSS/XAML styling script code.
    /// </summary>
    public static readonly CVarDef<string> CustomUserCode = CVarDef.Create("CustomUserCode", "");

    /// <summary>
    /// Custom text color (Hex string like #EEEEEE).
    /// </summary>
    public static readonly CVarDef<string> CustomTextColor = CVarDef.Create("CustomTextColor", "");

    /// <summary>
    /// Custom connect button color override (Hex string like #464966).
    /// </summary>
    public static readonly CVarDef<string> CustomConnectButtonColor = CVarDef.Create("CustomConnectButtonColor", "");

    /// <summary>
    /// Custom popup and card background color (Hex string like #202025).
    /// </summary>
    public static readonly CVarDef<string> CustomPopupBackgroundColor = CVarDef.Create("CustomPopupBackgroundColor", "");

    /// <summary>
    /// Custom UI font size in points (12 to 22).
    /// </summary>
    public static readonly CVarDef<float> CustomFontSize = CVarDef.Create("CustomFontSize", 15.0f);

    /// <summary>
    /// Custom window title for launcher.
    /// </summary>
    public static readonly CVarDef<string> CustomWindowTitle = CVarDef.Create("CustomWindowTitle", "");

    /// <summary>
    /// Custom tab name for Options tab.
    /// </summary>
    public static readonly CVarDef<string> CustomOptionsTabName = CVarDef.Create("CustomOptionsTabName", "");

    /// <summary>
    /// Currently selected login in the drop down.
    /// </summary>
    public static readonly CVarDef<string> SelectedLogin = CVarDef.Create("SelectedLogin", "");

    public static readonly CVarDef<string> Fingerprint = CVarDef.Create("Fingerprint", "");

    /// <summary>
    /// Maximum amount of TOTAL versions to keep in the content database.
    /// </summary>
    public static readonly CVarDef<int> MaxVersionsToKeep = CVarDef.Create("MaxVersionsToKeep", 15);

    /// <summary>
    /// Maximum amount of versions to keep of a specific fork ID.
    /// </summary>
    public static readonly CVarDef<int> MaxForkVersionsToKeep = CVarDef.Create("MaxForkVersionsToKeep", 3);

     /// <summary>
    /// If a download gets interrupted, keep the files for a week.
    /// </summary>
    public static readonly CVarDef<int> InterruptibleDownloadKeepHours = CVarDef.Create("InterruptibleDownloadKeepHours", 7 * 24);

    /// <summary>
    /// Whether to display override assets.
    /// </summary>
    public static readonly CVarDef<bool> OverrideAssets = CVarDef.Create("OverrideAssets", false);

    /// <summary>
    /// Stores the minimum player count value used by the "minimum player count" filter.
    /// </summary>
    /// <seealso cref="ServerFilter.PlayerCountMin"/>
    public static readonly CVarDef<int> FilterPlayerCountMinValue = CVarDef.Create("FilterPlayerCountMinValue", 0);

    /// <summary>
    /// Stores the maximum player count value used by the "maximum player count" filter.
    /// </summary>
    /// <seealso cref="ServerFilter.PlayerCountMax"/>
    public static readonly CVarDef<int> FilterPlayerCountMaxValue = CVarDef.Create("FilterPlayerCountMaxValue", 0);

    /// <summary>
    /// Stores whether the user has seen the Wine warning.
    /// </summary>
    public static readonly CVarDef<bool> WineWarningShown = CVarDef.Create("WineWarningShown", false);

    /// <summary>
    /// Language the user selected. Null means it should be automatically selected based on system language.
    /// </summary>
    public static readonly CVarDef<string?> Language = CVarDef.Create<string?>("Language", null);

    /// <summary>
    /// The CPU architecture this launcher was last run with.
    public static readonly CVarDef<int> CurrentArchitecture = CVarDef.Create("CurrentArchitecture", (int) Architecture.X64);

    public static readonly CVarDef<bool> HighProcessPriority = CVarDef.Create("HighProcessPriority", false);
    public static readonly CVarDef<bool> ForceDedicatedGpu = CVarDef.Create("ForceDedicatedGpu", false);
    public static readonly CVarDef<bool> MaxPerformanceJit = CVarDef.Create("MaxPerformanceJit", false);
    public static readonly CVarDef<bool> LowLatencyNetworking = CVarDef.Create("LowLatencyNetworking", false);
    public static readonly CVarDef<bool> DisableDiagnosticsOverhead = CVarDef.Create("DisableDiagnosticsOverhead", false);
    public static readonly CVarDef<bool> LowPauseGc = CVarDef.Create("LowPauseGc", false);
    public static readonly CVarDef<bool> SmartCacheCleaner = CVarDef.Create("SmartCacheCleaner", false);
    public static readonly CVarDef<bool> FastLaunchPreload = CVarDef.Create("FastLaunchPreload", false);
    public static readonly CVarDef<bool> DnsOverHttps = CVarDef.Create("DnsOverHttps", false);
    public static readonly CVarDef<string> LocalBuilds = CVarDef.Create("LocalBuilds", "[]");
    public static readonly CVarDef<string> ServerHistory = CVarDef.Create("ServerHistory", "[]");
    public static readonly CVarDef<bool> ProxyEnabled = CVarDef.Create("ProxyEnabled", false);
    public static readonly CVarDef<string> ProxyType = CVarDef.Create("ProxyType", "SOCKS5");
    public static readonly CVarDef<string> ProxyHost = CVarDef.Create("ProxyHost", "127.0.0.1");
    public static readonly CVarDef<int> ProxyPort = CVarDef.Create("ProxyPort", 1080);
    public static readonly CVarDef<string> ProxyUsername = CVarDef.Create("ProxyUsername", "");
    public static readonly CVarDef<string> ProxyPassword = CVarDef.Create("ProxyPassword", "");
    public static readonly CVarDef<bool> ProxyApplyToGameClient = CVarDef.Create("ProxyApplyToGameClient", true);
    public static readonly CVarDef<bool> ProxyApplyToLauncher = CVarDef.Create("ProxyApplyToLauncher", false);

    public static readonly CVarDef<string> DevCustomLaunchArguments = CVarDef.Create("DevCustomLaunchArguments", "");
    public static readonly CVarDef<string> DevLogLevel = CVarDef.Create("DevLogLevel", "Default");
    public static readonly CVarDef<bool> DevUncappedFps = CVarDef.Create("DevUncappedFps", false);
    public static readonly CVarDef<bool> DevGenerateCrashDumps = CVarDef.Create("DevGenerateCrashDumps", false);
    public static readonly CVarDef<bool> DevRenderValidation = CVarDef.Create("DevRenderValidation", false);
    public static readonly CVarDef<int> DevSimulatedPingMs = CVarDef.Create("DevSimulatedPingMs", 0);
    public static readonly CVarDef<int> DevSimulatedPacketLoss = CVarDef.Create("DevSimulatedPacketLoss", 0);

    public static readonly CVarDef<bool> DevTieredPgo = CVarDef.Create("DevTieredPgo", false);
    public static readonly CVarDef<int> DevGcHeapLimitMb = CVarDef.Create("DevGcHeapLimitMb", 0);
    public static readonly CVarDef<string> DevGraphicsBackend = CVarDef.Create("DevGraphicsBackend", "Default");
    public static readonly CVarDef<string> DevDisplayMode = CVarDef.Create("DevDisplayMode", "Default");
    public static readonly CVarDef<bool> DevDebugFpsOverlay = CVarDef.Create("DevDebugFpsOverlay", false);
    public static readonly CVarDef<bool> DevDebugNetGraph = CVarDef.Create("DevDebugNetGraph", false);
    public static readonly CVarDef<bool> DevOpenConsoleOnStart = CVarDef.Create("DevOpenConsoleOnStart", false);
    public static readonly CVarDef<bool> DevPhysicsDebug = CVarDef.Create("DevPhysicsDebug", false);
    public static readonly CVarDef<string> DevCustomEnvVars = CVarDef.Create("DevCustomEnvVars", "");
    public static readonly CVarDef<int> DevAudioBufferSize = CVarDef.Create("DevAudioBufferSize", 0);
    public static readonly CVarDef<bool> DevFastThreadPool = CVarDef.Create("DevFastThreadPool", false);
    public static readonly CVarDef<bool> DevAggressiveLohTrim = CVarDef.Create("DevAggressiveLohTrim", false);
    public static readonly CVarDef<bool> DevStrictDiagnostics = CVarDef.Create("DevStrictDiagnostics", false);
    public static readonly CVarDef<int> DevSimulatedJitterMs = CVarDef.Create("DevSimulatedJitterMs", 0);
    public static readonly CVarDef<bool> DevMuteAudio = CVarDef.Create("DevMuteAudio", false);
    public static readonly CVarDef<bool> DevShowLightMap = CVarDef.Create("DevShowLightMap", false);
    public static readonly CVarDef<bool> DevShowEntityBounds = CVarDef.Create("DevShowEntityBounds", false);
    public static readonly CVarDef<bool> DevDisableNetCompression = CVarDef.Create("DevDisableNetCompression", false);
    public static readonly CVarDef<bool> DevGCNoAffinitize = CVarDef.Create("DevGCNoAffinitize", false);
    public static readonly CVarDef<bool> DevForceScalarSearch = CVarDef.Create("DevForceScalarSearch", false);
    public static readonly CVarDef<bool> ShowDevelopmentTab = CVarDef.Create("ShowDevelopmentTab", false);
    public static readonly CVarDef<bool> ShowNewsTab = CVarDef.Create("ShowNewsTab", false);
    public static readonly CVarDef<string> CustomNewsTabName = CVarDef.Create("CustomNewsTabName", "");
    public static readonly CVarDef<string> CustomDevelopmentTabName = CVarDef.Create("CustomDevelopmentTabName", "");

    /// <summary>
    /// Version tag that user chose to skip (e.g. "v1.1.3"). Resets when a newer version appears.
    /// </summary>
    public static readonly CVarDef<string> SkippedUpdateVersion = CVarDef.Create("SkippedUpdateVersion", "");

    /// <summary>
    /// Enable tracking of playtime per server and total playtime. Default is false.
    /// </summary>
    public static readonly CVarDef<bool> TrackPlaytime = CVarDef.Create("TrackPlaytime", false);

    /// <summary>
    /// Serialized JSON dictionary of server address -> played seconds.
    /// </summary>
    public static readonly CVarDef<string> ServerPlaytime = CVarDef.Create("ServerPlaytime", "{}");

    /// <summary>
    /// Enable desktop notifications when a monitored full server has available player slots. Default is false.
    /// </summary>
    public static readonly CVarDef<bool> EnableSlotNotifier = CVarDef.Create("EnableSlotNotifier", false);

    /// <summary>
    /// Serialized JSON list of server addresses being monitored for available player slots.
    /// </summary>
    public static readonly CVarDef<string> WatchedSlotServers = CVarDef.Create("WatchedSlotServers", "[]");

    /// <summary>
    /// Enable high performance HTTP response compression (Zstd, Brotli, GZip) for hub requests and game assets. Default is true.
    /// </summary>
    public static readonly CVarDef<bool> EnableHttpCompression = CVarDef.Create("EnableHttpCompression", true);

    /// <summary>
    /// Enable Discord Rich Presence (RPC) activity status. Default is true.
    /// </summary>
    public static readonly CVarDef<bool> DiscordRpcEnabled = CVarDef.Create("DiscordRpcEnabled", true);

    /// <summary>
    /// Force IPv4 only DNS resolution and socket connections (bypasses broken IPv6 routes/blocks). Default is false.
    /// </summary>
    public static readonly CVarDef<bool> ForceIPv4 = CVarDef.Create("ForceIPv4", false);

    /// <summary>
    /// Fast aggressive fallback between primary and fallback CDN/Hub endpoints (1s instead of 3s). Default is true.
    /// </summary>
    public static readonly CVarDef<bool> FastHubFallback = CVarDef.Create("FastHubFallback", true);

    /// <summary>
    /// Network connection timeout in seconds per attempt. Default is 6 seconds.
    /// </summary>
    public static readonly CVarDef<int> NetworkTimeout = CVarDef.Create("NetworkTimeout", 6);
}

/// <summary>
/// Base definition of a CVar.
/// </summary>
/// <seealso cref="DataManager"/>
/// <seealso cref="CVars"/>
public abstract class CVarDef
{
    public string Name { get; }
    public object? DefaultValue { get; }
    public Type ValueType { get; }

    private protected CVarDef(string name, object? defaultValue, Type type)
    {
        Name = name;
        DefaultValue = defaultValue;
        ValueType = type;
    }

    public static CVarDef<T> Create<T>(
        string name,
        T defaultValue)
    {
        return new CVarDef<T>(name, defaultValue);
    }
}

/// <summary>
/// Generic specialized definition of CVar definition.
/// </summary>
/// <typeparam name="T">The type of value stored in this CVar.</typeparam>
public sealed class CVarDef<T> : CVarDef
{
    public new T DefaultValue { get; }

    internal CVarDef(string name, T defaultValue) : base(name, defaultValue, typeof(T))
    {
        DefaultValue = defaultValue;
    }
}
