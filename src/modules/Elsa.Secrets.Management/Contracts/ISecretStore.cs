using Elsa.Common.Models;

namespace Elsa.Secrets.Management;

public interface ISecretStore
{
    Task<Page<Secret>> FindManyAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task<Secret?> FindAsync<TOrderBy>(SecretFilter filter, SecretOrder<TOrderBy> order, CancellationToken cancellationToken = default);
}