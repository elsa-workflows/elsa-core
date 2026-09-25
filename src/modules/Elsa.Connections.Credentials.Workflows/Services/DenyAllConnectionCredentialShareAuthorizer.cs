using System.Security.Claims;
using Elsa.Connections.Contracts;

namespace Elsa.Connections.Credentials.Workflows.Services;

internal sealed class DenyAllConnectionCredentialShareAuthorizer : IConnectionCredentialShareAuthorizer
{
    public Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionCredentialShareRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
