using System.Security.Cryptography;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a secret hasher.
/// </summary>
public interface ISecretHasher
{
    /// <summary>
    /// Hashes the secret.
    /// </summary>
    /// <param name="secret">The secret to hash.</param>
    /// <returns>The hashed secret.</returns>
    HashedSecret HashSecret(string secret);
    
    /// <summary>
    /// Hashes the secret.
    /// </summary>
    /// <param name="secret">The secret to hash.</param>
    /// <param name="salt">The salt to use.</param>
    /// <returns>The hashed secret.</returns>
    HashedSecret HashSecret(string secret, byte[] salt);
    
    /// <summary>
    /// Hashes the secret.
    /// </summary>
    /// <param name="secret">The secret to hash.</param>
    /// <param name="salt">The salt to use.</param>
    /// <returns>The hashed secret.</returns>
    byte[] HashSecret(byte[] secret, byte[] salt);
    
    /// <summary>
    /// Verifies the secret.
    /// </summary>
    /// <param name="clearTextSecret">The secret to verify.</param>
    /// <param name="secret">The hashed secret.</param>
    /// <param name="salt">The salt.</param>
    /// <returns>True if the secret is valid, otherwise false.</returns>
    bool VerifySecret(string clearTextSecret, string secret, string salt);
    
    /// <summary>
    /// Verifies the secret.
    /// </summary>
    /// <param name="clearTextSecret">The secret to verify.</param>
    /// <param name="hashedSecret">The hashed secret.</param>
    /// <returns>True if the secret is valid, otherwise false.</returns>
    bool VerifySecret(string clearTextSecret, HashedSecret hashedSecret);

    /// <summary>
    /// Generates a salt.
    /// </summary>
    /// <param name="saltSize">The size of the salt.</param>
    /// <returns>The salt.</returns>
    byte[] GenerateSalt(int saltSize = 32) => RandomNumberGenerator.GetBytes(saltSize);
}