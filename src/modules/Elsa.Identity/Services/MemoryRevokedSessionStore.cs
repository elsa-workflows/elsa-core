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
    public Task AddOrExtendAsync(RevokedSession revokedSession, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var existing = store.Find(x => x.Id == revokedSession.Id);

            if (existing is null)
                store.Add(revokedSession, x => x.Id);
            else if (existing.ExpiresAt < revokedSession.ExpiresAt)
                existing.ExpiresAt = revokedSession.ExpiresAt;
        }

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
        lock (store.Sync)
            store.DeleteWhere(x => x.ExpiresAt < now);

        return Task.CompletedTask;
    }
}
