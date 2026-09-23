using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Persistence.EFCore;

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
                        (x.OperationStatus == CredentialOperationStatus.None || x.OperationStatus == CredentialOperationStatus.Completed))
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

    public async Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await Scoped(db, id, tenantId, environmentId)
            .Where(x => x.Revision == expectedRevision && x.OperationExpectedRevision == expectedRevision && x.OperationId == operationId &&
                        x.OperationFence == fence && x.Status == ConnectionStatus.Active && x.OperationStatus == CredentialOperationStatus.Claimed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.OperationStatus, CredentialOperationStatus.ProviderCallStarted), cancellationToken) == 1;
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
                        x.OperationFence == fence && (x.OperationStatus == CredentialOperationStatus.ProviderCallStarted || x.OperationStatus == CredentialOperationStatus.CredentialReceived))
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
                        x.StagedSecretName != null && x.StagedGenerationId != null)
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
                        (x.StagedGenerationId == null || x.StagedGenerationId == x.PlannedGenerationId))
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
                        x.OperationStatus != CredentialOperationStatus.Completed && x.OperationStatus != CredentialOperationStatus.RecoveryRequired)
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
