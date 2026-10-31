using Elsa.Connections.Contracts;

namespace Elsa.Connections.Credentials.Workflows.Services;

/// <summary>Allows the separate durable grant policy to decide use when the host has no additional use policy.</summary>
public sealed class AllowGrantControlledConnectionCredentialBindingUseAuthorizer : IConnectionCredentialBindingUseAuthorizer
{
    public Task<bool> AuthorizeAsync(ConnectionCredentialBindingUseRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
