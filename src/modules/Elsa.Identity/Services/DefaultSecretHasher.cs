using System.Security.Cryptography;
using System.Text;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class DefaultSecretHasher : ISecretHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const char Separator = '$';
    private const int DefaultIterationCount = 600_000;
    private const int MaxIterationCount = DefaultIterationCount * 4;
    private const int KeySize = 32;

    /// <inheritdoc />
    public HashedSecret HashSecret(string secret)
    {
        var saltBytes = GenerateSalt();
        return HashSecret(secret, saltBytes);
    }

    /// <inheritdoc />
    public HashedSecret HashSecret(string secret, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(secret);
        var hashedPassword = HashSecret(passwordBytes, salt);
        return HashedSecret.FromBytes(hashedPassword, salt);
    }

    /// <inheritdoc />
    public bool VerifySecret(string clearTextSecret, string secret, string salt)
    {
        var hashedPassword = HashedSecret.FromString(secret, salt);
        return VerifySecret(clearTextSecret, hashedPassword);
    }

    /// <inheritdoc />
    public bool VerifySecret(string clearTextSecret, string secret, string salt, out bool needsRehash)
    {
        var hashedPassword = HashedSecret.FromString(secret, salt);
        return VerifySecret(clearTextSecret, hashedPassword, out needsRehash);
    }

    /// <inheritdoc />
    public bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret)
    {
        return VerifySecret(clearTextSecret, hashedSecret, out _);
    }

    /// <inheritdoc />
    public bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret, out bool needsRehash)
    {
        var password = hashedSecret.Secret;
        var salt = hashedSecret.Salt;
        var passwordBytes = Encoding.UTF8.GetBytes(clearTextSecret);

        if (TryReadPbkdf2Hash(password, out var iterationCount, out var expectedHash))
        {
            var providedHash = HashSecret(passwordBytes, salt, iterationCount);
            var matches = CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
            needsRehash = matches && iterationCount < DefaultIterationCount;
            return matches;
        }

        var legacyHash = HashLegacySha256(passwordBytes, salt);
        var isLegacyMatch = CryptographicOperations.FixedTimeEquals(legacyHash, password);
        needsRehash = isLegacyMatch;
        return isLegacyMatch;
    }

    /// <inheritdoc />
    public byte[] HashSecret(byte[] secret, byte[] salt)
    {
        var hash = HashSecret(secret, salt, DefaultIterationCount);
        var encodedHash = Convert.ToBase64String(hash);
        return Encoding.UTF8.GetBytes($"{Algorithm}{Separator}{DefaultIterationCount}{Separator}{encodedHash}");
    }

    /// <inheritdoc />
    public byte[] GenerateSalt(int saltSize = 32) => RandomNumberGenerator.GetBytes(saltSize);

    private static byte[] HashSecret(byte[] secret, byte[] salt, int iterationCount)
    {
        return Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterationCount, HashAlgorithmName.SHA256, KeySize);
    }

    private static byte[] HashLegacySha256(byte[] secret, byte[] salt)
    {
        return SHA256.HashData(secret.Concat(salt).ToArray());
    }

    private static bool TryReadPbkdf2Hash(byte[] secret, out int iterationCount, out byte[] hash)
    {
        iterationCount = 0;
        hash = [];

        var hashString = Encoding.UTF8.GetString(secret);
        var segments = hashString.Split(Separator, 3);
        if (segments.Length != 3 || !string.Equals(segments[0], Algorithm, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(segments[1], out iterationCount) || iterationCount <= 0 || iterationCount > MaxIterationCount)
            return false;

        try
        {
            hash = Convert.FromBase64String(segments[2]);
            return true;
        }
        catch (FormatException)
        {
            iterationCount = 0;
            hash = [];
            return false;
        }
    }
}
