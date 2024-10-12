namespace Elsa.Common.Multitenancy;

/// <summary>
/// A strategy for resolving the current tenant. This is called the tenant initializer.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Attempts to resolve the current tenant.
    /// </summary>
    /// <returns>Current tenant or null.</returns>
    Task<TenantResolverResult> ResolveAsync(TenantResolverContext context);
}