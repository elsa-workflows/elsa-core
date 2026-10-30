namespace Elsa.Secrets.Management;

/// <summary>
/// Encrypts and decrypts secret values.
/// </summary>
public interface ISecretEncryptor
{
    Task<Secret> EncryptAsync(SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Encrypts the specified value and stores it in the specified secret.
    /// </summary>
    Task EncryptAsync(Secret secret, SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Decrypts the specified secret and returns the value.
    /// </summary>
    Task<string> DecryptAsync(Secret secret, CancellationToken cancellationToken = default);
}