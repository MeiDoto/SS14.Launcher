using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Splat;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher;

public static class HappyEyeballsHttp
{
    private const int ConnectionAttemptDelay = 50;

#if DEBUG

    private const int SlowIpv6 = 0;
    private const bool BrokenIpv6 = false;

#endif

    public static HttpClient CreateHttpClient(bool autoRedirect = true)
    {
        var cfg = Locator.Current.GetService<DataManager>();
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = autoRedirect,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 20,
            EnableMultipleHttp2Connections = true,
            InitialHttp2StreamWindowSize = 2 * 1024 * 1024,
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(10)
        };

        if (cfg != null && cfg.GetCVar(CVars.ProxyEnabled) && cfg.GetCVar(CVars.ProxyApplyToLauncher))
        {
            var host = cfg.GetCVar(CVars.ProxyHost).Trim();
            if (!string.IsNullOrWhiteSpace(host))
            {
                var type = cfg.GetCVar(CVars.ProxyType).ToLowerInvariant();
                var port = cfg.GetCVar(CVars.ProxyPort);
                var user = cfg.GetCVar(CVars.ProxyUsername);
                var pass = cfg.GetCVar(CVars.ProxyPassword);
                var scheme = type.StartsWith("socks") ? "socks5" : (type.StartsWith("https") ? "https" : "http");

                var proxyUri = new Uri($"{scheme}://{host}:{port}");
                var webProxy = new WebProxy(proxyUri);
                if (!string.IsNullOrWhiteSpace(user))
                {
                    webProxy.Credentials = new NetworkCredential(user, pass);
                }
                handler.Proxy = webProxy;
                handler.UseProxy = true;
                return new HttpClient(handler);
            }
        }

        handler.ConnectCallback = OnConnect;
        return new HttpClient(handler);
    }

    private static async ValueTask<Stream> OnConnect(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        // Get IPs via DNS.
        // Note that we do not attempt to exclude IPv6 if the user doesn't have IPv6.
        // According to the docs, GetHostEntryAsync will not return them if there's no address.
        // BUT! I tested and that's a lie at least on Linux.
        // Regardless, if you don't have IPv6,
        // an attempt to connect to an IPv6 socket *should* immediately give a "network unreachable" socket error.
        // This will cause the code to immediately try the next address,
        // so IPv6 just gets "skipped over" if you don't have it.
        // I could find no other robust way to check "is there a chance in hell IPv6 works" other than "try it",
        // so... try it we will.
        var endPoint = context.DnsEndPoint;
        Log.Information("Seeking connection to {EndPoint}", endPoint);

        var resolvedAddresses = await GetIpsForHost(endPoint, cancellationToken).ConfigureAwait(false);
        if (resolvedAddresses.Length == 0)
            throw new Exception($"Host {context.DnsEndPoint.Host} resolved to no IPs!");

        // Sort as specified in the RFC, interleaving.
        var ips = SortInterleaved(resolvedAddresses);

        Debug.Assert(ips.Length > 0);

        var (socket, index) = await ParallelTask(
            ips.Length,
            (i, cancel) => AttemptConnection(i, ips[i], endPoint.Port, cancel),
            TimeSpan.FromMilliseconds(ConnectionAttemptDelay),
            cancellationToken);

        Log.Verbose("Successfully connected {EndPoint} to address: {Address}", endPoint, ips[index]);

        return new NetworkStream(socket, ownsSocket: true);
    }

    private static async Task<Socket> AttemptConnection(
        int index,
        IPAddress address,
        int port,
        CancellationToken cancel)
    {
        Log.Verbose("Trying IP {Address} for happy eyeballs [{Index}]", address, index);

        // The following socket constructor will create a dual-mode socket on systems where IPV6 is available.
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
            NoDelay = true
        };

        try
        {
#if DEBUG
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                await Task.Delay(SlowIpv6, cancel).ConfigureAwait(false);

                if (BrokenIpv6)
                    throw new Exception("Oh no I can't reach the network this is SO SAD.");
            }
#endif

            await socket.ConnectAsync(new IPEndPoint(address, port), cancel).ConfigureAwait(false);
            return socket;
        }
        catch (Exception e)
        {
            // If IPv6 is unreachable, don't print entire stacktrace.
            var exceptionToLog = e;
            if (e is SocketException { SocketErrorCode: SocketError.NetworkUnreachable }
                && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                exceptionToLog = null;
            }

            Log.Verbose(exceptionToLog, "Happy Eyeballs to {Address} [{Index}] failed", address, index);
            socket.Dispose();
            throw;
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (IPAddress[] IPs, DateTime Expires)> _dohCache = new();
    private static readonly HttpClient _dohHttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        ConnectTimeout = TimeSpan.FromSeconds(3)
    })
    {
        Timeout = TimeSpan.FromSeconds(4)
    };

    private static async Task<IPAddress[]> GetIpsForHost(DnsEndPoint endPoint, CancellationToken cancel)
    {
        var cfg = Locator.Current.GetService<DataManager>();
        var forceIpv4 = cfg != null && cfg.GetCVar(CVars.ForceIPv4);

        if (IPAddress.TryParse(endPoint.Host, out var ip))
            return [ip];

        if (cfg != null && cfg.GetCVar(CVars.DnsOverHttps))
        {
            var dohIps = await ResolveDohAsync(endPoint.Host, cancel).ConfigureAwait(false);
            if (dohIps is { Length: > 0 })
                return forceIpv4 ? FilterIpv4(dohIps) : dohIps;
        }

        var entry = await Dns.GetHostEntryAsync(endPoint.Host, cancel).ConfigureAwait(false);
        var addresses = entry.AddressList;
        return forceIpv4 ? FilterIpv4(addresses) : addresses;
    }

    private static IPAddress[] FilterIpv4(IPAddress[] ips)
    {
        var v4 = ips.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
        return v4.Length > 0 ? v4 : ips;
    }

    private static async Task<IPAddress[]?> ResolveDohAsync(string host, CancellationToken cancel)
    {
        if (_dohCache.TryGetValue(host, out var cached) && cached.Expires > DateTime.UtcNow)
            return cached.IPs;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://1.1.1.1/dns-query?name={Uri.EscapeDataString(host)}&type=A");
            req.Headers.Add("Accept", "application/dns-json");

            using var resp = await _dohHttpClient.SendAsync(req, cancel).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var doc = await System.Text.Json.JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancellationToken: cancel).ConfigureAwait(false);
                if (doc.RootElement.TryGetProperty("Answer", out var answerProp) && answerProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var ips = new List<IPAddress>();
                    foreach (var item in answerProp.EnumerateArray())
                    {
                        if (item.TryGetProperty("data", out var dataProp))
                        {
                            var ipStr = dataProp.GetString();
                            if (!string.IsNullOrEmpty(ipStr) && IPAddress.TryParse(ipStr, out var parsedIp))
                            {
                                ips.Add(parsedIp);
                            }
                        }
                    }

                    if (ips.Count > 0)
                    {
                        var ipArr = ips.ToArray();
                        _dohCache[host] = (ipArr, DateTime.UtcNow.AddMinutes(5));
                        Log.Debug("DoH resolved {Host} to {IPCount} addresses via Cloudflare 1.1.1.1", host, ipArr.Length);
                        return ipArr;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Verbose(ex, "DoH lookup failed for {Host}, falling back to standard DNS", host);
        }

        return null;
    }

    internal static IPAddress[] SortInterleaved(IPAddress[] addresses)
    {
        // Interleave returned addresses so that they are IPv6 -> IPv4 -> IPv6 -> IPv4.
        // Assuming we have multiple addresses of the same type that is.
        // As described in the RFC.

        var ipv6 = addresses.Where(x => x.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
        var ipv4 = addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray();

        var commonLength = Math.Min(ipv6.Length, ipv4.Length);

        var result = new IPAddress[addresses.Length];
        for (var i = 0; i < commonLength; i++)
        {
            result[i * 2] = ipv6[i];
            result[1 + i * 2] = ipv4[i];
        }

        if (ipv4.Length > ipv6.Length)
        {
            ipv4.AsSpan(commonLength).CopyTo(result.AsSpan(commonLength * 2));
        }
        else if (ipv6.Length > ipv4.Length)
        {
            ipv6.AsSpan(commonLength).CopyTo(result.AsSpan(commonLength * 2));
        }

        return result;
    }

    internal static async Task<(T, int)> ParallelTask<T>(
        int candidateCount,
        Func<int, CancellationToken, Task<T>> taskBuilder,
        TimeSpan delay,
        CancellationToken cancel) where T : IDisposable
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateCount);

        using var successCts = CancellationTokenSource.CreateLinkedTokenSource(cancel);

        // All tasks we have ever tried.
        var allTasks = new List<Task<T>>();
        // Tasks we are still waiting on.
        var tasks = new List<Task<T>>();

        // The general loop here is as follows:
        // 1. Add a new task for the next IP to try.
        // 2. Wait until any task completes OR the delay happens.
        // If an error occurs, we stop checking that task and continue checking the next.
        // Every iteration we add another task, until we're full on them.
        // We keep looping until we have SUCCESS, or we run out of attempt tasks entirely.

        Task<T>? successTask = null;
        while (successTask == null && (allTasks.Count < candidateCount || tasks.Count > 0))
        {
            if (allTasks.Count < candidateCount)
            {
                // We have to queue another task this iteration.
                var newTask = taskBuilder(allTasks.Count, successCts.Token);
                _ = newTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                tasks.Add(newTask);
                allTasks.Add(newTask);
            }

            var whenAnyDone = Task.WhenAny(tasks);
            Task<T> completedTask;

            if (allTasks.Count < candidateCount)
            {
                Log.Verbose("Waiting on ConnectionAttemptDelay");
                // If we have another one to queue, wait for a timeout instead of *just* waiting for a connection task.
                var timeoutTask = Task.Delay(delay, successCts.Token);
                var whenAnyOrTimeout = await Task.WhenAny(whenAnyDone, timeoutTask).ConfigureAwait(false);
                if (whenAnyOrTimeout != whenAnyDone)
                {
                    // Timeout finished. Go to next iteration so we queue another one.
                    continue;
                }

                completedTask = whenAnyDone.Result;
            }
            else
            {
                completedTask = await whenAnyDone.ConfigureAwait(false);
            }

            if (completedTask.IsCompletedSuccessfully)
            {
                // We did it. We have success.
                successTask = completedTask;
                break;
            }
            else
            {
                // Faulted. Remove it.
                tasks.Remove(completedTask);
            }
        }

        Debug.Assert(allTasks.Count > 0);

        cancel.ThrowIfCancellationRequested();
        await successCts.CancelAsync().ConfigureAwait(false);

        if (successTask == null)
        {
            // We didn't get a single successful connection. Well heck.
            throw new AggregateException(
                allTasks.Where(x => x.IsFaulted).SelectMany(x => x.Exception!.InnerExceptions));
        }

        // Observe and clean up all non-winning connection attempt tasks
        foreach (var task in allTasks)
        {
            if (task != successTask)
            {
                if (task.IsCompletedSuccessfully)
                {
                    try { task.Result.Dispose(); } catch { }
                }
                else if (task.IsFaulted)
                {
                    _ = task.Exception;
                }
                else
                {
                    _ = task.ContinueWith(t =>
                    {
                        _ = t.Exception;
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            try { t.Result.Dispose(); } catch { }
                        }
                    }, TaskContinuationOptions.ExecuteSynchronously);
                }
            }
        }

        return (successTask.Result, allTasks.IndexOf(successTask));
    }
}
