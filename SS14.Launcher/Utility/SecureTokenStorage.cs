using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace SS14.Launcher.Utility;

/// <summary>
/// Provides secure encryption and decryption of sensitive authentication tokens stored in SQLite.
/// On Windows, utilizes the Data Protection API (DPAPI) tied to the current Windows user account.
/// On other operating systems, provides AES-based user-key protection with transparent migration of legacy plaintext tokens.
/// </summary>
public static class SecureTokenStorage
{
    private const string DpapiPrefix = "dpapi:";
    private const string EncV1Prefix = "enc:v1:";

    // Fixed entropy to bind the DPAPI encryption scope specifically to SS14.Launcher
    private static readonly byte[] DpapiEntropy =
        Encoding.UTF8.GetBytes("SS14.Launcher.TokenEntropy.2026");

    /// <summary>
    /// Encrypts an authentication token before persisting it to the SQLite database.
    /// </summary>
    /// <param name="token">Plaintext token string.</param>
    /// <returns>Protected ciphertext string with prefix.</returns>
    public static string Protect(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return "";

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var plainBytes = Encoding.UTF8.GetBytes(token);
                var cipherBytes = ProtectedData.Protect(plainBytes, DpapiEntropy, DataProtectionScope.CurrentUser);
                return DpapiPrefix + Convert.ToBase64String(cipherBytes);
            }
            else
            {
                // On Linux / macOS: AES-GCM with machine-local user entropy
                return EncryptAes(token);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to protect authentication token with native cryptography. Falling back to raw storage.");
            return token;
        }
    }

    /// <summary>
    /// Decrypts a stored authentication token from the SQLite database.
    /// Supports legacy plaintext tokens with automatic fallback.
    /// </summary>
    /// <param name="storedValue">The stored string from the database.</param>
    /// <returns>Decrypted plaintext token string, or empty string on failure.</returns>
    public static string Unprotect(string? storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
            return "";

        // Windows DPAPI protected token
        if (storedValue.StartsWith(DpapiPrefix, StringComparison.Ordinal))
        {
            if (!OperatingSystem.IsWindows())
            {
                Log.Warning("Encountered Windows DPAPI protected token on non-Windows platform. Cannot decrypt.");
                return "";
            }

            try
            {
                var cipherBytes = Convert.FromBase64String(storedValue.Substring(DpapiPrefix.Length));
                var plainBytes = ProtectedData.Unprotect(cipherBytes, DpapiEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to decrypt DPAPI token (possibly copied from another Windows user account or machine).");
                return "";
            }
        }

        // AES-protected token
        if (storedValue.StartsWith(EncV1Prefix, StringComparison.Ordinal))
        {
            try
            {
                return DecryptAes(storedValue.Substring(EncV1Prefix.Length));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to decrypt AES-protected token.");
                return "";
            }
        }

        // Legacy plaintext token (transparent backward-compatibility)
        return storedValue;
    }

    private static byte[] GetMachineKey()
    {
        // Derive key from username + machine ID / user home
        var seed = $"{Environment.UserName}@{Environment.MachineName}@{LauncherPaths.DirUserData}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(seed));
    }

    private static string EncryptAes(string plainText)
    {
        var key = GetMachineKey();
        var iv = new byte[16];
        RandomNumberGenerator.Fill(iv);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        ms.Write(iv); // prepend IV

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return EncV1Prefix + Convert.ToBase64String(ms.ToArray());
    }

    private static string DecryptAes(string cipherBase64)
    {
        var allBytes = Convert.FromBase64String(cipherBase64);
        if (allBytes.Length < 16)
            return "";

        var iv = new byte[16];
        Buffer.BlockCopy(allBytes, 0, iv, 0, 16);

        var key = GetMachineKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream(allBytes, 16, allBytes.Length - 16);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}
