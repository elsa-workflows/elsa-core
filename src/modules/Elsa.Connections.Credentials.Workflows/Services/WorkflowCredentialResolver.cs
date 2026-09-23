using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Workflows;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Credentials.Workflows.Services;

public sealed class WorkflowCredentialResolver(
    IConnectionCredentialBindingStore bindingStore,
    IConnectionCredentialBindingUseAuthorizer authorizer,
    IConnectionBackgroundUseService credentialService,
    IOptions<WorkflowCredentialBindingOptions> options,
    ITenantAccessor tenantAccessor) : IWorkflowCredentialResolver
{
    public async Task<ConnectionAccessCredential> ResolveAsync(
        WorkflowExecutionContext executionContext,
        string logicalBindingId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.TenantId;
        var environmentId = options.Value.EnvironmentId;
        if (executionContext is null || string.IsNullOrWhiteSpace(executionContext.Id) ||
            string.IsNullOrWhiteSpace(tenantId) || tenantId == Tenant.DefaultTenantId || tenantId == Tenant.AgnosticTenantId ||
            tenantId.Length > 200 || string.IsNullOrWhiteSpace(environmentId) || environmentId.Length > 200 ||
            string.IsNullOrWhiteSpace(logicalBindingId) || logicalBindingId.Length > 200)
        {
            throw new ConnectionUnavailableException();
        }

        var binding = await bindingStore.FindAsync(tenantId, environmentId, logicalBindingId, cancellationToken);
        if (binding is null)
        {
            throw new ConnectionUnavailableException();
        }

        var request = new ConnectionCredentialBindingUseRequest(
            tenantId,
            environmentId,
            logicalBindingId,
            binding.ConnectionId,
            binding.Revision,
            executionContext.Id);
        if (!await authorizer.AuthorizeAsync(request, cancellationToken))
        {
            throw new ConnectionUnavailableException();
        }

        var current = await bindingStore.FindAsync(tenantId, environmentId, logicalBindingId, cancellationToken);
        if (current is null || current.Revision != binding.Revision || current.ConnectionId != binding.ConnectionId)
        {
            throw new ConnectionUnavailableException();
        }

        // A rebind after this authorization recheck does not revoke an already admitted activity call. New
        // resolutions always read the latest revision; disconnect is independently enforced by the lifecycle service.
        return await credentialService.ResolveForUseAsync(tenantId, environmentId, binding.ConnectionId, cancellationToken);
    }
}
