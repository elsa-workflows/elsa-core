using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Persistence.EFCore.Modules.Identity;

/// <summary>
/// An EF Core implementation of <see cref="IRevokedSessionStore"/>.
/// </summary>
public class EFCoreRevokedSessionStore(EntityStore<IdentityElsaDbContext, RevokedSession> store) : IRevokedSessionStore
{
    /// <inheritdoc />
    public async Task SaveAsync(RevokedSession revokedSession, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(revokedSession, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await store.AnyAsync(x => x.Id == sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await store.DeleteWhereAsync(x => x.ExpiresAt < now, cancellationToken);
    }
}
