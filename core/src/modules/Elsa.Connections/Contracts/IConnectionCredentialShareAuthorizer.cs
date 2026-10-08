using System.Security.Claims;

namespace Elsa.Connections.Contracts;

/// <summary>The host's separate decision to share one named connection with one workflow instance.</summary>
public interface IConnectionCredentialShareAuthorizer
{
    Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal,
        ConnectionCredentialShareRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ConnectionCredentialShareRequest(
    string TenantId,
    string EnvironmentId,
    string WorkflowInstanceId,
    string LogicalBindingId,
    string ConnectionId,
    long BindingRevision);
