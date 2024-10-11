namespace Elsa.Common.Multitenancy;

/// <summary>
/// Base class for implementing a tenant resolution strategy.
/// </summary>
public abstract class TenantResolutionStrategyBase : ITenantResolutionStrategy
{
    ValueTask<TenantResolutionResult> ITenantResolutionStrategy.ResolveAsync(TenantResolutionContext context)
    {
        return ResolveAsync(context);
    }

    /// <summary>
    /// Implement this method to resolve the tenant.
    /// </summary>
    protected virtual ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionContext context)
    {
        return new(Resolve(context));
    }

    /// <summary>
    /// Implement this method to resolve the tenant.
    /// </summary>
    /// <returns></returns>
    protected virtual TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        return Unresolved();
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolutionResult"/> representing a resolved tenant.
    /// </summary>
    protected TenantResolutionResult Resolved(string tenantId) => new(tenantId);
    
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolutionResult"/> representing an unresolved tenant.
    /// </summary>
    protected TenantResolutionResult Unresolved() => new(null);
    
    /// <summary>
    /// Automatically resolves the tenant if the tenant ID is not null.
    /// </summary>
    protected TenantResolutionResult AutoResolve(string? tenantId) => tenantId == null ? Unresolved() : Resolved(tenantId);
}