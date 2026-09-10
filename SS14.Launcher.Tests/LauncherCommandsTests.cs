#nullable enable
using System;
using NUnit.Framework;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LauncherCommandsTests
{
    [Test]
    public void TestLauncherCommands_Constants()
    {
        Assert.That(LauncherCommands.PingCommand, Is.EqualTo(":Ping"));
        Assert.That(LauncherCommands.RedialWaitCommand, Is.EqualTo(":RedialWait"));
        Assert.That(LauncherCommands.BlankReasonCommand, Is.EqualTo("r"));
    }

    [Test]
    public void TestLauncherCommands_ConstructConnectCommand()
    {
        var uri = new Uri("ss14://server.spacestation14.com:1212");
        var cmd = LauncherCommands.ConstructConnectCommand(uri);

        Assert.That(cmd, Is.EqualTo("css14://server.spacestation14.com:1212/"));
        Assert.That(cmd.StartsWith("c"), Is.True);
    }
}
