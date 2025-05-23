namespace Elsa.SasTokens.Contracts;

/// <summary>
/// A service that can create and decrypt SAS (Shared Access Signature) tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a SAS (Shared Access Signature) token containing the specified data.
    /// </summary>
    string CreateToken<T>(T payload, TimeSpan lifetime);
    
    /// <summary>
    /// Creates a SAS (Shared Access Signature) token containing the specified data.
    /// </summary>
    string CreateToken<T>(T payload, DateTimeOffset expiresAt);
    
    /// <summary>
    /// Creates a SAS (Shared Access Signature) token containing the specified data.
    /// </summary>
    string CreateToken<T>(T payload);

    /// <summary>
    /// Decrypts the specified SAS token.
    /// </summary>
    T DecryptToken<T>(string token);

    /// <summary>
    /// Decrypts the specified SAS token.
    /// </summary>
    bool TryDecryptToken<T>(string token, out T payload);
}