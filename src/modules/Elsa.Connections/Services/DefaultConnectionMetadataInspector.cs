using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Features;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Services;

/// <summary>Separates metadata inspection from connection management and credential use.</summary>
public sealed class DefaultConnectionMetadataInspector(
    IConnectionLifecycleStore? store,
    IConnectionUseAuthorizer authorizer,
    ITenantAccessor tenantAccessor,
    IOptions<ConnectionInspectionOptions> options) : IConnectionMetadataInspector
{
    public async Task<ConnectionInspectionMetadata?> InspectAsync(
        ClaimsPrincipal principal, string connectionId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.TenantId;
        var environmentId = options.Value.EnvironmentId;
        if (store is null || principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(connectionId) || connectionId.Length > 200 ||
            string.IsNullOrWhiteSpace(tenantId) || tenantId is Tenant.DefaultTenantId or Tenant.AgnosticTenantId || tenantId.Length > 200 ||
            string.IsNullOrWhiteSpace(environmentId) || environmentId.Length > 200)
        {
            return null;
        }

        var request = new ConnectionUseRequest(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "inspect:metadata");
        if (!await authorizer.AuthorizeAsync(request, cancellationToken))
        {
            return null;
        }

        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        return connection is null
            ? null
            : new ConnectionInspectionMetadata(connection.Id, connection.ProviderId, connection.ProviderAccountId,
                connection.Status, connection.Revision);
    }
}
