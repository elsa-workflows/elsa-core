namespace Elsa.Secrets.Management;

public interface IEncryptor
{
    Task<EncryptedValue> EncryptAsync(string keyId, string value, CancellationToken cancellationToken = default);
}