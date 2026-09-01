#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS14.Launcher.Models.ServerStatus;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ServerStatusDataTests
{
    [Test]
    public void TestConstructor_SetsAddressAndHubAddress()
    {
        var server = new ServerStatusData("ss14://server.example.com:1212", "https://hub.spacestation14.io");
        Assert.That(server.Address, Is.EqualTo("ss14://server.example.com:1212"));
        Assert.That(server.HubAddress, Is.EqualTo("https://hub.spacestation14.io"));
        Assert.That(server.Status, Is.EqualTo(ServerStatusCode.FetchingStatus));
    }

    [Test]
    public void TestPropertyChangeNotifications()
    {
        var server = new ServerStatusData("ss14://test.com:1212");
        var changedProps = new List<string>();

        server.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProps.Add(e.PropertyName);
        };

        server.Name = "Test Server";
        server.Description = "A cool test server";
        server.PlayerCount = 42;
        server.SoftMaxPlayerCount = 60;
        server.Status = ServerStatusCode.Online;
        server.Ping = TimeSpan.FromMilliseconds(45);

        Assert.That(changedProps, Contains.Item(nameof(ServerStatusData.Name)));
        Assert.That(changedProps, Contains.Item(nameof(ServerStatusData.Description)));
        Assert.That(changedProps, Contains.Item(nameof(ServerStatusData.PlayerCount)));
        Assert.That(changedProps, Contains.Item(nameof(ServerStatusData.SoftMaxPlayerCount)));
        Assert.That(changedProps, Contains.Item(nameof(ServerStatusData.Status)));
        Assert.That(changedProps, Contains.Item(nameof(ServerStatusData.Ping)));
    }
}
