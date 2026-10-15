using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

public interface IConnectionLifecycleStore
{
    Task CreateAsync(IntegrationConnection connection, CancellationToken cancellationToken = default);

    Task<IntegrationConnection?> FindAsync(string id, string tenantId, string environmentId, CancellationToken cancellationToken = default);

    Task<ConnectionGenerationCleanup?> FindGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, CancellationToken cancellationToken = default);

    Task<ConnectionGenerationCleanup?> TryClaimGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, long expectedRevision, string generationId, DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken = default);

    Task<bool> CompleteGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default);

    Task<bool> CancelGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default);

    Task<IntegrationConnection?> TryDisconnectAndRecordAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        ConnectionOffboardingOperation operation,
        CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperation?> FindOffboardingOperationAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperation?> FindNextOffboardingOperationAsync(
        string tenantId,
        string environmentId,
        string connectionId,
        DateTimeOffset now,
        bool stableRevocationIdIdempotency,
        bool stableUninstallIdIdempotency,
        CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperation?> TryQueueOffboardingOperationAsync(
        long expectedConnectionRevision,
        ConnectionOffboardingOperation operation,
        CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperation?> TryClaimOffboardingOperationAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long expectedFence,
        DateTimeOffset now,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryStartOffboardingProviderCallAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkOffboardingOutcomeUnknownIfLeaseExpiredAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset now,
        string safeErrorCode,
        CancellationToken cancellationToken = default);

    Task<bool> TryReleaseOffboardingClaimAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset updatedAt,
        DateTimeOffset nextAttemptAt,
        string safeErrorCode,
        CancellationToken cancellationToken = default);

    Task<bool> TryCompleteOffboardingOperationAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryRecordOffboardingFailureAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        long fence,
        ConnectionOffboardingOperationStatus status,
        DateTimeOffset updatedAt,
        DateTimeOffset? nextAttemptAt,
        string safeErrorCode,
        CancellationToken cancellationToken = default);

    Task<IntegrationConnection?> TryClaimRefreshAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default);

    Task<IntegrationConnection?> TryClaimCredentialUpdateAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken = default) =>
        TryClaimRefreshAsync(id, tenantId, environmentId, expectedRevision, operationId, leaseExpiresAt, cancellationToken);

    Task<bool> TryAcceptCredentialUpdateAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<bool> TryReleaseUnstartedRefreshAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default);

    Task<bool> TryReleaseExpiredRefreshClaimAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default);

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

    Task<bool> TryRecordStagedGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string secretName,
        string generationId,
        ConnectionCredentialKind? credentialKind,
        DateTimeOffset? credentialExpiresAt,
        CancellationToken cancellationToken = default) =>
        TryRecordStagedGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, secretName, generationId, cancellationToken);

    Task<bool> TryPublishGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default);

    Task<bool> TryPromoteRecoveryGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default);

    Task<bool> TryPromoteRecoveryGenerationAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        ConnectionCredentialKind? credentialKind,
        DateTimeOffset? credentialExpiresAt,
        CancellationToken cancellationToken = default) =>
        TryPromoteRecoveryGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken);

    Task<bool> MarkRecoveryRequiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default);

    Task<bool> TryMarkRecoveryRequiredIfLeaseExpiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default);

    Task<bool> TryRestoreSourceGenerationAfterMissingPlanAsync(
        string id,
        string tenantId,
        string environmentId,
        long expectedRevision,
        string operationId,
        long fence,
        string sourceGenerationId,
        string safeErrorCode,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    Task<bool> TryDisconnectAsync(string id, string tenantId, string environmentId, long expectedRevision, CancellationToken cancellationToken = default);
}
