namespace Elsa.Secrets.Management;

public interface IDecryptor
{
    Task<string> DecryptAsync(string encryptedValue, CancellationToken cancellationToken = default);
}