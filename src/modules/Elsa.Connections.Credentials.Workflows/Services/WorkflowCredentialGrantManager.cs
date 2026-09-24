using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Credentials.Workflows.Services;

/// <summary>Issues and withdraws exact use grants after a separate host policy decision.</summary>
public sealed class WorkflowCredentialGrantManager(
    IConnectionCredentialBindingStore bindingStore,
    IConnectionCredentialUseGrantStore grantStore,
    IConnectionCredentialGrantManagementAuthorizer authorizer,
    IOptions<WorkflowCredentialBindingOptions> options,
    ITenantAccessor tenantAccessor,
    TimeProvider timeProvider) : IWorkflowCredentialGrantManager
{
    public async Task<WorkflowCredentialGrantResult> IssueAsync(
        ClaimsPrincipal principal, string workflowInstanceId, string logicalBindingId,
        long expectedBindingRevision, CancellationToken cancellationToken = default)
    {
        if (!TryGetScope(principal, workflowInstanceId, logicalBindingId, out var tenantId, out var environmentId) ||
            expectedBindingRevision < 1)
        {
            return Unavailable();
        }

        var binding = await bindingStore.FindAsync(tenantId, environmentId, logicalBindingId, cancellationToken);
        if (binding is null || binding.Revision != expectedBindingRevision)
        {
            return Unavailable();
        }

        var request = new ConnectionCredentialGrantManagementRequest(tenantId, environmentId, workflowInstanceId,
            logicalBindingId, binding.ConnectionId, binding.Revision, ConnectionCredentialGrantAction.Issue);
        if (!await authorizer.AuthorizeAsync(principal, request, cancellationToken))
        {
            return Unavailable();
        }

        var actorId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId) || actorId.Length > 200)
        {
            return Unavailable();
        }

        var grant = await grantStore.TryIssueAsync(tenantId, environmentId, workflowInstanceId, logicalBindingId,
            binding.ConnectionId, binding.Revision, actorId, timeProvider.GetUtcNow(), cancellationToken);
        return grant is null ? Unavailable() : new(true, grant.Revision, null);
    }

    public async Task<WorkflowCredentialGrantResult> WithdrawAsync(
        ClaimsPrincipal principal, string workflowInstanceId, string logicalBindingId,
        long expectedGrantRevision, CancellationToken cancellationToken = default)
    {
        if (!TryGetScope(principal, workflowInstanceId, logicalBindingId, out var tenantId, out var environmentId) ||
            expectedGrantRevision < 1 || expectedGrantRevision == long.MaxValue)
        {
            return Unavailable();
        }

        var grant = await grantStore.FindAsync(tenantId, environmentId, workflowInstanceId, logicalBindingId, cancellationToken);
        if (grant is null || !grant.IsActive || grant.Revision != expectedGrantRevision)
        {
            return Unavailable();
        }

        var request = new ConnectionCredentialGrantManagementRequest(tenantId, environmentId, workflowInstanceId,
            logicalBindingId, grant.ConnectionId, grant.BindingRevision, ConnectionCredentialGrantAction.Withdraw);
        if (!await authorizer.AuthorizeAsync(principal, request, cancellationToken) ||
            !await grantStore.TryWithdrawAsync(tenantId, environmentId, workflowInstanceId, logicalBindingId,
                expectedGrantRevision, timeProvider.GetUtcNow(), cancellationToken))
        {
            return Unavailable();
        }

        return new(true, expectedGrantRevision + 1, null);
    }

    private bool TryGetScope(
        ClaimsPrincipal principal, string workflowInstanceId, string logicalBindingId,
        out string tenantId, out string environmentId)
    {
        tenantId = tenantAccessor.TenantId;
        environmentId = options.Value.EnvironmentId ?? string.Empty;
        return principal.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(tenantId) &&
               tenantId != Tenant.DefaultTenantId && tenantId != Tenant.AgnosticTenantId && tenantId.Length <= 200 &&
               !string.IsNullOrWhiteSpace(environmentId) && environmentId.Length <= 200 &&
               !string.IsNullOrWhiteSpace(workflowInstanceId) && workflowInstanceId.Length <= 200 &&
               !string.IsNullOrWhiteSpace(logicalBindingId) && logicalBindingId.Length <= 200;
    }

    private static WorkflowCredentialGrantResult Unavailable() => new(false, null, "connection_unavailable");
}
