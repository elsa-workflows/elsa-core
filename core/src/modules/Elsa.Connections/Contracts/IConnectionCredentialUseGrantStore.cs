using System.Security.Claims;
using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

/// <summary>Host-only durable grant storage. A grant never carries credential material.</summary>
public interface IConnectionCredentialUseGrantStore
{
    Task<ConnectionCredentialUseGrant?> FindAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        CancellationToken cancellationToken = default);

    Task<ConnectionCredentialUseGrant?> TryIssueAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        string connectionId, long bindingRevision, string actorId, DateTimeOffset issuedAt,
        CancellationToken cancellationToken = default);

    Task<bool> TryWithdrawAsync(
        string tenantId, string environmentId, string workflowInstanceId, string logicalBindingId,
        long expectedRevision, DateTimeOffset withdrawnAt,
        CancellationToken cancellationToken = default);
}

public enum ConnectionCredentialGrantAction
{
    Issue,
    Withdraw
}

public sealed record ConnectionCredentialGrantManagementRequest(
    string TenantId,
    string EnvironmentId,
    string WorkflowInstanceId,
    string LogicalBindingId,
    string ConnectionId,
    long BindingRevision,
    ConnectionCredentialGrantAction Action);

/// <summary>Host policy for issuing or withdrawing workflow-use grants; connection management does not imply this permission.</summary>
public interface IConnectionCredentialGrantManagementAuthorizer
{
    Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal,
        ConnectionCredentialGrantManagementRequest request,
        CancellationToken cancellationToken = default);
}
