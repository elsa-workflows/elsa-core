namespace Elsa.Secrets.Management.Contracts;

public interface IMasterKeyProvider
{
    Task<byte[]> GetKeyAsync(CancellationToken cancellationToken = default);
}