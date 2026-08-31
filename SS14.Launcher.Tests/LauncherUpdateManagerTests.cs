using System;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LauncherUpdateManagerTests
{
    [TestCase("1.1.9", 1, 1, 9)]
    [TestCase("v1.2.0", 1, 2, 0)]
    [TestCase("V2.0.4", 2, 0, 4)]
    [TestCase("1.0.0-rc1", 1, 0, 0)]
    [TestCase("v1.1.8-hotfix2", 1, 1, 8)]
    [TestCase("2.5", 2, 5, 0)]
    public void TestTryParseVersion_Valid(string input, int major, int minor, int build)
    {
        var success = LauncherUpdateManager.TryParseVersion(input, out var ver);
        Assert.That(success, Is.True);
        Assert.That(ver.Major, Is.EqualTo(major));
        Assert.That(ver.Minor, Is.EqualTo(minor));
        if (build > 0 || ver.Build >= 0)
        {
            Assert.That(ver.Build, Is.EqualTo(build));
        }
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not_a_version")]
    [TestCase("vabc.def")]
    public void TestTryParseVersion_Invalid(string input)
    {
        var success = LauncherUpdateManager.TryParseVersion(input, out _);
        Assert.That(success, Is.False);
    }

    [Test]
    public void TestVersionComparison_Ordering()
    {
        LauncherUpdateManager.TryParseVersion("1.1.8", out var v118);
        LauncherUpdateManager.TryParseVersion("1.1.9", out var v119);
        LauncherUpdateManager.TryParseVersion("v1.2.0", out var v120);

        Assert.That(v119, Is.GreaterThan(v118));
        Assert.That(v120, Is.GreaterThan(v119));
        Assert.That(v118, Is.LessThan(v120));
    }
}
