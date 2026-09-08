using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// Diagnostics, benchmarking and network testing functionality for the Development tab.
/// </summary>
public sealed partial class DevelopmentTabViewModel
{
    private string _benchmarkResultsText = "";

    /// <summary>
    /// Gets or sets the text output from the algorithm benchmark run.
    /// </summary>
    public string BenchmarkResultsText
    {
        get => _benchmarkResultsText;
        set => SetProperty(ref _benchmarkResultsText, value);
    }

    private string _diagnosticsReportText = "";

    /// <summary>
    /// Gets or sets the text output from the system diagnostics report.
    /// </summary>
    public string DiagnosticsReportText
    {
        get => _diagnosticsReportText;
        set => SetProperty(ref _diagnosticsReportText, value);
    }

    private string _networkReportText = "";

    /// <summary>
    /// Gets or sets the text output from the network diagnostics report.
    /// </summary>
    public string NetworkReportText
    {
        get => _networkReportText;
        set => SetProperty(ref _networkReportText, value);
    }

    /// <summary>
    /// Runs a performance benchmark comparing Myers bit-parallel vs Damerau-Levenshtein distance
    /// algorithms and SIMD vs scalar string normalization.
    /// </summary>
    public async Task RunAlgorithmBenchmarkAsync()
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

    /// <summary>
    /// Collects and displays system information: OS, runtime, CPU, SIMD capabilities, and GC stats.
    /// </summary>
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

    /// <summary>
    /// Runs TCP connectivity tests to central hub servers, CDN, and DNS endpoints
    /// to diagnose network issues.
    /// </summary>
    public async Task RunNetworkDiagnosticsAsync()
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

    /// <summary>
    /// Forces a full GC collection (Gen2, compacting) and reports memory freed.
    /// </summary>
    public void ForceGarbageCollection()
    {
        var beforeMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var afterMb = GC.GetTotalMemory(true) / (1024.0 * 1024.0);
        ActionStatus = $"🧹 GC: {beforeMb:F2} MB -> {afterMb:F2} MB (Freed {(beforeMb - afterMb):F2} MB, Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)})";
    }
}
