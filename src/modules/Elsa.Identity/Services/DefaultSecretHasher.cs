using System.Security.Cryptography;
using System.Text;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class DefaultSecretHasher : ISecretHasher
{
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
    public bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret)
    {
        var password = hashedSecret.Secret;
        var salt = hashedSecret.Salt;
        var providedHashedPassword = HashSecret(clearTextSecret, salt); 
        return providedHashedPassword.Secret.SequenceEqual(password);
    }

    /// <inheritdoc />
    public byte[] HashSecret(byte[] secret, byte[] salt)
    {
        using var sha256 = SHA256.Create();
        var passwordAndSalt = secret.Concat(salt).ToArray();
        return sha256.ComputeHash(passwordAndSalt);
    }

    /// <inheritdoc />
    public byte[] GenerateSalt(int saltSize = 32) => RandomNumberGenerator.GetBytes(saltSize);
}