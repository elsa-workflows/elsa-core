namespace Elsa.Common.Multitenancy;

/// <summary>
/// Base class for implementing a tenant resolution strategy.
/// </summary>
public abstract class TenantResolverBase : ITenantResolver
{
    Task<TenantResolverResult> ITenantResolver.ResolveAsync(TenantResolverContext context)
    {
        return ResolveAsync(context);
    }

    /// <summary>
    /// Implement this method to resolve the tenant.
    /// </summary>
    protected virtual Task<TenantResolverResult> ResolveAsync(TenantResolverContext context)
    {
        return Task.FromResult(Resolve(context));
    }

    /// <summary>
    /// Implement this method to resolve the tenant.
    /// </summary>
    protected virtual TenantResolverResult Resolve(TenantResolverContext context)
    {
        return Unresolved();
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolverResult"/> representing a resolved tenant.
    /// </summary>
    protected TenantResolverResult Resolved(string? tenantId) => TenantResolverResult.Resolved(tenantId);

    /// <summary>
    /// Creates a new instance of <see cref="TenantResolverResult"/> representing an unresolved tenant.
    /// </summary>
    protected TenantResolverResult Unresolved() => TenantResolverResult.Unresolved();

    /// <summary>
    /// Automatically resolves the tenant if the tenant ID is not null.
    /// </summary>
    protected TenantResolverResult AutoResolve(string? tenantId) => tenantId == null ? Unresolved() : Resolved(tenantId);
}