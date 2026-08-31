using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class SearchAlgorithmTests
{
    [Test]
    public void TestGetMatchScore_ExactAndPrefix()
    {
        // Exact match should have highest score
        var exactScore = SearchAlgorithm.GetMatchScore("Corvax", "Corvax");
        Assert.That(exactScore, Is.EqualTo(1000));

        // Prefix match
        var prefixScore = SearchAlgorithm.GetMatchScore("Corvax", "Corvax Main Server");
        Assert.That(prefixScore, Is.EqualTo(800));

        // Word boundary match
        var wordScore = SearchAlgorithm.GetMatchScore("Station", "Space Station 14");
        Assert.That(wordScore, Is.EqualTo(600));

        // Exact > Prefix > Word
        Assert.That(exactScore, Is.GreaterThan(prefixScore));
        Assert.That(prefixScore, Is.GreaterThan(wordScore));
    }

    [Test]
    public void TestGetMatchScore_SubstringAndSubsequence()
    {
        // Substring match
        var subScore = SearchAlgorithm.GetMatchScore("Roleplay", "Mega Roleplay Server [RU]");
        Assert.That(subScore, Is.GreaterThan(0));

        // Subsequence match (e.g. ss14 matching Space Station 14)
        var seqScore = SearchAlgorithm.GetMatchScore("ss14", "Space Station 14");
        Assert.That(seqScore, Is.GreaterThan(0));
    }

    [Test]
    public void TestGetMatchScore_Cyrillic()
    {
        var exactRu = SearchAlgorithm.GetMatchScore("Бункер", "Бункер");
        Assert.That(exactRu, Is.EqualTo(1000));

        var prefixRu = SearchAlgorithm.GetMatchScore("Станция", "Станция 14 Официальный");
        Assert.That(prefixRu, Is.EqualTo(800));

        var wordRu = SearchAlgorithm.GetMatchScore("Сервер", "Русский Сервер SS14");
        Assert.That(wordRu, Is.EqualTo(600));
    }

    [Test]
    public void TestGetMatchScore_EmptyAndWhitespace()
    {
        Assert.That(SearchAlgorithm.GetMatchScore("", "Server"), Is.EqualTo(100));
        Assert.That(SearchAlgorithm.GetMatchScore(null, "Server"), Is.EqualTo(100));
        Assert.That(SearchAlgorithm.GetMatchScore("   ", "Server"), Is.EqualTo(100));

        Assert.That(SearchAlgorithm.GetMatchScore("test", ""), Is.EqualTo(0));
        Assert.That(SearchAlgorithm.GetMatchScore("test", null), Is.EqualTo(0));
    }

    [Test]
    public void TestLevenshteinDistance()
    {
        Assert.That(SearchAlgorithm.LevenshteinDistance("kitten", "sitting"), Is.EqualTo(3));
        Assert.That(SearchAlgorithm.LevenshteinDistance("station", "station"), Is.EqualTo(0));
        Assert.That(SearchAlgorithm.LevenshteinDistance("station", "station1"), Is.EqualTo(1));
    }
}
