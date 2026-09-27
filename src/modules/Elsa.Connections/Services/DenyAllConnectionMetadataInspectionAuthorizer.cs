using System.Security.Claims;
using Elsa.Connections.Contracts;

namespace Elsa.Connections.Services;

internal sealed class DenyAllConnectionMetadataInspectionAuthorizer : IConnectionMetadataInspectionAuthorizer
{
    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionMetadataInspectionRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
