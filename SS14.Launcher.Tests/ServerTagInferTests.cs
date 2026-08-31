#nullable enable

using System.Linq;
using NUnit.Framework;
using SS14.Launcher.Api;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class ServerTagInferTests
{
    [Test]
    public void TestInferLanguageTags()
    {
        var statusRu = new ServerApi.ServerStatus("Corvax Server [RU]", 10, 50, null, null, null);
        var inferredRu = ServerTagInfer.InferTags(statusRu).ToList();
        Assert.That(inferredRu, Contains.Item(ServerApi.Tags.TagLanguage + "ru"));

        var statusEn = new ServerApi.ServerStatus("Space Station [EN]", 10, 50, null, null, null);
        var inferredEn = ServerTagInfer.InferTags(statusEn).ToList();
        Assert.That(inferredEn, Contains.Item(ServerApi.Tags.TagLanguage + "en"));
    }

    [Test]
    public void TestInferEighteenPlusTags()
    {
        var status18 = new ServerApi.ServerStatus("Chaos Station [18+]", 10, 50, null, null, null);
        var inferred18 = ServerTagInfer.InferTags(status18).ToList();
        Assert.That(inferred18, Contains.Item(ServerApi.Tags.TagEighteenPlus));
    }

    [Test]
    public void TestInferRoleplayTags()
    {
        var statusMrp = new ServerApi.ServerStatus("Station [MRP]", 10, 50, null, null, null);
        var inferredMrp = ServerTagInfer.InferTags(statusMrp).ToList();
        Assert.That(inferredMrp, Contains.Item(ServerApi.Tags.TagRolePlay + ServerApi.Tags.RolePlayMedium));

        var statusHrp = new ServerApi.ServerStatus("Hardcore Station [HRP]", 10, 50, null, null, null);
        var inferredHrp = ServerTagInfer.InferTags(statusHrp).ToList();
        Assert.That(inferredHrp, Contains.Item(ServerApi.Tags.TagRolePlay + ServerApi.Tags.RolePlayHigh));
    }

    [Test]
    public void TestNoTagInferSuppression()
    {
        var status = new ServerApi.ServerStatus("Station [RU] [18+]", 10, 50, null, null, new[] { ServerApi.Tags.TagNoTagInfer });
        var inferred = ServerTagInfer.InferTags(status).ToList();
        Assert.That(inferred, Is.Empty);
    }
}
