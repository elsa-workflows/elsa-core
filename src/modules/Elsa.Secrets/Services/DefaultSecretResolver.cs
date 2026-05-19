namespace Elsa.Secrets.Services;

public class DefaultSecretResolver(ISecretManager secretManager) : ISecretResolver
{
    public Task<string> ResolveAsync(string name, CancellationToken cancellationToken = default) => ResolveAsync(new SecretReference(name), cancellationToken);

    public async Task<string> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default)
    {
        if (secretManager is not DefaultSecretManager manager)
            throw new InvalidOperationException("The configured secret manager does not expose runtime resolution.");

        var secret = await secretManager.GetAsync(reference.Name, cancellationToken);
        if (secret == null)
            throw new InvalidOperationException($"Secret '{reference.Name}' was not found.");

        if (!string.IsNullOrWhiteSpace(reference.TypeName) && !string.Equals(secret.TypeName, reference.TypeName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Secret '{reference.Name}' is not compatible with required type '{reference.TypeName}'.");

        if (!string.IsNullOrWhiteSpace(reference.Scope) && !string.Equals(secret.Scope, reference.Scope, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Secret '{reference.Name}' is not compatible with required scope '{reference.Scope}'.");

        var payload = await manager.ResolvePayloadAsync(reference.Name, cancellationToken);
        return payload.Value!;
    }
}
