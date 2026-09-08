using System;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LauncherTimeoutsTests
{
    [Test]
    public void TestTimeoutsArePositiveAndNonZero()
    {
        Assert.That(LauncherTimeouts.ServerStatusTimeout.TotalMilliseconds, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.FastPingSocketTimeoutMs, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.StandardPingSocketTimeoutMs, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.NetworkDiagnosticsTimeout.TotalMilliseconds, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.SearchDebounce.TotalMilliseconds, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.ReplaysWatcherDebounce.TotalMilliseconds, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.HttpRetryBaseDelay.TotalMilliseconds, Is.GreaterThan(0));
        Assert.That(LauncherTimeouts.MaxDownloadRetries, Is.GreaterThan(0));
    }

    [Test]
    public void TestFastPingIsShorterThanStandardPing()
    {
        Assert.That(LauncherTimeouts.FastPingSocketTimeoutMs, Is.LessThan(LauncherTimeouts.StandardPingSocketTimeoutMs));
    }

    [Test]
    public void TestDebounceRangesAreSensibleForUi()
    {
        Assert.That(LauncherTimeouts.SearchDebounce.TotalMilliseconds, Is.InRange(50, 1000));
        Assert.That(LauncherTimeouts.ReplaysWatcherDebounce.TotalMilliseconds, Is.InRange(100, 2000));
    }
}
