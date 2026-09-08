using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class DesktopNotificationTests
{
    [Test]
    public void TestNotifyDoesNotThrowOnNullOrEmpty()
    {
        Assert.DoesNotThrow(() =>
        {
            DesktopNotificationManager.Notify("", "");
            DesktopNotificationManager.Notify("Test Server", "Server is online with 10 players", "ss14://localhost:1212");
        });
    }

    [Test]
    public void TestNotifySpecialCharactersInServerName()
    {
        Assert.DoesNotThrow(() =>
        {
            DesktopNotificationManager.Notify("Server \"With\" $Special `Chars` & 'Quotes'", "Message text", "ss14://127.0.0.1:1212");
        });
    }
}
