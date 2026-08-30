using System;
using System.Linq;
using SS14.Launcher.Models.ServerStatus;

namespace SS14.Launcher.Utility;

public static class SearchAlgorithm
{
    public static int GetMatchScore(string? query, string? target)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 100;

        if (string.IsNullOrWhiteSpace(target))
            return 0;

        var q = query.Trim();
        var t = target.Trim();

        // Exact match
        if (string.Equals(q, t, StringComparison.OrdinalIgnoreCase))
            return 1000;

        // Starts with
        if (t.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            return 800;

        // Word boundary match
        var words = t.Split([' ', '-', '_', '/', '|', ':', '[', ']'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                return 600;
        }

        // Substring match
        var idx = t.IndexOf(q, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return 400 - Math.Min(idx * 5, 200);

        // Subsequence match
        var qIdx = 0;
        var consecutiveBonus = 0;
        var totalScore = 0;
        for (var i = 0; i < t.Length && qIdx < q.Length; i++)
        {
            if (char.ToLowerInvariant(t[i]) == char.ToLowerInvariant(q[qIdx]))
            {
                qIdx++;
                consecutiveBonus += 15;
                totalScore += 20 + consecutiveBonus;
            }
            else
            {
                consecutiveBonus = 0;
            }
        }

        if (qIdx == q.Length)
            return 150 + Math.Min(totalScore, 150);

        // Fuzzy match
        if (q.Length >= 3)
        {
            var trigramSim = AdvancedAlgorithms.TrigramCosineSimilarity(q, t);
            if (trigramSim >= 0.65)
                return (int)(trigramSim * 350);

            foreach (var word in words)
            {
                var jw = AdvancedAlgorithms.JaroWinklerSimilarity(q, word);
                if (jw >= 0.80)
                    return (int)(jw * 300);

                if (q.Length >= 4)
                {
                    var dist = AdvancedAlgorithms.MyersBitParallelDistance(q.AsSpan(), word.AsSpan());
                    if (dist <= 2)
                        return 220 - (dist * 60);
                }
            }
        }

        return 0;
    }

    public static int LevenshteinDistance(string s, string t)
    {
        return AdvancedAlgorithms.MyersBitParallelDistance(s.AsSpan(), t.AsSpan());
    }

    public static double CalculateQualityScore(ServerStatusData server, bool isFavorite)
    {
        if (server.Status == ServerStatusCode.Offline)
            return -10000;

        double? pingMs = server.Ping.HasValue ? server.Ping.Value.TotalMilliseconds : null;
        var isInRound = server.RoundStatus == GameRoundStatus.InRound;

        return AdvancedAlgorithms.CalculatePredictiveQualityIndex(
            server.PlayerCount,
            server.SoftMaxPlayerCount > 0 ? server.SoftMaxPlayerCount : 100,
            pingMs,
            pingJitter: 0.0,
            isFavorite: isFavorite,
            playerVelocity: 0f,
            isInRound: isInRound,
            isPanicBunker: server.PanicBunker);
    }
}
