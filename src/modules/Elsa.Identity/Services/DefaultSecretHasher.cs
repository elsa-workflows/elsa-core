using System.Buffers;
using System.Buffers.Text;
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
    private const byte SeparatorByte = (byte)Separator;
    private const int DefaultIterationCount = 600_000;
    private const int MaxIterationCount = DefaultIterationCount * 4;
    private const int KeySize = 32;
    private static readonly byte[] AlgorithmBytes = Encoding.UTF8.GetBytes(Algorithm);

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
        try
        {
            var hashedPassword = HashSecret(passwordBytes, salt);
            return HashedSecret.FromBytes(hashedPassword, salt);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
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
        var storedSecretBytes = hashedSecret.Secret;
        var saltBytes = hashedSecret.Salt;
        var clearTextBytes = Encoding.UTF8.GetBytes(clearTextSecret);
        Span<byte> expectedHash = stackalloc byte[KeySize];
        byte[]? providedHash = null;
        byte[]? legacyHash = null;

        try
        {
            if (TryReadPbkdf2Hash(storedSecretBytes, expectedHash, out var iterationCount))
            {
                providedHash = HashSecret(clearTextBytes, saltBytes, iterationCount);
                var matches = CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
                needsRehash = matches && iterationCount < DefaultIterationCount;
                return matches;
            }

            legacyHash = HashLegacySha256(clearTextBytes, saltBytes);
            if (storedSecretBytes.Length != legacyHash.Length)
            {
                needsRehash = false;
                return false;
            }

            var isLegacyMatch = CryptographicOperations.FixedTimeEquals(legacyHash, storedSecretBytes);
            needsRehash = isLegacyMatch;
            return isLegacyMatch;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clearTextBytes);

            if (providedHash is not null)
                CryptographicOperations.ZeroMemory(providedHash);

            if (legacyHash is not null)
                CryptographicOperations.ZeroMemory(legacyHash);

            CryptographicOperations.ZeroMemory(expectedHash);
        }
    }

    /// <inheritdoc />
    public byte[] HashSecret(byte[] secret, byte[] salt)
    {
        var hash = HashSecret(secret, salt, DefaultIterationCount);
        try
        {
            return FormatHashEnvelope(hash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }

    /// <inheritdoc />
    public byte[] GenerateSalt(int saltSize = 32) => RandomNumberGenerator.GetBytes(saltSize);

    private static byte[] HashSecret(byte[] secret, byte[] salt, int iterationCount)
    {
        return Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterationCount, HashAlgorithmName.SHA256, KeySize);
    }

    private static byte[] FormatHashEnvelope(byte[] hash)
    {
        Span<byte> iterationBytes = stackalloc byte[16];
        if (!Utf8Formatter.TryFormat(DefaultIterationCount, iterationBytes, out var iterationBytesWritten))
            throw new InvalidOperationException("Failed to format the PBKDF2 iteration count.");

        var encodedHashLength = ((hash.Length + 2) / 3) * 4;
        var result = new byte[AlgorithmBytes.Length + 1 + iterationBytesWritten + 1 + encodedHashLength];
        var resultSpan = result.AsSpan();
        AlgorithmBytes.CopyTo(resultSpan);

        var offset = AlgorithmBytes.Length;
        resultSpan[offset++] = SeparatorByte;
        iterationBytes[..iterationBytesWritten].CopyTo(resultSpan[offset..]);
        offset += iterationBytesWritten;
        resultSpan[offset++] = SeparatorByte;

        var status = Base64.EncodeToUtf8(hash, resultSpan[offset..], out var consumed, out var written);
        if (status != OperationStatus.Done || consumed != hash.Length || written != encodedHashLength)
            throw new InvalidOperationException("Failed to encode the PBKDF2 hash.");

        return result;
    }

    private static byte[] HashLegacySha256(byte[] secret, byte[] salt)
    {
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha256.AppendData(secret);
        sha256.AppendData(salt);
        return sha256.GetHashAndReset();
    }

    private static bool TryReadPbkdf2Hash(byte[] storedSecretBytes, Span<byte> hash, out int iterationCount)
    {
        iterationCount = 0;

        var envelope = storedSecretBytes.AsSpan();
        var algorithmSeparatorIndex = envelope.IndexOf(SeparatorByte);
        if (algorithmSeparatorIndex <= 0)
            return false;

        var algorithmBytes = envelope[..algorithmSeparatorIndex];
        if (!algorithmBytes.SequenceEqual(AlgorithmBytes))
            return false;

        var iterationAndHashBytes = envelope[(algorithmSeparatorIndex + 1)..];
        var iterationSeparatorIndex = iterationAndHashBytes.IndexOf(SeparatorByte);
        if (iterationSeparatorIndex <= 0)
            return false;

        var iterationBytes = iterationAndHashBytes[..iterationSeparatorIndex];
        if (!Utf8Parser.TryParse(iterationBytes, out iterationCount, out var bytesConsumed) || bytesConsumed != iterationBytes.Length || iterationCount <= 0 || iterationCount > MaxIterationCount)
            return false;

        var encodedHashBytes = iterationAndHashBytes[(iterationSeparatorIndex + 1)..];
        var status = Base64.DecodeFromUtf8(encodedHashBytes, hash, out var consumed, out var written);
        if (status == OperationStatus.Done && consumed == encodedHashBytes.Length && written == KeySize)
            return true;

        iterationCount = 0;
        CryptographicOperations.ZeroMemory(hash);
        return false;
    }
}
