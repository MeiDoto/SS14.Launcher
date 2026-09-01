#nullable enable
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class DiscordRpcTests
{
    [Test]
    public async Task TestDiscordRpc_GracefulHandlingWhenDiscordNotRunning()
    {
        using var client = new DiscordRpcClient();
        // Calling update presence when Discord is not running must not throw or crash
        Assert.DoesNotThrowAsync(async () =>
        {
            await client.UpdatePresenceAsync("Тест", "Выбирает сервер");
            await client.ClearPresenceAsync();
        });
    }
}
