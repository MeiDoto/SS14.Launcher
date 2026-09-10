#nullable enable
using NUnit.Framework;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LauncherDiagnosticsTests
{
    [Test]
    public void TestLauncherDiagnostics_LogDiagnostics_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            LauncherDiagnostics.LogDiagnostics();
        });
    }
}
