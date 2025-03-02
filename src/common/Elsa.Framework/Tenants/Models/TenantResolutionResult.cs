namespace Elsa.Framework.Tenants;

/// <summary>
/// Represents the result of a tenant resolution.
/// </summary>
/// <param name="TenantId">The resolved tenant ID.</param>
public record TenantResolutionResult(string? TenantId)
{
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolutionResult"/> representing a resolved tenant.
    /// </summary>
    /// <param name="tenantId">The resolved tenant.</param>
    /// <returns>A new instance of <see cref="TenantResolutionResult"/> representing a resolved tenant.</returns>
    public static TenantResolutionResult Resolved(string tenantId) => new(tenantId);
    
    /// <summary>
    /// Creates a new instance of <see cref="TenantResolutionResult"/> representing an unresolved tenant.
    /// </summary>
    /// <returns>A new instance of <see cref="TenantResolutionResult"/> representing an unresolved tenant.</returns>
    public static TenantResolutionResult Unresolved() => new(default(string?));
    
    /// <summary>
    /// Gets a value indicating whether the tenant has been resolved.
    /// </summary>
    public bool IsResolved => TenantId != null;
}