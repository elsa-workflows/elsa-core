namespace Elsa.Identity.Models;

/// <summary>
/// Represents a hashed secret.
/// </summary>
/// <param name="Secret">A base64 encoded string representing the hashed secret.</param>
/// <param name="Salt">A base64 encoded string representing the salt.</param>
public record HashedSecret(byte[] Secret, byte[] Salt)
{
    /// <summary>
    /// Creates a new instance of <see cref="HashedSecret"/> from a byte array representing the hashed secret and the salt.
    /// </summary>
    /// <param name="secret">The hashed secret.</param>
    /// <param name="salt">The salt.</param>
    /// <returns>A new instance of <see cref="HashedSecret"/>.</returns>
    public static HashedSecret FromBytes(byte[] secret, byte[] salt) => new(secret, salt);
    
    /// <summary>
    /// Creates a new instance of <see cref="HashedSecret"/> from a string representing the hashed secret and the salt.
    /// </summary>
    /// <param name="secret">The hashed secret.</param>
    /// <param name="salt">The salt.</param>
    /// <returns>A new instance of <see cref="HashedSecret"/>.</returns>
    public static HashedSecret FromString(string secret, string salt) => new(Decode(secret), Decode(salt));
    
    /// <summary>
    /// Encodes the secret using base64.
    /// </summary>
    /// <returns>The base64-encoded secret.</returns>
    public string EncodeSecret() => Encode(Secret);
    
    /// <summary>
    /// Encodes the salt using base64.
    /// </summary>
    /// <returns>The base64-encoded salt.</returns>
    public string EncodeSalt() => Encode(Salt);
    
    private static byte[] Decode(string value) => Convert.FromBase64String(value);
    private static string Encode(byte[] value) => Convert.ToBase64String(value);
}