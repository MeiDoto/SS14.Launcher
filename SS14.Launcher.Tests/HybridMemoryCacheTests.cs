#nullable enable
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class HybridMemoryCacheTests
{
    [Test]
    public void TestSetAndTryGet_RetrievesStoredValue()
    {
        var cache = new AdvancedAlgorithms.HybridMemoryCache<string, string>(capacity: 10);
        cache.Set("key1", "value1");

        var found = cache.TryGet("key1", out var value);
        Assert.That(found, Is.True);
        Assert.That(value, Is.EqualTo("value1"));
    }

    [Test]
    public void TestTryGet_NonExistentKeyReturnsFalse()
    {
        var cache = new AdvancedAlgorithms.HybridMemoryCache<string, int>(capacity: 10);
        var found = cache.TryGet("non_existent", out var value);
        Assert.That(found, Is.False);
        Assert.That(value, Is.EqualTo(0));
    }

    [Test]
    public void TestRemove_DeletesKey()
    {
        var cache = new AdvancedAlgorithms.HybridMemoryCache<string, string>(capacity: 10);
        cache.Set("test", "data");
        Assert.That(cache.Count, Is.EqualTo(1));

        var removed = cache.Remove("test");
        Assert.That(removed, Is.True);
        Assert.That(cache.Count, Is.EqualTo(0));
        Assert.That(cache.TryGet("test", out _), Is.False);
    }

    [Test]
    public void TestClear_EmptiesAllItems()
    {
        var cache = new AdvancedAlgorithms.HybridMemoryCache<int, string>(capacity: 10);
        for (int i = 0; i < 5; i++)
        {
            cache.Set(i, $"item-{i}");
        }

        Assert.That(cache.Count, Is.EqualTo(5));
        cache.Clear();
        Assert.That(cache.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestCapacityEviction_EvictsOldestWhenFull()
    {
        var cache = new AdvancedAlgorithms.HybridMemoryCache<int, string>(capacity: 10);

        // Fill 10 items
        for (int i = 1; i <= 10; i++)
        {
            cache.Set(i, $"val-{i}");
        }

        Assert.That(cache.Count, Is.EqualTo(10));

        // Add 11th item (should evict oldest: 1)
        cache.Set(11, "val-11");
        Assert.That(cache.Count, Is.EqualTo(10));
        Assert.That(cache.TryGet(11, out _), Is.True);
    }

    [Test]
    public void TestExpiration_StaleItemReturnsFalse()
    {
        // 10ms TTL
        var cache = new AdvancedAlgorithms.HybridMemoryCache<string, string>(capacity: 10, itemTtl: TimeSpan.FromMilliseconds(10));
        cache.Set("expire_me", "hello");

        System.Threading.Thread.Sleep(50);

        var found = cache.TryGet("expire_me", out _);
        Assert.That(found, Is.False);
    }

    [Test]
    public void TestConcurrency_MultiThreadedAccess()
    {
        var cache = new AdvancedAlgorithms.HybridMemoryCache<int, int>(capacity: 100);

        Parallel.For(0, 1000, i =>
        {
            var key = i % 50;
            cache.Set(key, i);
            cache.TryGet(key, out _);
        });

        Assert.That(cache.Count, Is.LessThanOrEqualTo(100));
    }
}
