using System.Security.Claims;
using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

public interface IConnectionLifecycleService
{
    Task<ConnectionLifecycleResult> ConnectAsync(ClaimsPrincipal principal, ConnectConnectionRequest request, CancellationToken cancellationToken = default);

    Task<ConnectionAccessCredential> ResolveForUseAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);

    Task<ConnectionLifecycleResult> RefreshAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);
}

/// <summary>Host-owned background recovery contract. It receives no caller identity or caller-selectable system kind.</summary>
public interface IConnectionLifecycleRecoveryService
{
    Task<ConnectionLifecycleResult> ReconcileAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default);
}

public sealed record ConnectConnectionRequest(string TenantId, string EnvironmentId, string ProviderId, string ProviderAccountId, Elsa.Connections.Models.CredentialMaterial InitialCredentials);

public sealed record ConnectionLifecycleResult(bool Succeeded, string? SafeErrorCode, long? Revision, string? ConnectionId = null);
