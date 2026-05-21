namespace Elsa.Secrets.Contracts;

/// <summary>
/// Backward-compatible adapter surface for existing extension consumers.
/// </summary>
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}
