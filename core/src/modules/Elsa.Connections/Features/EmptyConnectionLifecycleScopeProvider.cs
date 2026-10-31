using Elsa.Connections.Contracts;

namespace Elsa.Connections.Features;

internal sealed class EmptyConnectionLifecycleScopeProvider : IConnectionLifecycleScopeProvider
{
    public Task<ConnectionLifecycleScope?> GetNextScopeAsync(CancellationToken cancellationToken = default) => Task.FromResult<ConnectionLifecycleScope?>(null);
}
