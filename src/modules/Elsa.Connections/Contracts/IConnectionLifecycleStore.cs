using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

public interface IConnectionLifecycleStore
{
    Task CreateAsync(IntegrationConnection connection, CancellationToken cancellationToken = default);

    Task<IntegrationConnection?> FindAsync(string id, string tenantId, string environmentId, CancellationToken cancellationToken = default);

    Task<IntegrationConnection?> TryClaimRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default);

    Task<bool> TryRecordStagedGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string secretName,
        string generationId,
        CancellationToken cancellationToken = default);

    Task<bool> TryPublishGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default);

    Task<bool> TryPromoteRecoveryGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default);

    Task<bool> MarkRecoveryRequiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default);

    Task<bool> TryMarkRecoveryRequiredIfLeaseExpiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default);

    Task<bool> TryDisconnectAsync(string id, string tenantId, string environmentId, long expectedRevision, CancellationToken cancellationToken = default);
}
