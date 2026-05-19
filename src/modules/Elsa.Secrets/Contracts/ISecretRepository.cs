namespace Elsa.Secrets.Contracts;

public interface ISecretRepository
{
    Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Secret secret, CancellationToken cancellationToken = default);
    Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default);
    Task SaveAsync(Secret secret, CancellationToken cancellationToken = default);
}
