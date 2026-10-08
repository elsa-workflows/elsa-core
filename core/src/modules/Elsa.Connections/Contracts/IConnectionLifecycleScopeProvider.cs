namespace Elsa.Connections.Contracts;

/// <summary>Supplies the next trusted tenant and environment scope for hosted credential lifecycle work.</summary>
/// <remarks>Implementations must select scope from host configuration or trusted tenant infrastructure, never workflow input.</remarks>
public interface IConnectionLifecycleScopeProvider
{
    Task<ConnectionLifecycleScope?> GetNextScopeAsync(CancellationToken cancellationToken = default);
}

public sealed record ConnectionLifecycleScope(string TenantId, string EnvironmentId);
