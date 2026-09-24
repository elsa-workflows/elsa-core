using Elsa.Connections.Contracts;
using Elsa.Connections.Models;

namespace Elsa.Connections.Credentials.Workflows.Services;

/// <summary>Checks the durable grant for the exact binding snapshot and workflow instance at each use.</summary>
public sealed class StoredConnectionCredentialBindingUseAuthorizer(
    IConnectionCredentialUseGrantStore store,
    IConnectionLifecycleStore? lifecycleStore)
    : IConnectionCredentialBindingUseAuthorizer
{
    public async Task<bool> AuthorizeAsync(
        ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default)
    {
        var grant = await store.FindAsync(request.TenantId, request.EnvironmentId, request.WorkflowInstanceId,
            request.LogicalBindingId, cancellationToken);
        if (lifecycleStore is null || grant is not { IsActive: true } || grant.ConnectionId != request.ConnectionId ||
            grant.BindingRevision != request.BindingRevision)
        {
            return false;
        }

        var connection = await lifecycleStore.FindAsync(request.ConnectionId, request.TenantId,
            request.EnvironmentId, cancellationToken);
        return connection?.Status == ConnectionStatus.Active;
    }
}
