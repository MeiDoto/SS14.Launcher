using System;
using NUnit.Framework;
using SS14.Launcher;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ConfigConstantsTests
{
    [Test]
    public void TestEssentialUrlsAreValidUris()
    {
        Assert.That(Uri.IsWellFormedUriString(ConfigConstants.WebsiteUrl, UriKind.Absolute), Is.True);
        Assert.That(Uri.IsWellFormedUriString(ConfigConstants.DiscordUrl, UriKind.Absolute), Is.True);
        Assert.That(Uri.IsWellFormedUriString(ConfigConstants.AccountBaseUrl, UriKind.Absolute), Is.True);
        Assert.That(Uri.IsWellFormedUriString(ConfigConstants.AccountRegisterUrl, UriKind.Absolute), Is.True);
        Assert.That(Uri.IsWellFormedUriString(ConfigConstants.NewsFeedUrl, UriKind.Absolute), Is.True);
        Assert.That(Uri.IsWellFormedUriString(ConfigConstants.TranslateUrl, UriKind.Absolute), Is.True);
    }

    [Test]
    public void TestFallbackSetsAreNotEmpty()
    {
        Assert.That(ConfigConstants.AuthUrl.Urls.Length, Is.GreaterThan(0));
        Assert.That(ConfigConstants.DefaultHubUrls.Length, Is.GreaterThan(0));
        foreach (var hub in ConfigConstants.DefaultHubUrls)
        {
            Assert.That(hub.Urls.Length, Is.GreaterThan(0));
        }
    }

    [Test]
    public void TestLauncherRepoAndPipeConfiguration()
    {
        Assert.That(ConfigConstants.LauncherGitHubRepo, Is.EqualTo("MeiDoto/SS14.Launcher"));
        Assert.That(ConfigConstants.LauncherCommandsNamedPipeName, Is.Not.Empty);
        Assert.That(ConfigConstants.LauncherCommandsNamedPipeTimeout, Is.GreaterThan(0));
    }
}
