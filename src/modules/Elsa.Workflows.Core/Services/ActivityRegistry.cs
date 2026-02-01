using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityRegistry(IActivityDescriber activityDescriber, IEnumerable<IActivityDescriptorModifier> modifiers, ITenantAccessor tenantAccessor, ILogger<ActivityRegistry> logger) : IActivityRegistry
{
    // Legacy support for manually registered activities
    private readonly ISet<ActivityDescriptor> _manualActivityDescriptors = new HashSet<ActivityDescriptor>();

    // Per-tenant activity descriptors (workflow-as-activities, tenant-specific providers, etc.)
    private readonly ConcurrentDictionary<string, TenantRegistryData> _tenantRegistries = new();

    // Tenant-agnostic activity descriptors (built-in activities, manually registered, etc.)
    private readonly TenantRegistryData _agnosticRegistry = new();

    /// <inheritdoc />
    public void Add(Type providerType, ActivityDescriptor descriptor)
    {
        var registry = GetOrCreateRegistry(descriptor.TenantId);
        var providerDescriptors = GetOrCreateProviderDescriptors(registry, providerType);
        Add(descriptor, registry.ActivityDescriptors, providerDescriptors);
    }

    /// <inheritdoc />
    public void Remove(Type providerType, ActivityDescriptor descriptor)
    {
        var registry = GetOrCreateRegistry(descriptor.TenantId);
        if (registry.ProvidedActivityDescriptors.TryGetValue(providerType, out var providerDescriptors))
        {
            providerDescriptors.Remove(descriptor);
            registry.ActivityDescriptors.TryRemove((descriptor.TypeName, descriptor.Version), out _);
        }
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> ListAll()
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Get descriptors from current tenant's registry
        var tenantDescriptors = _tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry)
            ? tenantRegistry.ActivityDescriptors.Values
            : Enumerable.Empty<ActivityDescriptor>();

        // Get descriptors from agnostic registry
        var agnosticDescriptors = _agnosticRegistry.ActivityDescriptors.Values;

        return tenantDescriptors.Concat(agnosticDescriptors);
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> ListByProvider(Type providerType)
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Get descriptors from current tenant's registry
        var tenantDescriptors = _tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry) &&
                                tenantRegistry.ProvidedActivityDescriptors.TryGetValue(providerType, out var tenantProviderDescriptors)
            ? tenantProviderDescriptors
            : Enumerable.Empty<ActivityDescriptor>();

        // Get descriptors from agnostic registry
        var agnosticDescriptors = _agnosticRegistry.ProvidedActivityDescriptors.TryGetValue(providerType, out var agnosticProviderDescriptors)
            ? agnosticProviderDescriptors
            : Enumerable.Empty<ActivityDescriptor>();

        return tenantDescriptors.Concat(agnosticDescriptors);
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(string type)
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Always prefer tenant-specific descriptors over tenant-agnostic ones
        // Get highest version from current tenant's registry
        if (_tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry))
        {
            var tenantDescriptor = tenantRegistry.ActivityDescriptors.Values
                .Where(x => x.TypeName == type)
                .MaxBy(x => x.Version);

            if (tenantDescriptor != null)
                return tenantDescriptor;
        }

        // Fall back to agnostic registry only if no tenant-specific descriptor exists
        return _agnosticRegistry.ActivityDescriptors.Values
            .Where(x => x.TypeName == type)
            .MaxBy(x => x.Version);
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(string type, int version)
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Check current tenant's registry first
        if (_tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry) &&
            tenantRegistry.ActivityDescriptors.TryGetValue((type, version), out var tenantDescriptor))
        {
            return tenantDescriptor;
        }

        // Fall back to agnostic registry
        return _agnosticRegistry.ActivityDescriptors.TryGetValue((type, version), out var agnosticDescriptor)
            ? agnosticDescriptor
            : null;
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate)
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Check current tenant's registry first
        if (_tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry))
        {
            var tenantMatch = tenantRegistry.ActivityDescriptors.Values.FirstOrDefault(predicate);
            if (tenantMatch != null) return tenantMatch;
        }

        // Fall back to agnostic registry
        return _agnosticRegistry.ActivityDescriptors.Values.FirstOrDefault(predicate);
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate)
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Get descriptors from current tenant's registry
        var tenantDescriptors = _tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry)
            ? tenantRegistry.ActivityDescriptors.Values.Where(predicate)
            : Enumerable.Empty<ActivityDescriptor>();

        // Get descriptors from agnostic registry
        var agnosticDescriptors = _agnosticRegistry.ActivityDescriptors.Values.Where(predicate);

        return tenantDescriptors.Concat(agnosticDescriptors);
    }

    /// <inheritdoc />
    public void Register(ActivityDescriptor descriptor)
    {
        var registry = GetOrCreateRegistry(descriptor.TenantId);
        var providerDescriptors = GetOrCreateProviderDescriptors(registry, GetType());
        Add(descriptor, registry.ActivityDescriptors, providerDescriptors);
    }

    /// <inheritdoc />
    public async Task RegisterAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken)
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(activityType);

        // Check if already registered in any registry
        if (ListAll().Any(x => x.TypeName == activityTypeName))
            return;

        var activityDescriptor = await activityDescriber.DescribeActivityAsync(activityType, cancellationToken);

        var registry = GetOrCreateRegistry(activityDescriptor.TenantId);
        Add(activityDescriptor, registry.ActivityDescriptors, _manualActivityDescriptors);
        _manualActivityDescriptors.Add(activityDescriptor);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default)
    {
        foreach (var activityType in activityTypes)
            await RegisterAsync(activityType, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) => new(_manualActivityDescriptors);

    /// <inheritdoc />
    public async Task RefreshDescriptorsAsync(IEnumerable<IActivityProvider> activityProviders, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantAccessor.TenantId;

        foreach (var activityProvider in activityProviders)
        {
            await RefreshDescriptorsAsync(activityProvider, cancellationToken);
        }
    }

    public async Task RefreshDescriptorsAsync(IActivityProvider activityProvider, CancellationToken cancellationToken = default)
    {
        var providerType = activityProvider.GetType();
        var currentTenantId = tenantAccessor.TenantId;

        // Get new descriptors from provider
        var descriptors = (await activityProvider.GetDescriptorsAsync(cancellationToken)).ToList();

        // Group descriptors by normalized tenant ID (null becomes agnostic marker for grouping purposes)
        var descriptorsByTenant = descriptors.GroupBy(d => d.TenantId);

        foreach (var group in descriptorsByTenant)
        {
            var tenantId = group.Key;
            var registry = GetOrCreateRegistry(tenantId);

            // Remove old descriptors for this provider from this tenant's registry
            if (registry.ProvidedActivityDescriptors.TryGetValue(providerType, out var oldDescriptors))
            {
                foreach (var oldDescriptor in oldDescriptors.ToList())
                {
                    registry.ActivityDescriptors.TryRemove((oldDescriptor.TypeName, oldDescriptor.Version), out _);
                }
            }

            // Add new descriptors for this tenant
            var providerDescriptors = new List<ActivityDescriptor>();
            foreach (var descriptor in group)
            {
                Add(descriptor, registry.ActivityDescriptors, providerDescriptors);
            }

            // Update the provider's descriptor list in this registry
            registry.ProvidedActivityDescriptors[providerType] = providerDescriptors;
        }
    }

    private void Add(ActivityDescriptor? descriptor, ConcurrentDictionary<(string Type, int Version), ActivityDescriptor> activityDescriptors, ICollection<ActivityDescriptor> providerDescriptors)
    {
        if (descriptor is null)
        {
            logger.LogError("Unable to add a null descriptor");
            return;
        }

        foreach (var modifier in modifiers)
            modifier.Modify(descriptor);

        // If the descriptor already exists, replace it. But log a warning.
        if (activityDescriptors.TryGetValue((descriptor.TypeName, descriptor.Version), out var existingDescriptor))
        {
            // Remove the existing descriptor from the providerDescriptors collection.
            providerDescriptors.Remove(existingDescriptor);

            // Log a warning.
            logger.LogWarning("Activity descriptor {ActivityType} v{ActivityVersion} was already registered for tenant {TenantId}. Replacing with new descriptor", descriptor.TypeName, descriptor.Version, descriptor.TenantId);
        }

        activityDescriptors[(descriptor.TypeName, descriptor.Version)] = descriptor;
        providerDescriptors.Add(descriptor);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _tenantRegistries.Clear();
        _agnosticRegistry.ActivityDescriptors.Clear();
        _agnosticRegistry.ProvidedActivityDescriptors.Clear();
    }

    /// <inheritdoc />
    public void ClearProvider(Type providerType)
    {
        var currentTenantId = tenantAccessor.TenantId;

        // Clear from current tenant's registry
        if (_tenantRegistries.TryGetValue(currentTenantId, out var tenantRegistry)
            && tenantRegistry.ProvidedActivityDescriptors.TryGetValue(providerType, out var descriptors))
        {
            foreach (var descriptor in descriptors.ToList()) 
                tenantRegistry.ActivityDescriptors.TryRemove((descriptor.TypeName, descriptor.Version), out _);

            tenantRegistry.ProvidedActivityDescriptors.TryRemove(providerType, out _);
        }

        // Clear from agnostic registry
        if (_agnosticRegistry.ProvidedActivityDescriptors.TryGetValue(providerType, out var agnosticDescriptors))
        {
            foreach (var descriptor in agnosticDescriptors.ToList()) 
                _agnosticRegistry.ActivityDescriptors.TryRemove((descriptor.TypeName, descriptor.Version), out _);

            _agnosticRegistry.ProvidedActivityDescriptors.TryRemove(providerType, out _);
        }
    }

    /// <summary>
    /// Clears all activity descriptors for a specific tenant. Useful when a tenant is deactivated.
    /// </summary>
    public void ClearTenant(string tenantId)
    {
        _tenantRegistries.TryRemove(tenantId, out _);
    }

    private TenantRegistryData GetOrCreateRegistry(string? tenantId)
    {
        // Null or agnostic tenant ID goes to agnostic registry
        if (tenantId is null or Tenant.AgnosticTenantId)
            return _agnosticRegistry;

        // Get or create tenant-specific registry
        return _tenantRegistries.GetOrAdd(tenantId, _ => new());
    }

    private ICollection<ActivityDescriptor> GetOrCreateProviderDescriptors(TenantRegistryData registry, Type providerType)
    {
        return registry.ProvidedActivityDescriptors.GetOrAdd(providerType, _ => new List<ActivityDescriptor>());
    }
}