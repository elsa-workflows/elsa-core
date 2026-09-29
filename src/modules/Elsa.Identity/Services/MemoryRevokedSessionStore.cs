using Elsa.Common.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

/// <summary>
/// An in-memory <see cref="IRevokedSessionStore"/>. Revocations are lost on restart and not shared between nodes.
/// </summary>
public class MemoryRevokedSessionStore(MemoryStore<RevokedSession> store) : IRevokedSessionStore
{
    /// <inheritdoc />
    public Task SaveAsync(RevokedSession revokedSession, CancellationToken cancellationToken = default)
    {
        store.Save(revokedSession, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(store.Any(x => x.Id == sessionId));
    }

    /// <inheritdoc />
    public Task DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        store.DeleteWhere(x => x.ExpiresAt < now);
        return Task.CompletedTask;
    }
}
