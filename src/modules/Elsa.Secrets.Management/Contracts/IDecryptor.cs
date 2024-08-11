namespace Elsa.Secrets.Management;

public interface IDecryptor
{
    Task<string> DecryptAsync(EncryptedValue encryptedValue, CancellationToken cancellationToken = default);
}