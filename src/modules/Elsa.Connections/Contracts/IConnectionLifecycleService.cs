using System.Security.Claims;
using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

public interface IConnectionLifecycleService
{
    Task<ConnectionLifecycleResult> ConnectAsync(ClaimsPrincipal principal, ConnectConnectionRequest request, CancellationToken cancellationToken = default);

    Task<ConnectionAccessCredential> ResolveForUseAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);

    Task<ConnectionLifecycleResult> RefreshAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperationResult> DisconnectAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperationResult> RequestTokenRevocationAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, string generationId, CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperationResult> RequestInstallationUninstallAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);

    Task<ConnectionLifecycleResult> CleanupGenerationAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, string generationId, CancellationToken cancellationToken = default);
}

/// <summary>Lifecycle operations for static credentials that have no provider refresh-token exchange.</summary>
public interface IStaticApiKeyLifecycleService
{
    Task<ConnectionLifecycleResult> ConnectApiKeyAsync(ClaimsPrincipal principal, ConnectApiKeyConnectionRequest request, CancellationToken cancellationToken = default);

    Task<ConnectionLifecycleResult> ReplaceApiKeyAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, long expectedRevision, string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>Host-owned background recovery contract. It receives no caller identity or caller-selectable system kind.</summary>
public interface IConnectionLifecycleRecoveryService
{
    Task<ConnectionLifecycleResult> ReconcileAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);

    Task<ConnectionOffboardingOperationResult> ReconcileOffboardingAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);
}

public sealed record ConnectConnectionRequest(string TenantId, string EnvironmentId, string ProviderId, string ProviderAccountId, Elsa.Connections.Models.CredentialMaterial InitialCredentials);

public sealed record ConnectApiKeyConnectionRequest(string TenantId, string EnvironmentId, string ProviderId, string ProviderAccountId, string ApiKey)
{
    public override string ToString() => "ConnectApiKeyConnectionRequest { Redacted = true }";
}

/// <summary>Non-secret identity and lifecycle metadata returned to authorized connection managers.</summary>
public sealed record ConnectionLifecycleMetadata(
    string ConnectionId,
    string ProviderId,
    string ProviderAccountId,
    ConnectionStatus Status,
    long Revision,
    string? GenerationId);

public sealed record ConnectionLifecycleResult(
    bool Succeeded,
    string? SafeErrorCode,
    long? Revision,
    string? ConnectionId = null,
    ConnectionLifecycleMetadata? Connection = null);

public sealed record ConnectionOffboardingOperationResult(
    bool Accepted,
    string? SafeErrorCode,
    string? OperationId,
    ConnectionOffboardingOperationStatus? Status,
    long? ConnectionRevision);
