namespace Elsa.Common.Multitenancy;

/// <summary>
/// A tenant resolver strategy in a pipeline of tenant resolvers.
/// </summary>
public interface ITenantResolutionStrategy
{
    /// <summary>
    /// Resolves the tenant.
    /// </summary>
    ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionContext context);
}