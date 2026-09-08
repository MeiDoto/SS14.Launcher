using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SS14.Launcher.Utility.Algorithms.Caching;

/// <summary>
/// Thread-safe in-memory cache combining LRU eviction with time-to-live (TTL) expiration.
/// </summary>
public class HybridMemoryCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly TimeSpan _itemTtl;
    private readonly ConcurrentDictionary<TKey, CacheNode> _map = new();
    private readonly LinkedList<TKey> _lruList = new();
    private readonly object _lock = new();

    private sealed class CacheNode
    {
        public TKey Key { get; }
        public TValue Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AccessFrequency { get; set; }
        public LinkedListNode<TKey>? LruNode { get; set; }

        public CacheNode(TKey key, TValue value, DateTime expiresAt)
        {
            Key = key;
            Value = value;
            ExpiresAt = expiresAt;
            AccessFrequency = 1;
        }
    }

    /// <summary>
    /// Creates a new hybrid memory cache.
    /// </summary>
    /// <param name="capacity">Maximum number of entries. Minimum enforced: 10.</param>
    /// <param name="itemTtl">Time-to-live for each cache entry. Default: 10 minutes.</param>
    public HybridMemoryCache(int capacity = 500, TimeSpan? itemTtl = null)
    {
        _capacity = Math.Max(10, capacity);
        _itemTtl = itemTtl ?? TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Attempts to retrieve a value from the cache. Promotes the entry to MRU on access.
    /// Expired entries are evicted transparently. Thread-safe.
    /// </summary>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="value">The cached value if found and not expired; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the key was found and not expired; <c>false</c> otherwise.</returns>
    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var node))
            {
                if (DateTime.UtcNow > node.ExpiresAt)
                {
                    if (node.LruNode != null && node.LruNode.List != null)
                    {
                        _lruList.Remove(node.LruNode);
                    }
                    _map.TryRemove(key, out _);
                    value = default;
                    return false;
                }

                node.AccessFrequency++;
                if (node.LruNode != null && node.LruNode.List != null)
                {
                    _lruList.Remove(node.LruNode);
                    _lruList.AddFirst(node.LruNode);
                }

                value = node.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            var expires = DateTime.UtcNow + _itemTtl;

            if (_map.TryGetValue(key, out var existing))
            {
                existing.Value = value;
                existing.ExpiresAt = expires;
                existing.AccessFrequency++;
                if (existing.LruNode != null && existing.LruNode.List != null)
                {
                    _lruList.Remove(existing.LruNode);
                    _lruList.AddFirst(existing.LruNode);
                }
                return;
            }

            if (_map.Count >= _capacity)
            {
                var last = _lruList.Last;
                if (last != null)
                {
                    _lruList.RemoveLast();
                    _map.TryRemove(last.Value, out _);
                }
            }

            var lruNode = new LinkedListNode<TKey>(key);
            _lruList.AddFirst(lruNode);

            var newNode = new CacheNode(key, value, expires)
            {
                LruNode = lruNode
            };
            _map[key] = newNode;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_map.TryRemove(key, out var node))
            {
                if (node.LruNode != null && node.LruNode.List != null)
                {
                    _lruList.Remove(node.LruNode);
                }
                return true;
            }
        }
        return false;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _map.Clear();
            _lruList.Clear();
        }
    }

    public int Count => _map.Count;
}
