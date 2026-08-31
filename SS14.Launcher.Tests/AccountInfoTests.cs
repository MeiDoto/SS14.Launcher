#nullable enable

using System;
using NUnit.Framework;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.ViewModels;

using System.Net.Http;
using SS14.Launcher.Api;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class AccountInfoTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (Locator.Current.GetService<DataManager>() == null)
        {
            Locator.CurrentMutable.RegisterConstant(new DataManager());
        }

        if (Locator.Current.GetService<LoginManager>() == null)
        {
            var dm = Locator.Current.GetRequiredService<DataManager>();
            var authApi = new AuthApi(new HttpClient());
            Locator.CurrentMutable.RegisterConstant(new LoginManager(dm, authApi));
        }
    }

    [Test]
    public void TestUserIdVisibilityToggle()
    {
        var vm = new AccountInfoViewModel();

        Assert.That(vm.IsUserIdRevealed, Is.False);
        Assert.That(vm.UserIdToggleIcon, Is.EqualTo("👁"));

        vm.ToggleUserIdVisibility();
        Assert.That(vm.IsUserIdRevealed, Is.True);
        Assert.That(vm.UserIdToggleIcon, Is.EqualTo("🔒"));

        vm.ToggleUserIdVisibility();
        Assert.That(vm.IsUserIdRevealed, Is.False);
        Assert.That(vm.UserIdToggleIcon, Is.EqualTo("👁"));
    }

    [Test]
    public void TestHwidVisibilityToggle()
    {
        var vm = new AccountInfoViewModel();

        Assert.That(vm.IsHwidRevealed, Is.False);
        Assert.That(vm.HwidToggleIcon, Is.EqualTo("👁"));

        vm.ToggleHwidVisibility();
        Assert.That(vm.IsHwidRevealed, Is.True);
        Assert.That(vm.HwidToggleIcon, Is.EqualTo("🔒"));

        vm.ToggleHwidVisibility();
        Assert.That(vm.IsHwidRevealed, Is.False);
        Assert.That(vm.HwidToggleIcon, Is.EqualTo("👁"));
    }

    [Test]
    public void TestGuestAccountFallbacks()
    {
        var vm = new AccountInfoViewModel();
        vm.Populate();

        Assert.That(vm.UserId, Is.EqualTo(Guid.Empty.ToString()));
        Assert.That(vm.Hwid, Is.Not.Null.And.Not.Empty);
        Assert.That(vm.SystemInfo, Is.Not.Null.And.Not.Empty);
    }
}
