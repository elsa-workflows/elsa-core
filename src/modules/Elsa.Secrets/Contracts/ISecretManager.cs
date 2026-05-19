namespace Elsa.Secrets.Contracts;

public interface ISecretManager
{
    Task<Secret> CreateAsync(CreateSecretRequest request, CancellationToken cancellationToken = default);
    Task<Secret?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Secret>> ListAsync(ListSecretsRequest request, CancellationToken cancellationToken = default);
    Task<long> CountAsync(ListSecretsRequest request, CancellationToken cancellationToken = default);
    Task<Secret> RotateAsync(string name, RotateSecretRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default);
    Task<SecretTestResponse> TestAsync(string name, CancellationToken cancellationToken = default);
    Task<SecretPayload> ResolvePayloadAsync(string name, CancellationToken cancellationToken = default);
}
