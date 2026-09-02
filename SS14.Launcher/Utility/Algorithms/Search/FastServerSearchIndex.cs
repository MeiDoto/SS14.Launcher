using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Launcher.Utility.Algorithms.Search;

/// <summary>
/// Ultra-fast memory-efficient prefix &amp; token search index for fast server list querying.
/// </summary>
public class FastServerSearchIndex<T>
{
    private readonly ConcurrentDictionary<string, HashSet<T>> _tokenMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Name, T Item)> _allEntries = new();
    private readonly object _lock = new();

    public void Rebuild(IEnumerable<(string Name, T Item)> entries)
    {
        lock (_lock)
        {
            _tokenMap.Clear();
            _allEntries.Clear();

            foreach (var (name, item) in entries)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                _allEntries.Add((name, item));
                var tokens = name.Split([' ', '-', '_', '/', '|', ':', '[', ']', '(', ')', '.', ','], StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in tokens)
                {
                    var t = token.Trim().ToLowerInvariant();
                    if (t.Length == 0) continue;

                    _tokenMap.AddOrUpdate(t,
                        _ => new HashSet<T> { item },
                        (_, set) => { lock (set) { set.Add(item); return set; } });
                }
            }
        }
    }

    public List<T> Search(string query, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            lock (_lock)
            {
                return _allEntries.Select(x => x.Item).Take(maxResults).ToList();
            }
        }

        var q = query.Trim().ToLowerInvariant();
        var results = new HashSet<T>();

        // 1. Direct token lookup
        foreach (var kvp in _tokenMap)
        {
            if (kvp.Key.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            {
                lock (kvp.Value)
                {
                    results.UnionWith(kvp.Value);
                }
                if (results.Count >= maxResults) break;
            }
        }

        // 2. Substring scan if not enough results
        if (results.Count < maxResults)
        {
            lock (_lock)
            {
                foreach (var (name, item) in _allEntries)
                {
                    if (results.Contains(item)) continue;

                    if (name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(item);
                        if (results.Count >= maxResults) break;
                    }
                }
            }
        }

        return results.Take(maxResults).ToList();
    }
}

/// <summary>
/// Default non-generic string server search index.
/// </summary>
public class FastServerSearchIndex
{
    private readonly FastServerSearchIndex<string> _inner = new();

    public void IndexServer(string id, string name)
    {
        _inner.Rebuild(new[] { (name, id) });
    }

    public List<string> Search(string query, int maxResults = 50)
    {
        return _inner.Search(query, maxResults);
    }
}
