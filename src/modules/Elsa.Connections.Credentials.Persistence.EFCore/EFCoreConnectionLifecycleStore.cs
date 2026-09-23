using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.EFCore;

/// <summary>Database-owned lifecycle state. Every mutation is a scoped conditional update for cross-worker CAS.</summary>
public sealed class EFCoreConnectionLifecycleStore(IDbContextFactory<ConnectionsElsaDbContext> dbContextFactory) : IConnectionLifecycleStore
{
    public async Task CreateAsync(IntegrationConnection connection, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Connections.Add(connection);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IntegrationConnection?> FindAsync(string id, string tenantId, string environmentId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ConnectionGenerationCleanup?> FindGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GenerationCleanups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId && x.GenerationId == generationId, cancellationToken);
    }

    public async Task<ConnectionGenerationCleanup?> TryClaimGenerationCleanupAsync(
        string connectionId,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string generationId,
        DateTimeOffset now,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var cleanup = await db.GenerationCleanups.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId && x.GenerationId == generationId, cancellationToken);

        if (cleanup?.Status == ConnectionGenerationCleanupStatus.Deleted ||
            cleanup != null && cleanup.Status == ConnectionGenerationCleanupStatus.Deleting && cleanup.LeaseExpiresAt.HasValue && cleanup.LeaseExpiresAt.Value > now)
            return null;

        var rows = await Scoped(db, connectionId, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.CurrentGenerationId != generationId &&
                        (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed ||
                         (x.OperationSourceGenerationId != generationId && x.PlannedGenerationId != generationId && x.StagedGenerationId != generationId)))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);

        if (rows != 1)
            return null;

        if (cleanup == null)
        {
            cleanup = new ConnectionGenerationCleanup
            {
                ConnectionId = connectionId,
                TenantId = tenantId,
                EnvironmentId = environmentId,
                GenerationId = generationId,
                Status = ConnectionGenerationCleanupStatus.Deleting,
                Fence = 1,
                LeaseExpiresAt = leaseExpiresAt
            };
            db.GenerationCleanups.Add(cleanup);
        }
        else
        {
            var updated = await db.GenerationCleanups
                .Where(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                            x.GenerationId == generationId && x.Status == ConnectionGenerationCleanupStatus.Deleting &&
                            x.Fence == cleanup.Fence && x.LeaseExpiresAt <= now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Fence, x => x.Fence + 1)
                    .SetProperty(x => x.LeaseExpiresAt, leaseExpiresAt), cancellationToken);
            if (updated != 1)
                return null;
            cleanup.Fence++;
            cleanup.LeaseExpiresAt = leaseExpiresAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return cleanup;
    }

    public async Task<bool> CompleteGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GenerationCleanups
            .Where(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.GenerationId == generationId && x.Fence == fence && x.Status == ConnectionGenerationCleanupStatus.Deleting)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionGenerationCleanupStatus.Deleted)
                .SetProperty(x => x.LeaseExpiresAt, (DateTimeOffset?)null), cancellationToken) == 1;
    }

    public async Task<bool> CancelGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GenerationCleanups
            .Where(x => x.ConnectionId == connectionId && x.TenantId == tenantId && x.EnvironmentId == environmentId &&
                        x.GenerationId == generationId && x.Fence == fence && x.Status == ConnectionGenerationCleanupStatus.Deleting)
            .ExecuteDeleteAsync(cancellationToken) == 1;
    }

    public async Task<IntegrationConnection?> TryClaimRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.Status == ConnectionStatus.Active &&
                        (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed) &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == operationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Revision, x => x.Revision + 1)
                .SetProperty(x => x.OperationId, operationId)
                .SetProperty(x => x.OperationExpectedRevision, expectedRevision + 1)
                .SetProperty(x => x.OperationFence, x => x.OperationFence + 1)
                .SetProperty(x => x.OperationLeaseExpiresAt, leaseExpiresAt)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Claimed)
                .SetProperty(x => x.OperationSourceGenerationId, x => x.CurrentGenerationId)
                .SetProperty(x => x.PlannedGenerationId, operationId)
                .SetProperty(x => x.PlannedSecretName, ManagedSecretNames.ForGeneration(id, operationId))
                .SetProperty(x => x.StagedSecretName, (string?)null)
                .SetProperty(x => x.StagedGenerationId, (string?)null)
                .SetProperty(x => x.LastSafeErrorCode, (string?)null), cancellationToken);
        if (rows != 1)
            return null;

        return await Scoped(db, id, tenantId, environmentId).AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
    }

    public async Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.Status == ConnectionStatus.Active && x.OperationStatus == CredentialOperationStatus.Claimed &&
                        x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OperationStatus, CredentialOperationStatus.ProviderCallStarted), cancellationToken) == 1;
    }

    public Task<bool> TryReleaseUnstartedRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        string operationId,
        long fence,
        string safeErrorCode,
        CancellationToken cancellationToken = default) =>
        ReleaseUnstartedRefreshAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, null, cancellationToken);

    public Task<bool> TryReleaseExpiredRefreshClaimAsync(
        string id,
        string tenantId,
        string environmentId,
        string operationId,
        long fence,
        DateTimeOffset now,
        string safeErrorCode,
        CancellationToken cancellationToken = default) =>
        ReleaseUnstartedRefreshAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, now, cancellationToken);

    private async Task<bool> ReleaseUnstartedRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        string operationId,
        long fence,
        string safeErrorCode,
        DateTimeOffset? expiredAt,
        CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = Scoped(db, id, tenantId, environmentId)
            .Where(x => x.OperationId == operationId && x.OperationFence == fence &&
                        (x.Status == ConnectionStatus.Active || x.Status == ConnectionStatus.Disconnected) &&
                        x.OperationStatus == CredentialOperationStatus.Claimed && x.CurrentGenerationId == x.OperationSourceGenerationId);

        if (expiredAt.HasValue)
            query = query.Where(x => x.Status == ConnectionStatus.Disconnected ||
                                     x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt <= expiredAt.Value);

        return await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Revision, x => x.Revision + 1)
            .SetProperty(x => x.OperationId, (string?)null)
            .SetProperty(x => x.OperationExpectedRevision, 0)
            .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
            .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
            .SetProperty(x => x.OperationSourceGenerationId, (string?)null)
            .SetProperty(x => x.PlannedSecretName, (string?)null)
            .SetProperty(x => x.PlannedGenerationId, (string?)null)
            .SetProperty(x => x.StagedSecretName, (string?)null)
            .SetProperty(x => x.StagedGenerationId, (string?)null)
            .SetProperty(x => x.LastSafeErrorCode, safeErrorCode), cancellationToken) == 1;
    }

    public async Task<bool> TryRecordStagedGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string secretName,
        string generationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            // Staging deliberately ignores current Connection.Revision and Status: disconnect or another state
            // change must not erase the durable fact that the provider may have rotated its one-time token.
            .Where(x => x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.PlannedSecretName == secretName && x.PlannedGenerationId == generationId &&
                        (x.OperationStatus == CredentialOperationStatus.ProviderCallStarted || x.OperationStatus == CredentialOperationStatus.CredentialReceived) &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == generationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StagedSecretName, secretName)
                .SetProperty(x => x.StagedGenerationId, generationId)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Staged), cancellationToken) == 1;
    }

    public async Task<bool> TryPublishGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.Status == ConnectionStatus.Active && x.OperationStatus == CredentialOperationStatus.Staged &&
                        x.StagedSecretName != null && x.StagedGenerationId != null &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == x.StagedGenerationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CurrentSecretName, x => x.StagedSecretName)
                .SetProperty(x => x.CurrentGenerationId, x => x.StagedGenerationId)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    public async Task<bool> TryPromoteRecoveryGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.Status == ConnectionStatus.RecoveryRequired &&
                        x.OperationStatus == CredentialOperationStatus.RecoveryRequired && x.OperationId == operationId &&
                        x.OperationFence == fence && x.OperationExpectedRevision < expectedRevision &&
                        x.CurrentGenerationId == x.OperationSourceGenerationId && x.PlannedSecretName != null && x.PlannedGenerationId != null &&
                        (x.StagedSecretName == null || x.StagedSecretName == x.PlannedSecretName) &&
                        (x.StagedGenerationId == null || x.StagedGenerationId == x.PlannedGenerationId) &&
                        !db.GenerationCleanups.Any(cleanup => cleanup.ConnectionId == id && cleanup.GenerationId == x.PlannedGenerationId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CurrentSecretName, x => x.PlannedSecretName)
                .SetProperty(x => x.CurrentGenerationId, x => x.PlannedGenerationId)
                .SetProperty(x => x.Status, ConnectionStatus.Active)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.Completed)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.LastSafeErrorCode, (string?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    public async Task<bool> MarkRecoveryRequiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.OperationId == operationId && x.OperationFence == fence &&
                        x.OperationStatus != CredentialOperationStatus.Completed && x.OperationStatus != CredentialOperationStatus.RecoveryRequired)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, x => x.Status == ConnectionStatus.Active ? ConnectionStatus.RecoveryRequired : x.Status)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.RecoveryRequired)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.Revision, x => x.Revision + (x.Status == ConnectionStatus.Active ? 1 : 0)), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkRecoveryRequiredIfLeaseExpiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.OperationId == operationId && x.OperationFence == fence &&
                        x.OperationLeaseExpiresAt != null && x.OperationLeaseExpiresAt <= now &&
                        (x.OperationStatus == CredentialOperationStatus.ProviderCallStarted || x.OperationStatus == CredentialOperationStatus.CredentialReceived))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, x => x.Status == ConnectionStatus.Active ? ConnectionStatus.RecoveryRequired : x.Status)
                .SetProperty(x => x.OperationStatus, CredentialOperationStatus.RecoveryRequired)
                .SetProperty(x => x.LastSafeErrorCode, safeErrorCode)
                .SetProperty(x => x.OperationLeaseExpiresAt, (DateTimeOffset?)null)
                .SetProperty(x => x.Revision, x => x.Revision + (x.Status == ConnectionStatus.Active ? 1 : 0)), cancellationToken) == 1;
    }

    public async Task<bool> TryDisconnectAsync(string id, string tenantId, string environmentId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && (x.Status == ConnectionStatus.Active || x.Status == ConnectionStatus.RecoveryRequired))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ConnectionStatus.Disconnected)
                .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken) == 1;
    }

    private static IQueryable<IntegrationConnection> Scoped(ConnectionsElsaDbContext db, string id, string tenantId, string environmentId) =>
        db.Connections.Where(x => x.Id == id && x.TenantId == tenantId && x.EnvironmentId == environmentId);
}
