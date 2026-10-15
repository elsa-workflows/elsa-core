using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Credentials.Workflows.Services;

public sealed class WorkflowCredentialBindingManager(
    IConnectionCredentialBindingStore store,
    IConnectionCredentialBindingManagementAuthorizer authorizer,
    IOptions<WorkflowCredentialBindingOptions> options,
    ITenantAccessor tenantAccessor) : IWorkflowCredentialBindingManager
{
    public async Task<ConnectionCredentialBindingResult> CreateAsync(
        ClaimsPrincipal principal,
        string logicalBindingId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetScope(logicalBindingId, connectionId, out var tenantId, out var environmentId))
        {
            return Unavailable();
        }

        var request = new ConnectionCredentialBindingManagementRequest(tenantId, environmentId, logicalBindingId, connectionId, null);
        if (!await authorizer.AuthorizeAsync(principal, request, cancellationToken))
        {
            return Unavailable();
        }

        var binding = await store.TryCreateAsync(tenantId, environmentId, logicalBindingId, connectionId, cancellationToken);
        return binding is null ? Unavailable() : new(true, binding.Revision, null);
    }

    public async Task<ConnectionCredentialBindingResult> RebindAsync(
        ClaimsPrincipal principal,
        string logicalBindingId,
        long expectedRevision,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (expectedRevision < 1 || !TryGetScope(logicalBindingId, connectionId, out var tenantId, out var environmentId))
        {
            return Unavailable();
        }

        var request = new ConnectionCredentialBindingManagementRequest(tenantId, environmentId, logicalBindingId, connectionId, expectedRevision);
        if (!await authorizer.AuthorizeAsync(principal, request, cancellationToken))
        {
            return Unavailable();
        }

        var binding = await store.TryRebindAsync(tenantId, environmentId, logicalBindingId, expectedRevision, connectionId, cancellationToken);
        return binding is null ? Unavailable() : new(true, binding.Revision, null);
    }

    private bool TryGetScope(string logicalBindingId, string connectionId, out string tenantId, out string environmentId)
    {
        tenantId = tenantAccessor.TenantId;
        environmentId = options.Value.EnvironmentId ?? string.Empty;
        return !string.IsNullOrWhiteSpace(tenantId) && tenantId != Tenant.DefaultTenantId && tenantId != Tenant.AgnosticTenantId &&
               tenantId.Length <= 200 && !string.IsNullOrWhiteSpace(environmentId) && environmentId.Length <= 200 &&
               !string.IsNullOrWhiteSpace(logicalBindingId) && logicalBindingId.Length <= 200 &&
               !string.IsNullOrWhiteSpace(connectionId) && connectionId.Length <= 200;
    }

    private static ConnectionCredentialBindingResult Unavailable() => new(false, null, "connection_unavailable");
}
