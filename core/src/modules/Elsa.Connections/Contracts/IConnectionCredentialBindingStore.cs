using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

/// <summary>
/// Host-only persistence primitive for durable, revisioned logical workflow bindings. Do not expose it through
/// HTTP; application services must authorize the exact tenant, environment, logical reference and target connection
/// before creating or rebinding a mapping.
/// </summary>
/// <remarks>
/// A binding selects a connection but does not grant use. A connection can disconnect after the store's active-state
/// check and leave a stale mapping; every resolution must still pass through the lifecycle service, which checks the
/// current connection status before returning a credential.
/// </remarks>
public interface IConnectionCredentialBindingStore
{
    Task<ConnectionCredentialBinding?> FindAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        CancellationToken cancellationToken = default);

    Task<ConnectionCredentialBinding?> TryCreateAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<ConnectionCredentialBinding?> TryRebindAsync(
        string tenantId,
        string environmentId,
        string logicalBindingId,
        long expectedRevision,
        string connectionId,
        CancellationToken cancellationToken = default);
}

public sealed record ConnectionCredentialBindingUseRequest(
    string TenantId,
    string EnvironmentId,
    string LogicalBindingId,
    string ConnectionId,
    long BindingRevision,
    string WorkflowInstanceId);

/// <summary>Host authorization for a workflow's use of the binding snapshot resolved by the adapter.</summary>
public interface IConnectionCredentialBindingUseAuthorizer
{
    Task<bool> AuthorizeAsync(ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default);
}

public sealed record ConnectionCredentialBindingManagementRequest(
    string TenantId,
    string EnvironmentId,
    string LogicalBindingId,
    string ConnectionId,
    long? ExpectedRevision);

/// <summary>Host authorization for a human creating or explicitly rebinding a logical connection mapping.</summary>
public interface IConnectionCredentialBindingManagementAuthorizer
{
    Task<bool> AuthorizeAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        ConnectionCredentialBindingManagementRequest request,
        CancellationToken cancellationToken = default);
}
