#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class DataManagerUnitTests
{
    private DataManager _dataManager = null!;

    [SetUp]
    public void SetUp()
    {
        _dataManager = new DataManager();
    }

    [Test]
    public void TestCVars_StringOperations()
    {
        var initial = _dataManager.GetCVar(CVars.CustomWindowTitle);
        Assert.That(initial, Is.EqualTo(""));

        _dataManager.SetCVar(CVars.CustomWindowTitle, "Custom Launcher Title");
        Assert.That(_dataManager.GetCVar(CVars.CustomWindowTitle), Is.EqualTo("Custom Launcher Title"));

        var entry = _dataManager.GetCVarEntry(CVars.CustomWindowTitle);
        Assert.That(entry.Value, Is.EqualTo("Custom Launcher Title"));

        bool changed = false;
        entry.PropertyChanged += (_, _) => changed = true;
        entry.Value = "Updated Title";

        Assert.That(changed, Is.True);
        Assert.That(_dataManager.GetCVar(CVars.CustomWindowTitle), Is.EqualTo("Updated Title"));
    }

    [Test]
    public void TestCVars_IntegerOperations()
    {
        _dataManager.SetCVar(CVars.MaxVersionsToKeep, 42);
        Assert.That(_dataManager.GetCVar(CVars.MaxVersionsToKeep), Is.EqualTo(42));

        var entry = _dataManager.GetCVarEntry(CVars.MaxVersionsToKeep);
        Assert.That(entry.Value, Is.EqualTo(42));

        entry.Value = 99;
        Assert.That(_dataManager.GetCVar(CVars.MaxVersionsToKeep), Is.EqualTo(99));
    }

    [Test]
    public void TestCVars_BooleanOperations()
    {
        _dataManager.SetCVar(CVars.DisableSigning, true);
        Assert.That(_dataManager.GetCVar(CVars.DisableSigning), Is.True);

        _dataManager.SetCVar(CVars.DisableSigning, false);
        Assert.That(_dataManager.GetCVar(CVars.DisableSigning), Is.False);
    }

    [Test]
    public void TestCVars_ResetAllToDefault()
    {
        _dataManager.SetCVar(CVars.MaxVersionsToKeep, 999);
        _dataManager.SetCVar(CVars.DisableSigning, true);

        _dataManager.ResetAllCVarsToDefault();

        Assert.That(_dataManager.GetCVar(CVars.MaxVersionsToKeep), Is.EqualTo(CVars.MaxVersionsToKeep.DefaultValue));
        Assert.That(_dataManager.GetCVar(CVars.DisableSigning), Is.EqualTo(CVars.DisableSigning.DefaultValue));
    }

    [Test]
    public void TestFavoriteServers_AddAndRemove()
    {
        var fav = new FavoriteServer("Honk Station", "ss14://honk.example.com:1212");
        _dataManager.AddFavoriteServer(fav);

        Assert.That(_dataManager.FavoriteServers.Count, Is.EqualTo(1));
        Assert.That(_dataManager.FavoriteServers.Lookup("ss14://honk.example.com:1212").HasValue, Is.True);

        // Duplicates should throw ArgumentException
        Assert.Throws<ArgumentException>(() => _dataManager.AddFavoriteServer(new FavoriteServer("Duplicate", "ss14://honk.example.com:1212")));

        _dataManager.RemoveFavoriteServer(fav);
        Assert.That(_dataManager.FavoriteServers.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestHubs_CollectionOperations()
    {
        var hub = new Hub(new Uri("https://hub.spacestation14.com/"), 0);
        _dataManager.Hubs.Add(hub);

        Assert.That(_dataManager.Hubs.Count, Is.EqualTo(1));
        Assert.That(_dataManager.Hubs.Contains(hub), Is.True);
        Assert.That(_dataManager.HasCustomHubs, Is.True);

        var copy = new Hub[1];
        _dataManager.Hubs.CopyTo(copy, 0);
        Assert.That(copy[0], Is.EqualTo(hub));

        var removed = _dataManager.Hubs.Remove(hub);
        Assert.That(removed, Is.True);
        Assert.That(_dataManager.Hubs.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestServerFilters_CollectionOperations()
    {
        var filter = new ServerFilter(ServerFilterCategory.Language, "ru");
        _dataManager.Filters.Add(filter);

        Assert.That(_dataManager.Filters.Count, Is.EqualTo(1));
        Assert.That(_dataManager.Filters.Contains(filter), Is.True);

        var array = new ServerFilter[1];
        _dataManager.Filters.CopyTo(array, 0);
        Assert.That(array[0], Is.EqualTo(filter));

        Assert.That(_dataManager.Filters.Remove(filter), Is.True);
        Assert.That(_dataManager.Filters.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestServerHistory_AddAndClear()
    {
        _dataManager.AddServerHistoryEntry("ss14://history1.example.com", "Server 1");
        _dataManager.AddServerHistoryEntry("ss14://history2.example.com", "Server 2");

        var history = _dataManager.GetServerHistory();
        Assert.That(history.Count, Is.EqualTo(2));
        Assert.That(history.Any(h => h.Address == "ss14://history1.example.com"), Is.True);

        _dataManager.RemoveServerHistoryEntry("ss14://history1.example.com");
        Assert.That(_dataManager.GetServerHistory().Count, Is.EqualTo(1));

        _dataManager.ClearServerHistory();
        Assert.That(_dataManager.GetServerHistory().Count, Is.EqualTo(0));
    }

    [Test]
    public void TestServerPlaytime_Tracking()
    {
        Assert.That(_dataManager.GetPlaytimeForServer("ss14://play.example.com"), Is.EqualTo(0));

        _dataManager.AddServerPlaytime("ss14://play.example.com", 3600);
        Assert.That(_dataManager.GetPlaytimeForServer("ss14://play.example.com"), Is.EqualTo(3600));

        _dataManager.AddServerPlaytime("ss14://play.example.com", 1800);
        Assert.That(_dataManager.GetPlaytimeForServer("ss14://play.example.com"), Is.EqualTo(5400));

        var playtimes = _dataManager.GetServerPlaytime();
        Assert.That(playtimes.ContainsKey("ss14://play.example.com"), Is.True);
        Assert.That(playtimes["ss14://play.example.com"], Is.EqualTo(5400));
    }

    [Test]
    public void TestWatchedSlotServers_Toggle()
    {
        Assert.That(_dataManager.IsSlotServerWatched("ss14://slot.example.com"), Is.False);

        _dataManager.ToggleWatchedSlotServer("ss14://slot.example.com");
        Assert.That(_dataManager.IsSlotServerWatched("ss14://slot.example.com"), Is.True);

        _dataManager.ToggleWatchedSlotServer("ss14://slot.example.com");
        Assert.That(_dataManager.IsSlotServerWatched("ss14://slot.example.com"), Is.False);
    }

    [Test]
    public void TestPrivacyPolicy_Acceptance()
    {
        Assert.That(_dataManager.HasAcceptedPrivacyPolicy("wizard_privacy", out var version), Is.False);
        Assert.That(version, Is.Null);

        _dataManager.AcceptPrivacyPolicy("wizard_privacy", "2.0");
        Assert.That(_dataManager.HasAcceptedPrivacyPolicy("wizard_privacy", out var acceptedVersion), Is.True);
        Assert.That(acceptedVersion, Is.EqualTo("2.0"));
    }
}
