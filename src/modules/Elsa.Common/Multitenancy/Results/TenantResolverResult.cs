namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents the result of a tenant resolution.
/// </summary>
/// <param name="TenantId">The resolved tenant.</param>
public record TenantResolverResult(string? TenantId)
{
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolverResult"/> representing a resolved tenant.
    /// </summary>
    /// <param name="tenantId">The resolved tenant.</param>
    /// <returns>A new instance of <see cref="TenantResolverResult"/> representing a resolved tenant.</returns>
    public static TenantResolverResult Resolved(string tenantId) => new(tenantId);
    
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolverResult"/> representing an unresolved tenant.
    /// </summary>
    /// <returns>A new instance of <see cref="TenantResolverResult"/> representing an unresolved tenant.</returns>
    public static TenantResolverResult Unresolved() => new(default(string?));
    
    /// <summary>
    /// Gets a value indicating whether the tenant has been resolved.
    /// </summary>
    public bool IsResolved => TenantId != null;
    
    public string ResolveTenantId() => TenantId ?? throw new InvalidOperationException("Tenant has not been resolved.");
}