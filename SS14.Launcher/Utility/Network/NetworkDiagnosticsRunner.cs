using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Utility.Network;

public enum DiagnosticsStepStatus
{
    Pending,
    Running,
    Success,
    Warning,
    Failed
}

public sealed class DiagnosticsStep
{
    public string Name { get; }
    public DiagnosticsStepStatus Status { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string Details { get; set; } = "";

    public DiagnosticsStep(string name)
    {
        Name = name;
        Status = DiagnosticsStepStatus.Pending;
    }
}

public sealed class DiagnosticsReport
{
    public string TargetUri { get; init; } = "";
    public string Host { get; init; } = "";
    public int Port { get; init; }
    public bool IsSecure { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public List<DiagnosticsStep> Steps { get; init; } = new();

    public double MinPingMs { get; set; }
    public double MaxPingMs { get; set; }
    public double AvgPingMs { get; set; }
    public double JitterMs { get; set; }
    public double PacketLossPercent { get; set; }

    public bool IsSuccess => Steps.All(s => s.Status != DiagnosticsStepStatus.Failed);

    public string GenerateFormattedReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== SS14 Launcher Network Diagnostic Report ===");
        sb.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Target: {TargetUri} ({Host}:{Port})");
        sb.AppendLine($"TLS Enabled: {IsSecure}");
        sb.AppendLine("------------------------------------------------");
        foreach (var step in Steps)
        {
            var statusIcon = step.Status switch
            {
                DiagnosticsStepStatus.Success => "[OK]",
                DiagnosticsStepStatus.Warning => "[WARN]",
                DiagnosticsStepStatus.Failed => "[FAIL]",
                _ => "[INFO]"
            };
            sb.AppendLine($"{statusIcon} {step.Name}: {step.ElapsedMilliseconds} ms - {step.Details}");
        }
        sb.AppendLine("------------------------------------------------");
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Latency: Min={MinPingMs:F1}ms, Max={MaxPingMs:F1}ms, Avg={AvgPingMs:F1}ms"));
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Jitter: {JitterMs:F1}ms | Packet Loss: {PacketLossPercent:F0}%"));
        sb.AppendLine("================================================");
        return sb.ToString();
    }
}

public sealed class NetworkDiagnosticsRunner
{
    public static (string Host, int Port, bool IsSecure) ParseTarget(string target)
    {
        var trimmed = target.Trim();
        if (trimmed.StartsWith("ss14s://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(trimmed);
            return (uri.Host, uri.Port > 0 ? uri.Port : 1212, true);
        }

        if (trimmed.StartsWith("ss14://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(trimmed);
            return (uri.Host, uri.Port > 0 ? uri.Port : 1212, false);
        }

        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(trimmed);
            return (uri.Host, uri.Port > 0 ? uri.Port : 443, true);
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(trimmed);
            return (uri.Host, uri.Port > 0 ? uri.Port : 80, false);
        }

        var parts = trimmed.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 1212;
        return (host, port, false);
    }

    public async Task<DiagnosticsReport> RunDiagnosticsAsync(
        string target,
        Action<DiagnosticsStep>? onStepUpdated = null,
        CancellationToken cancel = default)
    {
        var (host, port, isSecure) = ParseTarget(target);
        var report = new DiagnosticsReport
        {
            TargetUri = target,
            Host = host,
            Port = port,
            IsSecure = isSecure
        };

        // Step 1: DNS Resolution
        var dnsStep = new DiagnosticsStep("DNS Resolution");
        report.Steps.Add(dnsStep);
        IPAddress[] ipAddresses = Array.Empty<IPAddress>();

        try
        {
            dnsStep.Status = DiagnosticsStepStatus.Running;
            onStepUpdated?.Invoke(dnsStep);

            var sw = Stopwatch.StartNew();
            ipAddresses = await Dns.GetHostAddressesAsync(host, cancel);
            sw.Stop();

            dnsStep.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            if (ipAddresses.Length > 0)
            {
                var ips = string.Join(", ", ipAddresses.Select(ip => ip.ToString()));
                dnsStep.Details = $"Resolved {ipAddresses.Length} IP(s): {ips}";
                dnsStep.Status = DiagnosticsStepStatus.Success;
            }
            else
            {
                dnsStep.Details = "No IP addresses resolved";
                dnsStep.Status = DiagnosticsStepStatus.Failed;
            }
        }
        catch (Exception ex)
        {
            dnsStep.Status = DiagnosticsStepStatus.Failed;
            dnsStep.Details = $"DNS resolution failed: {ex.Message}";
            Log.Warning(ex, "Network diagnostics DNS resolution failed for {Host}", host);
        }
        onStepUpdated?.Invoke(dnsStep);

        if (dnsStep.Status == DiagnosticsStepStatus.Failed || ipAddresses.Length == 0)
            return report;

        var targetIp = ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork) ?? ipAddresses[0];

        // Step 2: TCP Connection Handshake
        var tcpStep = new DiagnosticsStep("TCP Socket Handshake");
        report.Steps.Add(tcpStep);
        bool tcpSuccess = false;

        try
        {
            tcpStep.Status = DiagnosticsStepStatus.Running;
            onStepUpdated?.Invoke(tcpStep);

            var sw = Stopwatch.StartNew();
            using var socket = new Socket(targetIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 5000;
            socket.SendTimeout = 5000;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            cts.CancelAfter(5000);

            await socket.ConnectAsync(targetIp, port, cts.Token);
            sw.Stop();

            tcpStep.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            tcpStep.Details = $"Connected to {targetIp}:{port} in {sw.ElapsedMilliseconds} ms";
            tcpStep.Status = sw.ElapsedMilliseconds > 300 ? DiagnosticsStepStatus.Warning : DiagnosticsStepStatus.Success;
            tcpSuccess = true;
        }
        catch (Exception ex)
        {
            tcpStep.Status = DiagnosticsStepStatus.Failed;
            tcpStep.Details = $"Connection refused or timed out: {ex.Message}";
            Log.Warning(ex, "Network diagnostics TCP connection failed for {TargetIp}:{Port}", targetIp, port);
        }
        onStepUpdated?.Invoke(tcpStep);

        if (!tcpSuccess)
            return report;

        // Step 3: TLS Handshake (if secure)
        if (isSecure)
        {
            var tlsStep = new DiagnosticsStep("TLS Handshake & Certificate Verification");
            report.Steps.Add(tlsStep);

            try
            {
                tlsStep.Status = DiagnosticsStepStatus.Running;
                onStepUpdated?.Invoke(tlsStep);

                var sw = Stopwatch.StartNew();
                using var tcpClient = new TcpClient(targetIp.AddressFamily);
                await tcpClient.ConnectAsync(targetIp, port, cancel);

                using var sslStream = new SslStream(tcpClient.GetStream(), false, (sender, cert, chain, sslPolicyErrors) =>
                {
                    return sslPolicyErrors == SslPolicyErrors.None;
                });

                var sslOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                };

                await sslStream.AuthenticateAsClientAsync(sslOptions, cancel);
                sw.Stop();

                tlsStep.ElapsedMilliseconds = sw.ElapsedMilliseconds;
                tlsStep.Details = $"Protocol: {sslStream.SslProtocol}, CipherSuite: {sslStream.NegotiatedCipherSuite}";
                tlsStep.Status = DiagnosticsStepStatus.Success;
            }
            catch (Exception ex)
            {
                tlsStep.Status = DiagnosticsStepStatus.Failed;
                tlsStep.Details = $"TLS handshake failed: {ex.Message}";
                Log.Warning(ex, "Network diagnostics TLS handshake failed for {Host}:{Port}", host, port);
            }
            onStepUpdated?.Invoke(tlsStep);
        }

        // Step 4: Multi-sample Ping & Jitter Analysis
        var pingStep = new DiagnosticsStep("Ping Latency & Jitter (5 samples)");
        report.Steps.Add(pingStep);

        try
        {
            pingStep.Status = DiagnosticsStepStatus.Running;
            onStepUpdated?.Invoke(pingStep);

            var sampleTimes = new List<double>();
            int packetsLost = 0;
            const int sampleCount = 5;

            for (int i = 0; i < sampleCount; i++)
            {
                if (cancel.IsCancellationRequested)
                    break;

                try
                {
                    var sw = Stopwatch.StartNew();
                    using var pingSocket = new Socket(targetIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
                    cts.CancelAfter(3000);

                    await pingSocket.ConnectAsync(targetIp, port, cts.Token);
                    sw.Stop();

                    sampleTimes.Add(sw.Elapsed.TotalMilliseconds);
                }
                catch (Exception)
                {
                    packetsLost++;
                }

                await Task.Delay(100, cancel);
            }

            if (sampleTimes.Count > 0)
            {
                report.MinPingMs = sampleTimes.Min();
                report.MaxPingMs = sampleTimes.Max();
                report.AvgPingMs = sampleTimes.Average();

                // Compute Jitter: mean difference between consecutive latency samples
                double totalDiff = 0;
                for (int i = 1; i < sampleTimes.Count; i++)
                {
                    totalDiff += Math.Abs(sampleTimes[i] - sampleTimes[i - 1]);
                }
                report.JitterMs = sampleTimes.Count > 1 ? totalDiff / (sampleTimes.Count - 1) : 0;
                report.PacketLossPercent = (packetsLost / (double)sampleCount) * 100.0;

                pingStep.ElapsedMilliseconds = (long)report.AvgPingMs;
                pingStep.Details = string.Create(CultureInfo.InvariantCulture, $"Avg: {report.AvgPingMs:F1} ms (Min: {report.MinPingMs:F1}, Max: {report.MaxPingMs:F1}), Jitter: {report.JitterMs:F1} ms, Loss: {report.PacketLossPercent:F0}%");
                pingStep.Status = (report.PacketLossPercent > 0 || report.AvgPingMs > 250)
                    ? DiagnosticsStepStatus.Warning
                    : DiagnosticsStepStatus.Success;
            }
            else
            {
                pingStep.Details = "All ping probes failed";
                pingStep.Status = DiagnosticsStepStatus.Failed;
                report.PacketLossPercent = 100.0;
            }
        }
        catch (Exception ex)
        {
            pingStep.Status = DiagnosticsStepStatus.Failed;
            pingStep.Details = $"Ping measurement error: {ex.Message}";
        }
        onStepUpdated?.Invoke(pingStep);

        return report;
    }
}
