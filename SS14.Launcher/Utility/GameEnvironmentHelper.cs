using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Utility;

/// <summary>
/// Helper class responsible for configuring process environment variables, GPU offloading,
/// .NET runtime optimizations, and OS process priority when launching game client sessions.
/// </summary>
public static class GameEnvironmentHelper
{
    /// <summary>
    /// Injects configured environment variables (PGO, JIT, GC, GPU, Proxy) into the process start info.
    /// </summary>
    public static void ConfigureEnvironment(ProcessStartInfo startInfo, DataManager cfg)
    {
        void EnvVar(string key, string val)
        {
            startInfo.EnvironmentVariables[key] = val;
        }

        EnvVar("SS14_LAUNCHER_PATH", Environment.ProcessPath ?? AppContext.BaseDirectory);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            EnvVar("SS14_LOG_CLIENT", LauncherPaths.PathClientMacLog);
        }

        // Performance tweaks: Tiered PGO
        if (cfg.GetCVar(CVars.EnableTieredPGO))
        {
            EnvVar("DOTNET_TieredPGO", "1");
            EnvVar("DOTNET_ReadyToRun", "0");
            EnvVar("DOTNET_TC_QuickJitForLoops", "1");
            EnvVar("DOTNET_ThreadPool_UnfairSemaphore", "1");
            EnvVar("DOTNET_EnableWriteXorExecute", "0");
        }

        // Performance tweaks: Max JIT optimizations
        if (cfg.GetCVar(CVars.MaxPerformanceJit))
        {
            EnvVar("DOTNET_EnableHWIntrinsic", "1");
            EnvVar("DOTNET_JITMinOpts", "0");
            EnvVar("DOTNET_GCConserveMemory", "0");
            EnvVar("DOTNET_GCHeapCount", Math.Min(Environment.ProcessorCount, 8).ToString());
        }

        // Low latency networking
        if (cfg.GetCVar(CVars.LowLatencyNetworking))
        {
            EnvVar("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS", "1");
        }

        // Disable profiling / diagnostic overhead
        if (cfg.GetCVar(CVars.DisableDiagnosticsOverhead))
        {
            EnvVar("DOTNET_EnableDiagnostics", "0");
        }

        // Low-pause GC
        if (cfg.GetCVar(CVars.LowPauseGc))
        {
            EnvVar("DOTNET_GCConcurrent", "1");
            EnvVar("DOTNET_GCLargeObjectHeapCompactionMode", "1");
        }

        // Force Dedicated GPU
        if (cfg.GetCVar(CVars.ForceDedicatedGpu))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                EnvVar("DRI_PRIME", "1");
                EnvVar("__NV_PRIME_RENDER_OFFLOAD", "1");

                if (File.Exists("/dev/nvidia0") || Directory.Exists("/proc/driver/nvidia"))
                {
                    EnvVar("__GLX_VENDOR_LIBRARY_NAME", "nvidia");
                    EnvVar("__VK_LAYER_NV_optimus", "NVIDIA_only");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnvVar("SHIM_MCCOMPAT", "0x000000001");
                EnvVar("GPU_MAX_ALLOC_PERCENT", "100");
                EnvVar("GPU_USE_SYNC_OBJECTS", "1");
                EnvVar("NV_SWAP_INTERVAL", "0");
            }
        }

        // Proxy injection
        if (cfg.GetCVar(CVars.ProxyEnabled) && cfg.GetCVar(CVars.ProxyApplyToGameClient))
        {
            var type = cfg.GetCVar(CVars.ProxyType).ToLowerInvariant();
            var host = cfg.GetCVar(CVars.ProxyHost);
            var port = cfg.GetCVar(CVars.ProxyPort);
            var user = cfg.GetCVar(CVars.ProxyUsername);
            var pass = cfg.GetCVar(CVars.ProxyPassword);

            if (!string.IsNullOrWhiteSpace(host))
            {
                var scheme = type.StartsWith("socks") ? "socks5" : (type.StartsWith("https") ? "https" : "http");
                string proxyUrl;
                if (!string.IsNullOrWhiteSpace(user))
                {
                    proxyUrl = $"{scheme}://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(pass)}@{host}:{port}";
                }
                else
                {
                    proxyUrl = $"{scheme}://{host}:{port}";
                }

                EnvVar("HTTP_PROXY", proxyUrl);
                EnvVar("HTTPS_PROXY", proxyUrl);
                EnvVar("ALL_PROXY", proxyUrl);
                EnvVar("http_proxy", proxyUrl);
                EnvVar("https_proxy", proxyUrl);
                EnvVar("all_proxy", proxyUrl);
            }
        }
    }

    /// <summary>
    /// Elevates process priority if configured.
    /// </summary>
    public static void ApplyProcessPriority(Process proc, DataManager cfg)
    {
        if (cfg.GetCVar(CVars.HighProcessPriority))
        {
            try
            {
                proc.PriorityClass = ProcessPriorityClass.High;
                Log.Debug("Elevated game process priority to High");
            }
            catch (Exception ex)
            {
                Log.Warning("Could not elevate game process priority: {Message}", ex.Message);
            }
        }
    }
}
