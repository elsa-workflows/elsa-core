namespace Elsa.Secrets.Contracts;

public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}