namespace Elsa.Secrets.Management;

/// <summary>
/// Encrypts a value using a specified key.
/// </summary>
public interface IEncryptor
{
    Task<EncryptedValue> EncryptAsync(string keyId, string value, CancellationToken cancellationToken = default);
}