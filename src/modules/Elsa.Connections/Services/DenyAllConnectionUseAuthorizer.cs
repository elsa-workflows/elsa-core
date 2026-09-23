using Elsa.Connections.Contracts;

namespace Elsa.Connections.Services;

public sealed class DenyAllConnectionUseAuthorizer : IConnectionUseAuthorizer
{
    public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default) => Task.FromResult(false);
}
