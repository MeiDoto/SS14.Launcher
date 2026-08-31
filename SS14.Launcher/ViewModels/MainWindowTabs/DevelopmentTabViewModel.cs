using System;
using System.Diagnostics;
using System.IO;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public sealed class DevelopmentTabViewModel : MainWindowTabViewModel
{
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly DataManager _cfg = Locator.Current.GetRequiredService<DataManager>();
    private readonly IEngineManager _engineManager = Locator.Current.GetRequiredService<IEngineManager>();
    private readonly ContentManager _contentManager = Locator.Current.GetRequiredService<ContentManager>();

    private string _actionStatus = "";

    public string ActionStatus
    {
        get => _actionStatus;
        set => SetProperty(ref _actionStatus, value);
    }

    public string[] LogLevels => ["Default", "Verbose", "Debug", "Info", "Warning", "Error"];
    public string[] GraphicsBackends => ["Default", "OpenGL", "OpenGLES", "Software"];
    public string[] DisplayModes => ["Default", "Windowed", "Borderless", "Fullscreen"];

    public DevelopmentTabViewModel()
    {
        _cfg.GetCVarEntry(CVars.EngineOverrideEnabled).PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Name));
        };
        _cfg.GetCVarEntry(CVars.CustomDevelopmentTabName).PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Name));
        };
    }

    public override string Name
    {
        get
        {
            var custom = _cfg.GetCVar(CVars.CustomDevelopmentTabName);
            if (!string.IsNullOrWhiteSpace(custom))
                return custom;
            return _cfg.GetCVar(CVars.EngineOverrideEnabled)
                ? _loc.GetString("tab-development-title-override")
                : _loc.GetString("tab-development-title");
        }
    }

    public bool DisableSigning
    {
        get => _cfg.GetCVar(CVars.DisableSigning);
        set
        {
            _cfg.SetCVar(CVars.DisableSigning, value);
            _cfg.CommitConfig();
        }
    }

    public bool EngineOverrideEnabled
    {
        get => _cfg.GetCVar(CVars.EngineOverrideEnabled);
        set
        {
            _cfg.SetCVar(CVars.EngineOverrideEnabled, value);
            _cfg.CommitConfig();
        }
    }

    public string EngineOverridePath
    {
        get => _cfg.GetCVar(CVars.EngineOverridePath);
        set
        {
            _cfg.SetCVar(CVars.EngineOverridePath, value);
            _cfg.CommitConfig();
        }
    }

    public string DevCustomLaunchArguments
    {
        get => _cfg.GetCVar(CVars.DevCustomLaunchArguments);
        set
        {
            _cfg.SetCVar(CVars.DevCustomLaunchArguments, value);
            _cfg.CommitConfig();
        }
    }

    public string DevLogLevel
    {
        get => _cfg.GetCVar(CVars.DevLogLevel);
        set
        {
            _cfg.SetCVar(CVars.DevLogLevel, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevUncappedFps
    {
        get => _cfg.GetCVar(CVars.DevUncappedFps);
        set
        {
            _cfg.SetCVar(CVars.DevUncappedFps, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevGenerateCrashDumps
    {
        get => _cfg.GetCVar(CVars.DevGenerateCrashDumps);
        set
        {
            _cfg.SetCVar(CVars.DevGenerateCrashDumps, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevRenderValidation
    {
        get => _cfg.GetCVar(CVars.DevRenderValidation);
        set
        {
            _cfg.SetCVar(CVars.DevRenderValidation, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevFastThreadPool
    {
        get => _cfg.GetCVar(CVars.DevFastThreadPool);
        set
        {
            _cfg.SetCVar(CVars.DevFastThreadPool, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevAggressiveLohTrim
    {
        get => _cfg.GetCVar(CVars.DevAggressiveLohTrim);
        set
        {
            _cfg.SetCVar(CVars.DevAggressiveLohTrim, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevStrictDiagnostics
    {
        get => _cfg.GetCVar(CVars.DevStrictDiagnostics);
        set
        {
            _cfg.SetCVar(CVars.DevStrictDiagnostics, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevTieredPgo
    {
        get => _cfg.GetCVar(CVars.DevTieredPgo);
        set
        {
            _cfg.SetCVar(CVars.DevTieredPgo, value);
            _cfg.CommitConfig();
        }
    }

    public int DevGcHeapLimitMb
    {
        get => _cfg.GetCVar(CVars.DevGcHeapLimitMb);
        set
        {
            _cfg.SetCVar(CVars.DevGcHeapLimitMb, Math.Max(0, value));
            _cfg.CommitConfig();
        }
    }

    public string DevGraphicsBackend
    {
        get => _cfg.GetCVar(CVars.DevGraphicsBackend);
        set
        {
            _cfg.SetCVar(CVars.DevGraphicsBackend, value);
            _cfg.CommitConfig();
        }
    }

    public string DevDisplayMode
    {
        get => _cfg.GetCVar(CVars.DevDisplayMode);
        set
        {
            _cfg.SetCVar(CVars.DevDisplayMode, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevDebugFpsOverlay
    {
        get => _cfg.GetCVar(CVars.DevDebugFpsOverlay);
        set
        {
            _cfg.SetCVar(CVars.DevDebugFpsOverlay, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevDebugNetGraph
    {
        get => _cfg.GetCVar(CVars.DevDebugNetGraph);
        set
        {
            _cfg.SetCVar(CVars.DevDebugNetGraph, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevOpenConsoleOnStart
    {
        get => _cfg.GetCVar(CVars.DevOpenConsoleOnStart);
        set
        {
            _cfg.SetCVar(CVars.DevOpenConsoleOnStart, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevPhysicsDebug
    {
        get => _cfg.GetCVar(CVars.DevPhysicsDebug);
        set
        {
            _cfg.SetCVar(CVars.DevPhysicsDebug, value);
            _cfg.CommitConfig();
        }
    }

    public string DevCustomEnvVars
    {
        get => _cfg.GetCVar(CVars.DevCustomEnvVars);
        set
        {
            _cfg.SetCVar(CVars.DevCustomEnvVars, value);
            _cfg.CommitConfig();
        }
    }

    public int DevAudioBufferSize
    {
        get => _cfg.GetCVar(CVars.DevAudioBufferSize);
        set
        {
            _cfg.SetCVar(CVars.DevAudioBufferSize, value);
            _cfg.CommitConfig();
        }
    }

    public int DevSimulatedPingMs
    {
        get => _cfg.GetCVar(CVars.DevSimulatedPingMs);
        set
        {
            _cfg.SetCVar(CVars.DevSimulatedPingMs, Math.Clamp(value, 0, 2000));
            _cfg.CommitConfig();
        }
    }

    public int DevSimulatedJitterMs
    {
        get => _cfg.GetCVar(CVars.DevSimulatedJitterMs);
        set
        {
            _cfg.SetCVar(CVars.DevSimulatedJitterMs, Math.Clamp(value, 0, 500));
            _cfg.CommitConfig();
        }
    }

    public int DevSimulatedPacketLoss
    {
        get => _cfg.GetCVar(CVars.DevSimulatedPacketLoss);
        set
        {
            _cfg.SetCVar(CVars.DevSimulatedPacketLoss, Math.Clamp(value, 0, 100));
            _cfg.CommitConfig();
        }
    }

    public bool DevMuteAudio
    {
        get => _cfg.GetCVar(CVars.DevMuteAudio);
        set
        {
            _cfg.SetCVar(CVars.DevMuteAudio, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevShowLightMap
    {
        get => _cfg.GetCVar(CVars.DevShowLightMap);
        set
        {
            _cfg.SetCVar(CVars.DevShowLightMap, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevShowEntityBounds
    {
        get => _cfg.GetCVar(CVars.DevShowEntityBounds);
        set
        {
            _cfg.SetCVar(CVars.DevShowEntityBounds, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevDisableNetCompression
    {
        get => _cfg.GetCVar(CVars.DevDisableNetCompression);
        set
        {
            _cfg.SetCVar(CVars.DevDisableNetCompression, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevGCNoAffinitize
    {
        get => _cfg.GetCVar(CVars.DevGCNoAffinitize);
        set
        {
            _cfg.SetCVar(CVars.DevGCNoAffinitize, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevForceScalarSearch
    {
        get => _cfg.GetCVar(CVars.DevForceScalarSearch);
        set
        {
            _cfg.SetCVar(CVars.DevForceScalarSearch, value);
            _cfg.CommitConfig();
        }
    }

    public bool HighProcessPriority
    {
        get => _cfg.GetCVar(CVars.HighProcessPriority);
        set
        {
            _cfg.SetCVar(CVars.HighProcessPriority, value);
            _cfg.CommitConfig();
        }
    }

    public bool ForceDedicatedGpu
    {
        get => _cfg.GetCVar(CVars.ForceDedicatedGpu);
        set
        {
            _cfg.SetCVar(CVars.ForceDedicatedGpu, value);
            _cfg.CommitConfig();
        }
    }

    public bool MaxPerformanceJit
    {
        get => _cfg.GetCVar(CVars.MaxPerformanceJit);
        set
        {
            _cfg.SetCVar(CVars.MaxPerformanceJit, value);
            _cfg.CommitConfig();
        }
    }

    public bool LowLatencyNetworking
    {
        get => _cfg.GetCVar(CVars.LowLatencyNetworking);
        set
        {
            _cfg.SetCVar(CVars.LowLatencyNetworking, value);
            _cfg.CommitConfig();
        }
    }

    private string _benchmarkResultsText = "";
    public string BenchmarkResultsText
    {
        get => _benchmarkResultsText;
        set => SetProperty(ref _benchmarkResultsText, value);
    }

    private string _diagnosticsReportText = "";
    public string DiagnosticsReportText
    {
        get => _diagnosticsReportText;
        set => SetProperty(ref _diagnosticsReportText, value);
    }

    private string _networkReportText = "";
    public string NetworkReportText
    {
        get => _networkReportText;
        set => SetProperty(ref _networkReportText, value);
    }

    public async void RunAlgorithmBenchmarkAsync()
    {
        BenchmarkResultsText = "Running benchmark (20,000 iterations)...";
        var result = await System.Threading.Tasks.Task.Run(() =>
        {
            const int iterations = 20_000;
            const string strA = "Space Station 14 Official Corvax Sandbox Roleplay";
            const string strB = "Station 14 Official Corvax Sandbox Roleplay Server";

            // 1. Myers vs Damerau-Levenshtein
            var sw = Stopwatch.StartNew();
            int sink = 0;
            for (int i = 0; i < iterations; i++)
            {
                sink += AdvancedAlgorithms.MyersBitParallelDistance(strA.AsSpan(), strB.AsSpan());
            }
            sw.Stop();
            var myersMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                sink += AdvancedAlgorithms.DamerauLevenshteinDistance(strA.AsSpan(), strB.AsSpan());
            }
            sw.Stop();
            var dlMs = sw.Elapsed.TotalMilliseconds;

            // 2. SIMD ToLower vs Scalar ToLower
            Span<char> destSimd = stackalloc char[strA.Length];
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                AdvancedAlgorithms.SimdStringHelper.ToLowerAsciiSimd(strA.AsSpan(), destSimd);
            }
            sw.Stop();
            var simdMs = sw.Elapsed.TotalMilliseconds;

            Span<char> destScalar = stackalloc char[strA.Length];
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < strA.Length; j++)
                {
                    char c = strA[j];
                    destScalar[j] = (c >= 'A' && c <= 'Z') ? (char)(c + 32) : c;
                }
            }
            sw.Stop();
            var scalarMs = sw.Elapsed.TotalMilliseconds;

            var speedupMyers = dlMs / Math.Max(0.001, myersMs);
            var speedupSimd = scalarMs / Math.Max(0.001, simdMs);

            return $"⚡ Benchmark ({iterations:N0} ops):\n" +
                   $"• Myers Bit-Vector: {myersMs:F2} ms (Damerau-Lev: {dlMs:F2} ms) -> {speedupMyers:F1}x faster\n" +
                   $"• SIMD String Normalize: {simdMs:F2} ms (Scalar: {scalarMs:F2} ms) -> {speedupSimd:F1}x faster\n" +
                   $"• Vector256 Accelerated: {System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated}, Vector128: {System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated}";
        });

        BenchmarkResultsText = result;
    }

    public void RunSystemDiagnostics()
    {
        var procCount = Environment.ProcessorCount;
        var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        var v256 = System.Runtime.Intrinsics.Vector256.IsHardwareAccelerated;
        var v128 = System.Runtime.Intrinsics.Vector128.IsHardwareAccelerated;
        var isServerGc = System.Runtime.GCSettings.IsServerGC;
        var latencyMode = System.Runtime.GCSettings.LatencyMode;
        var gcMemMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

        DiagnosticsReportText = $"💻 System & Runtime:\n" +
                                $"• OS: {os} ({arch})\n" +
                                $"• Runtime: {framework}\n" +
                                $"• CPU Threads: {procCount}\n" +
                                $"• Hardware SIMD: Vector256={v256}, Vector128={v128}\n" +
                                $"• GC Mode: {(isServerGc ? "Server (Multi-threaded)" : "Workstation")}, Latency: {latencyMode}\n" +
                                $"• GC Memory: {gcMemMb:F2} MB | Portable: {LauncherPaths.IsPortable}";
    }

    public async void RunNetworkDiagnosticsAsync()
    {
        NetworkReportText = "Testing network latency to central hubs...";
        var report = await System.Threading.Tasks.Task.Run(async () =>
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("🌐 Network Diagnostics:");

            var targets = new (string Name, string Host, int Port)[]
            {
                ("Central Hub", "central.spacestation14.io", 443),
                ("Robust CDN", "robust-builds.cdn.spacestation14.com", 443),
                ("GitHub API", "api.github.com", 443),
                ("Cloudflare DNS", "1.1.1.1", 53),
                ("Google DNS", "8.8.8.8", 53)
            };

            foreach (var (name, host, port) in targets)
            {
                try
                {
                    using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp) { NoDelay = true };
                    var sw = Stopwatch.StartNew();
                    using var cts = new System.Threading.CancellationTokenSource(2000);
                    await socket.ConnectAsync(host, port, cts.Token);
                    sw.Stop();
                    results.AppendLine($"• {name} ({host}): {sw.ElapsedMilliseconds} ms [OK]");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"• {name} ({host}): Failed ({ex.GetType().Name})");
                }
            }

            return results.ToString().TrimEnd();
        });

        NetworkReportText = report;
    }

    public void ForceGarbageCollection()
    {
        var beforeMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var afterMb = GC.GetTotalMemory(true) / (1024.0 * 1024.0);
        ActionStatus = $"🧹 GC: {beforeMb:F2} MB -> {afterMb:F2} MB (Freed {(beforeMb - afterMb):F2} MB, Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)})";
    }

    public void ClearAllInstalledEngines()
    {
        try
        {
            _engineManager.ClearAllEngines();
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-cleared-engines");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-engines", ("error", e.Message));
        }
    }

    public async void ClearAllInstalledServers()
    {
        try
        {
            _engineManager.ClearAllEngines();
            await _contentManager.ClearAll();
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-cleared-content");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-content", ("error", e.Message));
        }
    }

    public void ClearLogs()
    {
        try
        {
            if (Directory.Exists(LauncherPaths.DirLogs))
            {
                foreach (var file in Directory.GetFiles(LauncherPaths.DirLogs))
                {
                    try { File.Delete(file); } catch { }
                }
            }
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-cleared-logs");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-logs", ("error", e.Message));
        }
    }

    public void OpenUserDataFolder()
    {
        try
        {
            Directory.CreateDirectory(LauncherPaths.DirUserData);
            Process.Start(new ProcessStartInfo
            {
                FileName = LauncherPaths.DirUserData,
                UseShellExecute = true
            });
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-opened-user-data");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-folder", ("error", e.Message));
        }
    }

    public void OpenLogsFolder()
    {
        try
        {
            Directory.CreateDirectory(LauncherPaths.DirLogs);
            Process.Start(new ProcessStartInfo
            {
                FileName = LauncherPaths.DirLogs,
                UseShellExecute = true
            });
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-opened-logs");
        }
        catch (Exception e)
        {
            ActionStatus = LocalizationManager.Instance.GetString("tab-dev-error-logs-folder", ("error", e.Message));
        }
    }

    public void ClearServerPlaytime()
    {
        try
        {
            _cfg.SetCVar(CVars.ServerPlaytime, "{}");
            _cfg.CommitConfig();
            ActionStatus = _loc.GetString("tab-dev-cleared-playtime");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }

    public void ClearWatchedSlots()
    {
        try
        {
            _cfg.SetCVar(CVars.WatchedSlotServers, "[]");
            _cfg.CommitConfig();
            ActionStatus = _loc.GetString("tab-dev-cleared-slots");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }

    public void ClearNewsCache()
    {
        try
        {
            var file = Path.Combine(LauncherPaths.DirLocalData, "news_cache.json");
            if (File.Exists(file))
                File.Delete(file);
            ActionStatus = _loc.GetString("tab-dev-cleared-news-cache");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }

    public void ResetAllCVarsToDefaults()
    {
        try
        {
            _cfg.ResetAllCVarsToDefault();
            ActionStatus = _loc.GetString("tab-dev-cleared-cvars");
        }
        catch (Exception e)
        {
            ActionStatus = _loc.GetString("tab-dev-error-action", ("error", e.Message));
        }
    }
}
