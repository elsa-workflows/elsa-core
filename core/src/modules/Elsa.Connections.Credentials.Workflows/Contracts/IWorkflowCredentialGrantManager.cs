using System.Security.Claims;

namespace Elsa.Connections.Credentials.Workflows.Contracts;

/// <summary>Host-facing management of exact workflow-instance credential-use grants.</summary>
public interface IWorkflowCredentialGrantManager
{
    Task<WorkflowCredentialGrantResult> IssueAsync(
        ClaimsPrincipal principal, string workflowInstanceId, string logicalBindingId,
        long expectedBindingRevision, CancellationToken cancellationToken = default);

    Task<WorkflowCredentialGrantResult> WithdrawAsync(
        ClaimsPrincipal principal, string workflowInstanceId, string logicalBindingId,
        long expectedGrantRevision, CancellationToken cancellationToken = default);
}

public sealed record WorkflowCredentialGrantResult(bool Succeeded, long? Revision, string? SafeErrorCode);
