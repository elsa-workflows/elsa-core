namespace Elsa.Secrets.Contracts;

public interface ISecretStore
{
    string Name { get; }
    SecretStoreDescriptor Descriptor { get; }
    Task<SecretPayload> WriteAsync(Secret secret, SecretVersion version, SecretPayload payload, CancellationToken cancellationToken = default);
    Task<SecretPayload?> ReadAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default);
    Task DeleteAsync(Secret secret, CancellationToken cancellationToken = default);
    Task<bool> TestAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default);
}
