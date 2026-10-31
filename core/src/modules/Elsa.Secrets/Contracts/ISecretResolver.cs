namespace Elsa.Secrets.Contracts;

public interface ISecretResolver
{
    Task<string> ResolveAsync(string name, CancellationToken cancellationToken = default);
    Task<string> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default);
}
