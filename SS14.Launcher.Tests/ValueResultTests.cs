using System;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class ValueResultTests
{
    [Test]
    public void TestValueResultSuccess()
    {
        var res = ValueResult<int>.Ok(42);
        Assert.That(res.IsSuccess, Is.True);
        Assert.That(res.IsFailure, Is.False);
        Assert.That(res.Value, Is.EqualTo(42));
        Assert.That(res.Error, Is.Empty);

        Assert.That(res.TryGetValue(out var val, out var err), Is.True);
        Assert.That(val, Is.EqualTo(42));
        Assert.That(err, Is.Null);
    }

    [Test]
    public void TestValueResultFailure()
    {
        var res = ValueResult<string>.Fail("Not found");
        Assert.That(res.IsSuccess, Is.False);
        Assert.That(res.IsFailure, Is.True);
        Assert.That(res.Error, Is.EqualTo("Not found"));
        Assert.Throws<InvalidOperationException>(() => _ = res.Value);

        Assert.That(res.TryGetValue(out var val, out var err), Is.False);
        Assert.That(val, Is.Null);
        Assert.That(err, Is.EqualTo("Not found"));
    }

    [Test]
    public void TestGenericTypedErrorValueResult()
    {
        var success = ValueResult<string, int>.Ok("Hello");
        Assert.That(success.IsSuccess, Is.True);
        Assert.That(success.Value, Is.EqualTo("Hello"));

        var failure = ValueResult<string, int>.Fail(404);
        Assert.That(failure.IsFailure, Is.True);
        Assert.That(failure.Error, Is.EqualTo(404));
        Assert.Throws<InvalidOperationException>(() => _ = failure.Value);
    }
}
