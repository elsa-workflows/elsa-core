namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents the result of a tenant resolution.
/// </summary>
public record TenantResolverResult
{
    private readonly bool _isResolved;

    private TenantResolverResult(string? tenantId, bool isResolved)
    {
        TenantId = tenantId;
        _isResolved = isResolved;
    }

    /// <summary>
    /// The normalized tenant ID. Returns null if unresolved.
    /// </summary>
    public string? TenantId => _isResolved ? field.NormalizeTenantId() : null;

    /// <summary>
    /// Creates a new instance of <see cref="TenantResolverResult"/> representing a resolved tenant.
    /// </summary>
    /// <param name="tenantId">The resolved tenant.</param>
    /// <returns>A new instance of <see cref="TenantResolverResult"/> representing a resolved tenant.</returns>
    public static TenantResolverResult Resolved(string? tenantId) => new(tenantId, true);

    /// <summary>
    /// Creates a new instance of <see cref="TenantResolverResult"/> representing an unresolved tenant.
    /// </summary>
    /// <returns>A new instance of <see cref="TenantResolverResult"/> representing an unresolved tenant.</returns>
    public static TenantResolverResult Unresolved() => new(null, false);

    /// <summary>
    /// Gets a value indicating whether the tenant has been resolved.
    /// </summary>
    public bool IsResolved => _isResolved;

    public string ResolveTenantId() => TenantId ?? throw new InvalidOperationException("Tenant has not been resolved.");
}