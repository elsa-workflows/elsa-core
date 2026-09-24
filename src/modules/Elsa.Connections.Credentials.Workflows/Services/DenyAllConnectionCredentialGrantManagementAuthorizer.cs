using System.Security.Claims;
using Elsa.Connections.Contracts;

namespace Elsa.Connections.Credentials.Workflows.Services;

internal sealed class DenyAllConnectionCredentialGrantManagementAuthorizer : IConnectionCredentialGrantManagementAuthorizer
{
    public Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal, ConnectionCredentialGrantManagementRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
