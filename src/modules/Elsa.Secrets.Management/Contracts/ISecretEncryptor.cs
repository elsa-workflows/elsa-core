namespace Elsa.Secrets.Management;

/// Encrypts and decrypts secret values.
public interface ISecretEncryptor
{
    Task<Secret> EncryptAsync(SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// Encrypts the specified value and stores it in the specified secret.
    Task EncryptAsync(Secret secret, SecretInputModel input, CancellationToken cancellationToken = default);
    
    /// Decrypts the specified secret and returns the value.
    Task<string> DecryptAsync(Secret secret, CancellationToken cancellationToken = default);
}