using Elsa.Common.Entities;

namespace Elsa.Common.Contexts;

/// <summary>
/// Represents the context for resolving a tenant in a tenant resolution strategy pipeline.
/// </summary>
/// <remarks>
/// This class provides the necessary information for resolving a tenant, including a cancellation token
/// to allow for cancelling the resolution process.
/// </remarks>
public class TenantResolutionContext
{
    private readonly IDictionary<string, Tenant> _tenantsDictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolutionContext"/> class.
    /// </summary>
    public TenantResolutionContext(IDictionary<string, Tenant> tenants, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _tenantsDictionary = tenants;
    }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; } = default;

    /// <summary>
    /// Finds a tenant based on the provided tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The found tenant or null if no tenant with the provided ID exists.</returns>
    public Tenant? FindTenant(string tenantId)
    {
        return _tenantsDictionary.TryGetValue(tenantId, out var tenant) ? tenant : null;
    }

    /// <summary>
    /// Finds a tenant based on the provided predicate.
    /// </summary>
    /// <param name="predicate">The predicate used to filter the tenants.</param>
    /// <returns>The found tenant or null if no tenant satisfies the predicate.</returns>
    public Tenant? FindTenant(Func<Tenant, bool> predicate)
    {
        return _tenantsDictionary.Values.FirstOrDefault(predicate);
    }
}