using Elsa.Connections.Contracts;

namespace Elsa.Connections.Credentials.Workflows.Services;

public sealed class DenyAllConnectionCredentialBindingUseAuthorizer : IConnectionCredentialBindingUseAuthorizer
{
    public Task<bool> AuthorizeAsync(ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}

public sealed class DenyAllConnectionCredentialBindingManagementAuthorizer : IConnectionCredentialBindingManagementAuthorizer
{
    public Task<bool> AuthorizeAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        ConnectionCredentialBindingManagementRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}
