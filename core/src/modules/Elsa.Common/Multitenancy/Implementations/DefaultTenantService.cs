using System.Collections.Concurrent;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy;

public class DefaultTenantService(
    IServiceScopeFactory scopeFactory,
    ITenantScopeFactory tenantScopeFactory,
    TenantEventsManager tenantEvents,
    ITenantAccessor tenantAccessor,
    IOptions<TenantActivationOptions> activationOptions) : ITenantService, IAsyncDisposable
{
    private readonly AsyncServiceScope _serviceScope = scopeFactory.CreateAsyncScope();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile ConcurrentDictionary<string, Tenant>? _tenantsDictionary;
    private volatile ConcurrentDictionary<Tenant, TenantScope>? _tenantScopesDictionary;
    private volatile bool _initialized;

    [Obsolete("Use the constructor that accepts TenantActivationOptions instead. Removed in the next major version.")]
    public DefaultTenantService(
        IServiceScopeFactory scopeFactory,
        ITenantScopeFactory tenantScopeFactory,
        TenantEventsManager tenantEvents,
        ITenantAccessor tenantAccessor) : this(scopeFactory, tenantScopeFactory, tenantEvents, tenantAccessor, Options.Create(new TenantActivationOptions()))
    {
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceScope.DisposeAsync();
        _initializationLock.Dispose();
    }

    public async Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return dictionary.TryGetValue(id.EmptyIfNull(), out var tenant) ? tenant : null;
    }

    public async Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return filter.Apply(dictionary.Values.AsQueryable()).FirstOrDefault();
    }

    public async Task<Tenant> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return dictionary[id.EmptyIfNull()];
    }

    public async Task<Tenant> GetAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return filter.Apply(dictionary.Values.AsQueryable()).First();
    }

    public async Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return dictionary.Values;
    }

    public async Task<IEnumerable<Tenant>> ListAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return filter.Apply(dictionary.Values.AsQueryable());
    }

    public async Task ActivateTenantsAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken);
        await tenantEvents.AllTenantsActivatedAsync(cancellationToken);
    }

    public async Task DeactivateTenantsAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            var dictionary = await GetInitializedTenantsDictionaryAsync(cancellationToken);
            var tenants = dictionary.Values.ToArray();

            foreach (var tenant in tenants)
                await UnregisterTenantAsync(tenant, false, cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
            var currentTenants = await GetInitializedTenantsDictionaryAsync(cancellationToken);
            var currentTenantIds = currentTenants.Keys.ToArray();
            var tenantsFromProvider = (await tenantsProvider.ListAsync(cancellationToken)).ToList();
            var newTenants = tenantsFromProvider.Count == 0
                ? new Dictionary<string, Tenant> { [Tenant.DefaultTenantId] = Tenant.Default }
                : tenantsFromProvider.ToDictionary(x => x.Id.EmptyIfNull());
            var newTenantIds = newTenants.Keys.ToArray();
            var removedTenantIds = currentTenantIds.Except(newTenantIds).ToArray();
            var addedTenantIds = newTenantIds.Except(currentTenantIds).ToArray();

            foreach (var removedTenantId in removedTenantIds)
            {
                if (currentTenants.TryGetValue(removedTenantId, out var removedTenant))
                    await UnregisterTenantAsync(removedTenant, true, cancellationToken);
            }

            var pendingTenants = addedTenantIds.Select(x => RegisterTenant(newTenants[x])).ToList();
            await ActivatePendingTenantsAsync(pendingTenants, cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<ConcurrentDictionary<string, Tenant>> GetTenantsDictionaryAsync(CancellationToken cancellationToken)
    {
        var existingDictionary = _tenantsDictionary;

        if (existingDictionary != null)
            return existingDictionary;

        return await InitializeAsync(cancellationToken);
    }

    // Lookups read the published dictionary while initialization is still activating; refresh and deactivation must not.
    private async Task<ConcurrentDictionary<string, Tenant>> GetInitializedTenantsDictionaryAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return _tenantsDictionary!;

        return await InitializeAsync(cancellationToken);
    }

    private async Task<ConcurrentDictionary<string, Tenant>> InitializeAsync(CancellationToken cancellationToken)
    {
        await _initializationLock.WaitAsync(cancellationToken); // Lock to ensure single-threaded initialization
        try
        {
            if (_initialized) // Double-check locking
                return _tenantsDictionary!;

            var tenantsProvider = _serviceScope.ServiceProvider.GetRequiredService<ITenantsProvider>();
            var tenants = (await tenantsProvider.ListAsync(cancellationToken)).ToList();
            if (tenants.Count == 0)
                tenants = [Tenant.Default];

            var tenantsDictionary = new ConcurrentDictionary<string, Tenant>();

            // Published before activation so a startup task that looks up a tenant from inside activation does not wait
            // for the lock its own activation holds. A tenant becomes visible once its own activation starts.
            _tenantScopesDictionary = new ConcurrentDictionary<Tenant, TenantScope>();
            _tenantsDictionary = tenantsDictionary;

            try
            {
                await ActivatePendingTenantsAsync(tenants.Select(RegisterTenant).ToList(), cancellationToken);
            }
            catch
            {
                // Don't stay half-started: deactivate the tenants that did activate and let the next call retry from scratch.
                foreach (var tenant in tenantsDictionary.Values.ToArray())
                    await UnregisterTenantAsync(tenant, false, CancellationToken.None);

                _tenantsDictionary = null;
                _tenantScopesDictionary = null;
                throw;
            }

            _initialized = true;
            return tenantsDictionary;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private PendingTenant RegisterTenant(Tenant tenant) => new(tenant, tenantScopeFactory.CreateScope(tenant));

    /// <summary>
    /// Activates the pending tenants in parallel. If an activation fails, only the tenants that completed stay registered.
    /// </summary>
    private async Task ActivatePendingTenantsAsync(IReadOnlyList<PendingTenant> pendingTenants, CancellationToken cancellationToken)
    {
        if (pendingTenants.Count == 0)
            return;

        var activatedTenants = new ConcurrentBag<Tenant>();

        try
        {
            var maxDegreeOfParallelism = activationOptions.Value.MaxDegreeOfParallelism;

            var parallelOptions = new ParallelOptions
            {
                // Parallel.ForEachAsync treats -1 as the processor count, not as unlimited.
                MaxDegreeOfParallelism = maxDegreeOfParallelism == -1 ? int.MaxValue : maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(pendingTenants, parallelOptions, async (pendingTenant, _) => await ActivateTenantAsync(pendingTenant, activatedTenants, cancellationToken));
        }
        catch
        {
            // A failure does not cancel in-flight activations, and Parallel.ForEachAsync waits for them, so nothing is still running here.
            // Keep the tenants that failed or never started unregistered, so the next refresh retries them.
            foreach (var pendingTenant in pendingTenants.Where(x => !activatedTenants.Contains(x.Tenant)))
            {
                _tenantsDictionary!.TryRemove(pendingTenant.Tenant.Id.EmptyIfNull(), out _);
                _tenantScopesDictionary!.TryRemove(pendingTenant.Tenant, out _);
                await pendingTenant.Scope.DisposeAsync();
            }

            throw;
        }
    }

    private async Task ActivateTenantAsync(PendingTenant pendingTenant, ConcurrentBag<Tenant> activatedTenants, CancellationToken cancellationToken)
    {
        _tenantScopesDictionary![pendingTenant.Tenant] = pendingTenant.Scope;
        _tenantsDictionary![pendingTenant.Tenant.Id.EmptyIfNull()] = pendingTenant.Tenant;

        using (tenantAccessor.PushContext(pendingTenant.Tenant))
            await tenantEvents.TenantActivatedAsync(new(pendingTenant.Tenant, pendingTenant.Scope, cancellationToken));

        activatedTenants.Add(pendingTenant.Tenant);
    }

    private async Task UnregisterTenantAsync(Tenant tenant, bool isDeleted, CancellationToken cancellationToken = default)
    {
        if (_tenantScopesDictionary!.TryRemove(tenant, out var scope))
        {
            _tenantsDictionary!.TryRemove(tenant.Id.EmptyIfNull(), out _);

            using (tenantAccessor.PushContext(tenant))
            {
                await tenantEvents.TenantDeactivatedAsync(new(tenant, scope, cancellationToken));

                if (isDeleted)
                    await tenantEvents.TenantDeletedAsync(new(tenant, scope, cancellationToken));
            }
        }
    }

    private sealed record PendingTenant(Tenant Tenant, TenantScope Scope);
}
