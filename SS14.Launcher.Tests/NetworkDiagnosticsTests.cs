using NUnit.Framework;
using SS14.Launcher.Utility.Network;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class NetworkDiagnosticsTests
{
    [TestCase("ss14s://central.spacestation14.io:1212", "central.spacestation14.io", 1212, true)]
    [TestCase("ss14s://game.example.com", "game.example.com", 1212, true)]
    [TestCase("ss14://localhost:25000", "localhost", 25000, false)]
    [TestCase("ss14://127.0.0.1", "127.0.0.1", 1212, false)]
    [TestCase("https://hub.spacestation14.io", "hub.spacestation14.io", 443, true)]
    [TestCase("http://hub.example.com:8080", "hub.example.com", 8080, false)]
    [TestCase("custom.server.org:9999", "custom.server.org", 9999, false)]
    [TestCase("simplehost", "simplehost", 1212, false)]
    public void TestParseTarget(string input, string expectedHost, int expectedPort, bool expectedSecure)
    {
        var (host, port, isSecure) = NetworkDiagnosticsRunner.ParseTarget(input);
        Assert.That(host, Is.EqualTo(expectedHost));
        Assert.That(port, Is.EqualTo(expectedPort));
        Assert.That(isSecure, Is.EqualTo(expectedSecure));
    }

    [Test]
    public void TestDiagnosticsReportFormatting()
    {
        var report = new DiagnosticsReport
        {
            TargetUri = "ss14s://test.server:1212",
            Host = "test.server",
            Port = 1212,
            IsSecure = true,
            AvgPingMs = 45.2,
            MinPingMs = 38.0,
            MaxPingMs = 52.1,
            JitterMs = 3.5,
            PacketLossPercent = 0
        };

        report.Steps.Add(new DiagnosticsStep("DNS Resolution")
        {
            Status = DiagnosticsStepStatus.Success,
            ElapsedMilliseconds = 12,
            Details = "Resolved 127.0.0.1"
        });

        report.Steps.Add(new DiagnosticsStep("TCP Socket Handshake")
        {
            Status = DiagnosticsStepStatus.Success,
            ElapsedMilliseconds = 25,
            Details = "Connected"
        });

        var text = report.GenerateFormattedReport();

        Assert.That(text, Does.Contain("SS14 Launcher Network Diagnostic Report"));
        Assert.That(text, Does.Contain("Target: ss14s://test.server:1212"));
        Assert.That(text, Does.Contain("[OK] DNS Resolution: 12 ms - Resolved 127.0.0.1"));
        Assert.That(text, Does.Contain("Latency: Min=38.0ms, Max=52.1ms, Avg=45.2ms"));
        Assert.That(text, Does.Contain("Jitter: 3.5ms | Packet Loss: 0%"));
        Assert.That(report.IsSuccess, Is.True);
    }
}
