namespace Elsa.Secrets.Management;

public interface IEncryptionKeyProvider
{
    Task<EncryptionKey> GetAsync(string id, CancellationToken cancellationToken = default);
}