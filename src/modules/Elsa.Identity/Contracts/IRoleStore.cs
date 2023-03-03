using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

public interface IRoleStore
{
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}