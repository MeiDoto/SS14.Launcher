using System;
using NUnit.Framework;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class DirectConnectTests
{
    [TestCase("ss14://127.0.0.1:1212", true, "127.0.0.1", 1212)]
    [TestCase("ss14s://game.spacestation14.com:443", true, "game.spacestation14.com", 443)]
    [TestCase("ss14://localhost:1212", true, "localhost", 1212)]
    [TestCase("127.0.0.1:1212", true, "127.0.0.1", 1212)] // auto-prefixed with ss14://
    [TestCase("", false, "", 0)]
    [TestCase("   ", false, "", 0)]
    public void TestUriHelperParsing(string input, bool expectedSuccess, string expectedHost, int expectedPort)
    {
        var success = UriHelper.TryParseSs14Uri(input, out var uri);
        Assert.That(success, Is.EqualTo(expectedSuccess));

        if (expectedSuccess && uri != null)
        {
            Assert.That(uri.Host, Is.EqualTo(expectedHost));
            Assert.That(uri.Port, Is.EqualTo(expectedPort));
        }
    }

    [Test]
    public void TestHttpSchemeIsRejected()
    {
        // UriHelper only accepts ss14:// and ss14s://
        Assert.That(UriHelper.TryParseSs14Uri("http://localhost:1212/", out _), Is.False);
        Assert.That(UriHelper.TryParseSs14Uri("https://corvax.org/server/", out _), Is.False);
    }

    [Test]
    public void TestCommandInjectionCharsRejected()
    {
        Assert.That(UriHelper.TryParseSs14Uri("ss14://server;rm -rf /", out _), Is.False);
        Assert.That(UriHelper.TryParseSs14Uri("ss14://server&whoami", out _), Is.False);
        Assert.That(UriHelper.TryParseSs14Uri("ss14://server|cat /etc/passwd", out _), Is.False);
        Assert.That(UriHelper.TryParseSs14Uri("ss14://server`id`", out _), Is.False);
    }

    [TestCase("ss14://127.0.0.1:1212")]
    [TestCase("ss14s://corvax.org")]
    public void TestGetServerStatusAddress(string input)
    {
        var uri = UriHelper.ParseSs14Uri(input);
        var statusUri = UriHelper.GetServerStatusAddress(uri);
        Assert.That(statusUri.ToString(), Does.EndWith("/status"));
    }

    [Test]
    public void TestGetServerApiAddressSchemeMapping()
    {
        var ss14Uri = UriHelper.ParseSs14Uri("ss14://localhost:1212");
        var apiUri = UriHelper.GetServerApiAddress(ss14Uri);
        Assert.That(apiUri.Scheme, Is.EqualTo("http"));

        var ss14sUri = UriHelper.ParseSs14Uri("ss14s://corvax.org");
        var apiUriS = UriHelper.GetServerApiAddress(ss14sUri);
        Assert.That(apiUriS.Scheme, Is.EqualTo("https"));
    }
}
