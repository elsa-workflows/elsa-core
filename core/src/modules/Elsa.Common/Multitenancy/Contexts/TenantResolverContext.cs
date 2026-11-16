namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents the context for resolving a tenant in a tenant resolution strategy pipeline.
/// </summary>
/// <remarks>
/// This class provides the necessary information for resolving a tenant, including a cancellation token
/// to allow for cancelling the resolution process.
/// </remarks>
public class TenantResolverContext
{
    private readonly IDictionary<string, Tenant>? _tenantsDictionary;
    private readonly ITenantService? _tenantService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolverContext"/> class.
    /// </summary>
    public TenantResolverContext(IDictionary<string, Tenant> tenants, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _tenantsDictionary = tenants;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantResolverContext"/> class that looks tenants up in the tenant service.
    /// Nothing is loaded up front: a lookup by ID is a single dictionary read and only a predicate lookup enumerates the tenants.
    /// </summary>
    public TenantResolverContext(ITenantService tenantService, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Finds a tenant based on the provided tenant ID.
    /// </summary>
    /// <remarks>
    /// When the context is backed by an <see cref="ITenantService"/>, this blocks until the tenant service has started. Under a single-threaded
    /// <see cref="SynchronizationContext"/> that can deadlock if it has not started yet. Prefer an async resolver.
    /// </remarks>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The found tenant or null if no tenant with the provided ID exists.</returns>
    public Tenant? FindTenant(string? tenantId)
    {
        var normalizedId = tenantId.NormalizeTenantId();

        if (_tenantsDictionary != null)
        {
            return _tenantsDictionary.TryGetValue(normalizedId, out var tenant) ? tenant : null;
        }

        // The tenant service answers from memory once started, so blocking here does not wait on I/O.
        return _tenantService!.FindAsync(normalizedId, CancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Finds a tenant based on the provided predicate.
    /// </summary>
    /// <remarks>
    /// When the context is backed by an <see cref="ITenantService"/>, this blocks until the tenant service has started. Under a single-threaded
    /// <see cref="SynchronizationContext"/> that can deadlock if it has not started yet. Use <see cref="FindTenantAsync"/> instead.
    /// </remarks>
    /// <param name="predicate">The predicate used to filter the tenants.</param>
    /// <returns>The found tenant or null if no tenant satisfies the predicate.</returns>
    public Tenant? FindTenant(Func<Tenant, bool> predicate)
    {
        return FindTenantAsync(predicate).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Finds a tenant based on the provided predicate.
    /// </summary>
    /// <param name="predicate">The predicate used to filter the tenants.</param>
    /// <returns>The found tenant or null if no tenant satisfies the predicate.</returns>
    public async Task<Tenant?> FindTenantAsync(Func<Tenant, bool> predicate)
    {
        if (_tenantsDictionary != null)
        {
            return _tenantsDictionary.Values.FirstOrDefault(predicate);
        }

        var tenants = await _tenantService!.ListAsync(CancellationToken);
        return tenants.FirstOrDefault(predicate);
    }
}
