using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

public interface IUserStore
{
    Task SaveAsync(User user, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> FindAsync(string userName, CancellationToken cancellationToken = default);
}