#nullable enable
using System;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class SecureTokenStorageTests
{
    [Test]
    public void TestProtectAndUnprotect_RoundTrip()
    {
        var originalToken = "header.payload.signature_super_secret_token_12345";
        var protectedToken = SecureTokenStorage.Protect(originalToken);

        Assert.That(protectedToken, Is.Not.EqualTo(originalToken));
        Assert.That(protectedToken, Is.Not.Null.And.Not.Empty);

        var restoredToken = SecureTokenStorage.Unprotect(protectedToken);
        Assert.That(restoredToken, Is.EqualTo(originalToken));
    }

    [Test]
    public void TestUnprotect_LegacyPlaintextCompatibility()
    {
        var legacyToken = "old_unencrypted_raw_token_xyz_98765";
        var result = SecureTokenStorage.Unprotect(legacyToken);

        Assert.That(result, Is.EqualTo(legacyToken));
    }

    [TestCase(null)]
    [TestCase("")]
    public void TestProtectAndUnprotect_NullOrEmpty(string? input)
    {
        Assert.That(SecureTokenStorage.Protect(input), Is.EqualTo(""));
        Assert.That(SecureTokenStorage.Unprotect(input), Is.EqualTo(""));
    }
}
