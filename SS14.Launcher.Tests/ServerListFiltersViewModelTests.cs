#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.ServerStatus;
using SS14.Launcher.Utility;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ServerListFiltersViewModelTests
{
    private DataManager _dataManager = null!;
    private LocalizationManager _loc = null!;
    private ServerListFiltersViewModel _filtersVm = null!;

    [SetUp]
    public void SetUp()
    {
        _dataManager = new DataManager();
        _loc = new LocalizationManager(_dataManager);
        _loc.Initialize();
        _filtersVm = new ServerListFiltersViewModel(_dataManager, _loc);
    }

    [AvaloniaTest]
    public void TestInitialState_NoFiltersActive()
    {
        var list = new List<ServerStatusData>
        {
            new("ss14://s1:1212") { PlayerCount = 0, Status = ServerStatusCode.Online },
            new("ss14://s2:1212") { PlayerCount = 20, SoftMaxPlayerCount = 40, Status = ServerStatusCode.Online },
            new("ss14://s3:1212") { PlayerCount = 50, SoftMaxPlayerCount = 50, Status = ServerStatusCode.Online },
        };

        _filtersVm.ApplyFilters(list);
        Assert.That(list.Count, Is.EqualTo(3));
    }

    [AvaloniaTest]
    public void TestFilter_HideEmptyServers()
    {
        var list = new List<ServerStatusData>
        {
            new("ss14://empty:1212") { PlayerCount = 0, Status = ServerStatusCode.Online },
            new("ss14://active:1212") { PlayerCount = 15, Status = ServerStatusCode.Online }
        };

        _filtersVm.FilterPlayerCountHideEmpty.Selected = true;
        _filtersVm.ApplyFilters(list);

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0].Address, Is.EqualTo("ss14://active:1212"));
    }

    [AvaloniaTest]
    public void TestFilter_HideFullServers()
    {
        var list = new List<ServerStatusData>
        {
            new("ss14://full:1212") { PlayerCount = 50, SoftMaxPlayerCount = 50, Status = ServerStatusCode.Online },
            new("ss14://open:1212") { PlayerCount = 20, SoftMaxPlayerCount = 50, Status = ServerStatusCode.Online }
        };

        _filtersVm.FilterPlayerCountHideFull.Selected = true;
        _filtersVm.ApplyFilters(list);

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0].Address, Is.EqualTo("ss14://open:1212"));
    }

    [AvaloniaTest]
    public void TestFilter_PlayerCount_MinAndMax()
    {
        var list = new List<ServerStatusData>
        {
            new("ss14://low:1212") { PlayerCount = 5, Status = ServerStatusCode.Online },
            new("ss14://mid:1212") { PlayerCount = 25, Status = ServerStatusCode.Online },
            new("ss14://high:1212") { PlayerCount = 80, Status = ServerStatusCode.Online }
        };

        _filtersVm.FilterPlayerCountMinimum.Selected = true;
        _filtersVm.FilterPlayerCountMinimum.CounterValue = 10;

        _filtersVm.FilterPlayerCountMaximum.Selected = true;
        _filtersVm.FilterPlayerCountMaximum.CounterValue = 50;

        _filtersVm.ApplyFilters(list);

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0].Address, Is.EqualTo("ss14://mid:1212"));
    }

    [AvaloniaTest]
    public void TestFilter_EighteenPlus()
    {
        var list = new List<ServerStatusData>
        {
            new("ss14://adult:1212") { Tags = ["18+"], Status = ServerStatusCode.Online },
            new("ss14://general:1212") { Tags = [], Status = ServerStatusCode.Online }
        };

        // Filter for non-18+
        var filterNo18 = _filtersVm.FiltersEighteenPlus.First(f => f.Filter.Data == ServerFilter.DataFalse);
        filterNo18.Selected = true;

        _filtersVm.ApplyFilters(list);
        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0].Address, Is.EqualTo("ss14://general:1212"));
    }

    [AvaloniaTest]
    public void TestUpdatePresentFilters_DetectsTags()
    {
        var servers = new List<ServerStatusData>
        {
            new("ss14://eu:1212") { Tags = ["region:central-eu", "lang:en", "rp:med"], Status = ServerStatusCode.Online },
            new("ss14://ru:1212") { Tags = ["region:russia", "lang:ru", "rp:high"], Status = ServerStatusCode.Online }
        };

        _filtersVm.UpdatePresentFilters(servers);

        Assert.That(_filtersVm.FiltersRegion.Count, Is.GreaterThan(0));
        Assert.That(_filtersVm.FiltersLanguage.Count, Is.GreaterThan(0));
        Assert.That(_filtersVm.FiltersRolePlay.Count, Is.GreaterThan(0));
    }
}
