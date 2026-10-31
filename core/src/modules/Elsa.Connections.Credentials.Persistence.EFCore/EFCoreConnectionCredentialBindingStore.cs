using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

/// <summary>Persists logical workflow bindings beside lifecycle state with compare-and-swap rebinds.</summary>
public sealed class EFCoreConnectionCredentialBindingStore(
    IDbContextFactory<ConnectionsElsaDbContext> dbContextFactory,
    IConnectionCredentialBindingConflictClassifier conflictClassifier)
    : IConnectionCredentialBindingStore
{
    public async Task<ConnectionCredentialBinding?> FindAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await FindQuery(db, tenantId, environmentId, logicalBindingId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ConnectionCredentialBinding?> TryCreateAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await IsActiveConnectionAsync(db, tenantId, environmentId, connectionId, cancellationToken) ||
            await FindQuery(db, tenantId, environmentId, logicalBindingId).AnyAsync(cancellationToken))
        {
            return null;
        }

        var binding = new ConnectionCredentialBinding
        {
            TenantId = tenantId,
            EnvironmentId = environmentId,
            LogicalBindingId = logicalBindingId,
            ConnectionId = connectionId,
            Revision = 1
        };
        db.CredentialBindings.Add(binding);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            !cancellationToken.IsCancellationRequested && conflictClassifier.IsDuplicateBindingKey(exception))
        {
            db.Entry(binding).State = EntityState.Detached;
            return null;
        }

        return binding;
    }

    public async Task<ConnectionCredentialBinding?> TryRebindAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        long expectedRevision,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (expectedRevision < 1 || expectedRevision == long.MaxValue)
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await IsActiveConnectionAsync(db, tenantId, environmentId, connectionId, cancellationToken))
        {
            return null;
        }

        var changed = await FindQuery(db, tenantId, environmentId, logicalBindingId)
            .Where(x => x.Revision == expectedRevision)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ConnectionId, connectionId)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
        if (changed != 1)
        {
            return null;
        }

        return new ConnectionCredentialBinding
        {
            TenantId = tenantId,
            EnvironmentId = environmentId,
            LogicalBindingId = logicalBindingId,
            ConnectionId = connectionId,
            Revision = expectedRevision + 1
        };
    }

    private static IQueryable<ConnectionCredentialBinding> FindQuery(
        ConnectionsElsaDbContext db,
        string tenantId,
        string environmentId,
        string logicalBindingId) =>
        db.CredentialBindings.Where(x => x.TenantId == tenantId && x.EnvironmentId == environmentId && x.LogicalBindingId == logicalBindingId);

    private static Task<bool> IsActiveConnectionAsync(
        ConnectionsElsaDbContext db,
        string tenantId,
        string environmentId,
        string connectionId,
        CancellationToken cancellationToken) =>
        db.Connections.AnyAsync(x => x.Id == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId && x.Status == ConnectionStatus.Active, cancellationToken);

}
