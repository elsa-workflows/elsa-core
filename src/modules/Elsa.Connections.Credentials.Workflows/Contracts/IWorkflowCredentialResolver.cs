using Elsa.Connections.Models;
using Elsa.Workflows;

namespace Elsa.Connections.Credentials.Workflows.Contracts;

/// <summary>Resolves a logical binding for the current trusted workflow execution.</summary>
public interface IWorkflowCredentialResolver
{
    Task<ConnectionAccessCredential> ResolveAsync(
        WorkflowExecutionContext executionContext,
        string logicalBindingId,
        CancellationToken cancellationToken = default);
}

public sealed record ConnectionCredentialBindingResult(bool Succeeded, long? Revision, string? SafeErrorCode);

/// <summary>Creates or explicitly rebinds a logical reference in the current tenant and host environment.</summary>
public interface IWorkflowCredentialBindingManager
{
    Task<ConnectionCredentialBindingResult> CreateAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        string logicalBindingId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<ConnectionCredentialBindingResult> RebindAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        string logicalBindingId,
        long expectedRevision,
        string connectionId,
        CancellationToken cancellationToken = default);
}
