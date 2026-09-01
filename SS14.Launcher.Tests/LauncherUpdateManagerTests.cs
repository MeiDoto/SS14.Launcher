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

    [Test]
    public void TestExtractSha256FromBody()
    {
        var body = @"# Release Notes
* SS14.Launcher_Windows.zip: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
* SS14.Launcher_Linux.tar.gz: ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb";

        var winSha = LauncherUpdateManager.ExtractSha256FromBody(body, "SS14.Launcher_Windows.zip");
        var linuxSha = LauncherUpdateManager.ExtractSha256FromBody(body, "SS14.Launcher_Linux.tar.gz");
        var unknownSha = LauncherUpdateManager.ExtractSha256FromBody(body, "SS14.Launcher_macOS.zip");

        Assert.That(winSha, Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
        Assert.That(linuxSha, Is.EqualTo("ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb"));
        Assert.That(unknownSha, Is.Null);
    }

    [Test]
    public void TestParseChecksumFromSha256Sums()
    {
        var shaSumsContent = @"e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  SS14.Launcher_Windows.zip
ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb  SS14.Launcher_Linux.tar.gz";

        var winSha = LauncherUpdateManager.ParseChecksumFromSha256Sums(shaSumsContent, "SS14.Launcher_Windows.zip");
        var linuxSha = LauncherUpdateManager.ParseChecksumFromSha256Sums(shaSumsContent, "SS14.Launcher_Linux.tar.gz");
        var unknown = LauncherUpdateManager.ParseChecksumFromSha256Sums(shaSumsContent, "SS14.Launcher_macOS.zip");

        Assert.That(winSha, Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
        Assert.That(linuxSha, Is.EqualTo("ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb"));
        Assert.That(unknown, Is.Null);
    }
}

