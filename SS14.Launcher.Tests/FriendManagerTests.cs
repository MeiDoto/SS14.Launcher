#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.Friends;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class FriendManagerTests
{
    private FriendManager _manager = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (Locator.Current.GetService<DataManager>() == null)
        {
            Locator.CurrentMutable.RegisterConstant(new DataManager());
        }
    }

    [SetUp]
    public void SetUp()
    {
        _manager = FriendManager.Instance;
        _manager.Load();
        // Clean out existing friends
        foreach (var friend in _manager.GetAllFriends().ToList())
        {
            _manager.RemoveFriend(friend.Username);
        }
    }

    [Test]
    public void AddAndRetrieveFriend()
    {
        var result = _manager.AddOrUpdateFriend("CaptainJack", "Good captain", "ss14://wizard.org:1212");
        Assert.That(result, Is.True);

        Assert.That(_manager.TryGetFriend("CaptainJack", out var friend), Is.True);
        Assert.That(friend, Is.Not.Null);
        Assert.That(friend!.Username, Is.EqualTo("CaptainJack"));
        Assert.That(friend.Note, Is.EqualTo("Good captain"));
        Assert.That(friend.FavoriteServerAddress, Is.EqualTo("ss14://wizard.org:1212"));
    }

    [Test]
    public void CaseInsensitiveLookup()
    {
        _manager.AddOrUpdateFriend("HonkGuy", "The mime friend");

        Assert.That(_manager.TryGetFriend("honkguy", out var lowerFriend), Is.True);
        Assert.That(lowerFriend, Is.Not.Null);
        Assert.That(lowerFriend!.Username, Is.EqualTo("HonkGuy"));

        Assert.That(_manager.TryGetFriend("HONKGUY", out var upperFriend), Is.True);
        Assert.That(upperFriend, Is.Not.Null);
    }

    [Test]
    public void UpdateExistingFriend()
    {
        _manager.AddOrUpdateFriend("Bob", "Old Note", "ss14://old:1212");
        _manager.AddOrUpdateFriend("Bob", "New Note", "ss14://new:1212");

        var all = _manager.GetAllFriends();
        Assert.That(all.Count, Is.EqualTo(1));
        Assert.That(all[0].Username, Is.EqualTo("Bob"));
        Assert.That(all[0].Note, Is.EqualTo("New Note"));
        Assert.That(all[0].FavoriteServerAddress, Is.EqualTo("ss14://new:1212"));
    }

    [Test]
    public void RemoveFriend()
    {
        _manager.AddOrUpdateFriend("Alice");
        Assert.That(_manager.GetAllFriends().Count, Is.EqualTo(1));

        var removed = _manager.RemoveFriend("Alice");
        Assert.That(removed, Is.True);
        Assert.That(_manager.GetAllFriends().Count, Is.EqualTo(0));
        Assert.That(_manager.TryGetFriend("Alice", out _), Is.False);
    }

    [Test]
    public void ServerPresenceQuery()
    {
        _manager.AddOrUpdateFriend("Friend1", favoriteServer: "ss14://server-a:1212");
        _manager.AddOrUpdateFriend("Friend2", favoriteServer: "ss14://server-b:1212");

        Assert.That(_manager.HasFriendsOnServer("ss14://server-a:1212"), Is.True);
        Assert.That(_manager.HasFriendsOnServer("ss14://server-b:1212"), Is.True);
        Assert.That(_manager.HasFriendsOnServer("ss14://server-c:1212"), Is.False);

        var onServerA = _manager.GetFriendsOnServer("ss14://server-a:1212");
        Assert.That(onServerA.Count, Is.EqualTo(1));
        Assert.That(onServerA[0].Username, Is.EqualTo("Friend1"));
    }

    [Test]
    public void RecordFriendSeen()
    {
        _manager.AddOrUpdateFriend("Clown");
        _manager.RecordFriendSeen("Clown", "ss14://circus:1212", "Circus Station");

        Assert.That(_manager.TryGetFriend("Clown", out var friend), Is.True);
        Assert.That(friend, Is.Not.Null);
        Assert.That(friend!.LastSeenServerAddress, Is.EqualTo("ss14://circus:1212"));
        Assert.That(friend.LastSeenServerName, Is.EqualTo("Circus Station"));
        Assert.That(friend.LastSeenTime, Is.Not.Null);

        Assert.That(_manager.HasFriendsOnServer("ss14://circus:1212"), Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void EmptyOrWhitespaceUsername_Ignored(string? invalidName)
    {
        var added = _manager.AddOrUpdateFriend(invalidName!);
        Assert.That(added, Is.False);
        Assert.That(_manager.GetAllFriends().Count, Is.EqualTo(0));
    }
}
