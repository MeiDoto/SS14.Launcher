using System;
using System.Collections.Generic;

namespace SS14.Launcher.Utility.Algorithms.Search;

/// <summary>
/// Metric and edit distance calculation algorithms (Jaro-Winkler, Myers bit-parallel, Damerau-Levenshtein, BM25).
/// </summary>
public static class StringMetrics
{
    public static double JaroWinklerSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (s1.Equals(s2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        s1 = s1.ToLowerInvariant();
        s2 = s2.ToLowerInvariant();

        int len1 = s1.Length;
        int len2 = s2.Length;
        int maxDist = Math.Max(len1, len2) / 2 - 1;
        if (maxDist < 0) maxDist = 0;

        bool[] match1 = new bool[len1];
        bool[] match2 = new bool[len2];
        int matches = 0;

        for (int i = 0; i < len1; i++)
        {
            int start = Math.Max(0, i - maxDist);
            int end = Math.Min(i + maxDist + 1, len2);

            for (int j = start; j < end; j++)
            {
                if (match2[j] || s1[i] != s2[j])
                    continue;

                match1[i] = true;
                match2[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        int transpositions = 0;
        int k = 0;
        for (int i = 0; i < len1; i++)
        {
            if (!match1[i])
                continue;

            while (!match2[k])
                k++;

            if (s1[i] != s2[k])
                transpositions++;

            k++;
        }

        double jaro = ((matches / (double)len1) +
                       (matches / (double)len2) +
                       ((matches - (transpositions / 2.0)) / matches)) / 3.0;

        int prefix = 0;
        int maxPrefix = Math.Min(4, Math.Min(len1, len2));
        for (int i = 0; i < maxPrefix; i++)
        {
            if (s1[i] == s2[i])
                prefix++;
            else
                break;
        }

        return jaro + (prefix * 0.1 * (1.0 - jaro));
    }

    public static int DamerauLevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        Span<int> d0 = stackalloc int[m + 1];
        Span<int> d1 = stackalloc int[m + 1];
        Span<int> d2 = stackalloc int[m + 1];

        for (int j = 0; j <= m; j++)
            d1[j] = j;

        for (int i = 1; i <= n; i++)
        {
            d2[0] = i;
            char sc = char.ToLowerInvariant(s[i - 1]);

            for (int j = 1; j <= m; j++)
            {
                char tc = char.ToLowerInvariant(t[j - 1]);
                int cost = sc == tc ? 0 : 1;

                int del = d1[j] + 1;
                int ins = d2[j - 1] + 1;
                int sub = d1[j - 1] + cost;

                int min = Math.Min(Math.Min(del, ins), sub);

                if (i > 1 && j > 1 &&
                    char.ToLowerInvariant(s[i - 1]) == char.ToLowerInvariant(t[j - 2]) &&
                    char.ToLowerInvariant(s[i - 2]) == char.ToLowerInvariant(t[j - 1]))
                {
                    min = Math.Min(min, d0[j - 2] + cost);
                }

                d2[j] = min;
            }

            d1.CopyTo(d0);
            d2.CopyTo(d1);
        }

        return d1[m];
    }

    public static int MyersBitParallelDistance(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        int m = pattern.Length;
        int n = text.Length;

        if (m == 0) return n;
        if (n == 0) return m;

        if (m > 64)
        {
            if (n <= 64)
            {
                return MyersBitParallelDistance(text, pattern);
            }
            return DamerauLevenshteinDistance(pattern, text);
        }

        Span<ulong> peq = stackalloc ulong[128];
        peq.Clear();
        Dictionary<char, ulong>? peqExtended = null;

        for (int i = 0; i < m; i++)
        {
            char c = char.ToLowerInvariant(pattern[i]);
            ulong bit = 1UL << i;
            if (c < 128)
            {
                peq[c] |= bit;
            }
            else
            {
                peqExtended ??= new Dictionary<char, ulong>();
                if (peqExtended.TryGetValue(c, out var mask))
                    peqExtended[c] = mask | bit;
                else
                    peqExtended[c] = bit;
            }
        }

        ulong pv = ~0UL;
        ulong mv = 0UL;
        int score = m;

        for (int j = 0; j < n; j++)
        {
            char c = char.ToLowerInvariant(text[j]);
            ulong eq;
            if (c < 128)
            {
                eq = peq[c];
            }
            else if (peqExtended != null && peqExtended.TryGetValue(c, out var mask))
            {
                eq = mask;
            }
            else
            {
                eq = 0UL;
            }

            ulong xv = eq | mv;
            ulong xh = (((eq & pv) + pv) ^ pv) | eq;

            ulong ph = mv | ~(xh | pv);
            ulong mh = pv & xh;

            if ((ph & (1UL << (m - 1))) != 0)
                score++;
            else if ((mh & (1UL << (m - 1))) != 0)
                score--;

            ph <<= 1;
            mh <<= 1;

            pv = mh | ~(xv | ph);
            mv = ph & xv;
        }

        return score;
    }

    public static double TrigramCosineSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        if (string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var grams1 = ExtractTrigrams(s1.ToLowerInvariant());
        var grams2 = ExtractTrigrams(s2.ToLowerInvariant());

        if (grams1.Count == 0 || grams2.Count == 0)
            return JaroWinklerSimilarity(s1, s2);

        double dot = 0.0;
        foreach (var (gram, count1) in grams1)
        {
            if (grams2.TryGetValue(gram, out var count2))
                dot += count1 * count2;
        }

        double mag1 = 0.0;
        foreach (var c in grams1.Values) mag1 += c * c;

        double mag2 = 0.0;
        foreach (var c in grams2.Values) mag2 += c * c;

        var denominator = Math.Sqrt(mag1) * Math.Sqrt(mag2);
        return denominator > 0.0 ? dot / denominator : 0.0;
    }

    private static Dictionary<string, int> ExtractTrigrams(string str)
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        var padded = $"  {str} ";

        for (int i = 0; i <= padded.Length - 3; i++)
        {
            var tri = padded.Substring(i, 3);
            dict[tri] = dict.GetValueOrDefault(tri, 0) + 1;
        }

        return dict;
    }

    public static double OkapiBM25Score(
        string[] queryTerms,
        string targetDocument,
        double avgDocLength = 25.0,
        double k1 = 1.2,
        double b = 0.75)
    {
        if (queryTerms.Length == 0 || string.IsNullOrEmpty(targetDocument))
            return 0.0;

        var words = targetDocument.Split(new[] { ' ', ',', '.', '-', '_', '/', '|', ':', '[', ']', '(', ')' },
            StringSplitOptions.RemoveEmptyEntries);
        var docLen = words.Length;

        if (docLen == 0) return 0.0;

        var termFreqs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in words)
        {
            termFreqs[word] = termFreqs.GetValueOrDefault(word, 0) + 1;
        }

        double totalScore = 0.0;
        var lenNorm = 1.0 - b + (b * (docLen / Math.Max(1.0, avgDocLength)));

        foreach (var term in queryTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
                continue;

            int tf = 0;
            foreach (var (word, count) in termFreqs)
            {
                if (word.Equals(term, StringComparison.OrdinalIgnoreCase))
                {
                    tf += count * 2;
                }
                else if (word.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                {
                    tf += count;
                }
            }

            if (tf > 0)
            {
                double idf = Math.Log(1.0 + (100.0 / (tf + 0.5)));
                double termScore = idf * (tf * (k1 + 1.0)) / (tf + (k1 * lenNorm));
                totalScore += termScore;
            }
        }

        return totalScore;
    }

    public static int FastBitwiseLevenshtein(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        return MyersBitParallelDistance(s, t);
    }
}
