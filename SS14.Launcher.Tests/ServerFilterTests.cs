using System;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ServerFilterTests
{
    [Test]
    public void TestServerFilter_RecordEquality()
    {
        var filterA = new ServerFilter(ServerFilterCategory.Language, "ru");
        var filterB = new ServerFilter(ServerFilterCategory.Language, "ru");
        var filterC = new ServerFilter(ServerFilterCategory.Language, "en");
        var filterD = new ServerFilter(ServerFilterCategory.Region, "ru");

        Assert.That(filterA, Is.EqualTo(filterB));
        Assert.That(filterA.GetHashCode(), Is.EqualTo(filterB.GetHashCode()));
        Assert.That(filterA, Is.Not.EqualTo(filterC));
        Assert.That(filterA, Is.Not.EqualTo(filterD));
    }

    [Test]
    public void TestServerFilter_StaticConstants()
    {
        Assert.That(ServerFilter.PlayerCountHideEmpty.Category, Is.EqualTo(ServerFilterCategory.PlayerCount));
        Assert.That(ServerFilter.PlayerCountHideEmpty.Data, Is.EqualTo("hide_empty"));

        Assert.That(ServerFilter.PlayerCountHideFull.Category, Is.EqualTo(ServerFilterCategory.PlayerCount));
        Assert.That(ServerFilter.PlayerCountHideFull.Data, Is.EqualTo("hide_full"));

        Assert.That(ServerFilter.Recommended.Category, Is.EqualTo(ServerFilterCategory.Recommended));
        Assert.That(ServerFilter.Recommended.Data, Is.EqualTo(ServerFilter.DataTrue));
    }

    [Test]
    public void TestServerFilter_CategoriesExist()
    {
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.Language), Is.True);
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.Region), Is.True);
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.RolePlay), Is.True);
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.EighteenPlus), Is.True);
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.PlayerCount), Is.True);
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.Hub), Is.True);
        Assert.That(Enum.IsDefined(typeof(ServerFilterCategory), ServerFilterCategory.Recommended), Is.True);
    }
}
