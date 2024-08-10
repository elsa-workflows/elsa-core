namespace Elsa.Secrets.Management.Contracts;

public interface IEncryptionKeyProvider
{
    Task<byte[]> GetKeyAsync(string id, CancellationToken cancellationToken = default);
}