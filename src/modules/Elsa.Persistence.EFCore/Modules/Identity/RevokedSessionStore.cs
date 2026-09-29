using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Identity;

/// <summary>
/// An EF Core implementation of <see cref="IRevokedSessionStore"/>.
/// </summary>
public class EFCoreRevokedSessionStore(EntityStore<IdentityElsaDbContext, RevokedSession> store) : IRevokedSessionStore
{
    /// <inheritdoc />
    public async Task AddOrExtendAsync(RevokedSession revokedSession, CancellationToken cancellationToken = default)
    {
        // Each write is conditional in the database, so a concurrent revocation of the same session can never replace
        // a later expiry with an earlier one. An insert that loses to a concurrent one goes back to extending it.
        while (!await TryExtendAsync(revokedSession, cancellationToken))
        {
            if (await TryAddAsync(revokedSession, cancellationToken))
                return;
        }
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

    // Whether the session is now revoked until at least the specified expiry; false when it has no revocation.
    private async Task<bool> TryExtendAsync(RevokedSession revokedSession, CancellationToken cancellationToken)
    {
        var expiresAt = revokedSession.ExpiresAt;
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var revocation = dbContext.RevokedSessions.Where(x => x.Id == revokedSession.Id);
        var extended = await revocation.Where(x => x.ExpiresAt < expiresAt).ExecuteUpdateAsync(x => x.SetProperty(r => r.ExpiresAt, expiresAt), cancellationToken);

        return extended > 0 || await revocation.AnyAsync(x => x.ExpiresAt >= expiresAt, cancellationToken);
    }

    private async Task<bool> TryAddAsync(RevokedSession revokedSession, CancellationToken cancellationToken)
    {
        try
        {
            await store.AddAsync(revokedSession, cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (DbExceptionClassifier.IsDuplicateKey(exception))
        {
            return false;
        }
    }
}
