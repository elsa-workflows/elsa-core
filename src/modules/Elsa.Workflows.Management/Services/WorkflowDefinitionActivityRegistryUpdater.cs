using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Caching;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Service responsible for updating the activity registry based on activity providers.
/// </summary>
public class WorkflowDefinitionActivityRegistryUpdater(
    WorkflowDefinitionActivityProvider provider,
    IActivityRegistry registry,
    ICacheManager? cacheManager,
    ITenantAccessor? tenantAccessor) : IWorkflowDefinitionActivityRegistryUpdater, IWorkflowDefinitionActivityRegistryReconciler
{
    private readonly Type _providerType = typeof(WorkflowDefinitionActivityProvider);
    private static readonly SemaphoreSlim RegistryLock = new(1, 1);

    /// <summary>
    /// Preserves the existing constructor for hosts that only use local registry updates.
    /// </summary>
    public WorkflowDefinitionActivityRegistryUpdater(WorkflowDefinitionActivityProvider provider, IActivityRegistry registry)
        : this(provider, registry, null, null)
    {
    }
    
    /// <inheritdoc />
    public async Task AddToRegistry(string workflowDefinitionVersionId, CancellationToken cancellationToken)
    {
        var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
        var descriptorToAdd = descriptors
            .FirstOrDefault(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) &&
                val.ToString() == workflowDefinitionVersionId);
        
        if (descriptorToAdd is null)
            return;

        await RegistryLock.WaitAsync(cancellationToken);
        try
        {
            registry.Add(_providerType, descriptorToAdd);
        }
        finally
        {
            RegistryLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ReconcileRegistryAsync(CancellationToken cancellationToken = default)
    {
        if (cacheManager is null || tenantAccessor is null)
            throw new InvalidOperationException("Registry reconciliation requires cache and tenant services.");

        await RegistryLock.WaitAsync(cancellationToken);
        try
        {
            // A cache warmed on this node before a remote write would otherwise hide the new store state.
            await cacheManager.TriggerTokenAsync(CachingWorkflowDefinitionStore.GetTenantReconciliationTokenKey(tenantAccessor.TenantId), cancellationToken);

            // Read the authoritative set before mutating the live registry. A failed or cancelled
            // store read must leave the currently usable descriptors in place.
            var descriptors = (await provider.GetDescriptorsAsync(cancellationToken)).ToList();

            // ListByProvider is tenant-aware: it exposes only the current tenant plus agnostic descriptors.
            // Removing that visible set first also handles an empty provider result, which the generic
            // ActivityRegistry.RefreshDescriptorsAsync currently does not clear.
            foreach (var descriptor in registry.ListByProvider(_providerType).ToList())
                registry.Remove(_providerType, descriptor);

            foreach (var descriptor in descriptors)
                registry.Add(_providerType, descriptor);
        }
        finally
        {
            RegistryLock.Release();
        }
    }

    /// <inheritdoc />
    public void RemoveDefinitionFromRegistry(string workflowDefinitionId)
    {
        RegistryLock.Wait();
        try
        {
            var descriptorsToRemove = registry.ListByProvider(_providerType)
                .Where(d => d.CustomProperties.TryGetValue("WorkflowDefinitionId", out var val) && val.ToString() == workflowDefinitionId)
                .ToList();

            foreach (var activityDescriptor in descriptorsToRemove)
                registry.Remove(_providerType, activityDescriptor);
        }
        finally
        {
            RegistryLock.Release();
        }
    }

    /// <inheritdoc />
    public void RemoveDefinitionVersionFromRegistry(string workflowDefinitionVersionId)
    {
        RegistryLock.Wait();
        try
        {
            var descriptorToRemove = registry.ListByProvider(_providerType)
                .FirstOrDefault(d => d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) && val.ToString() == workflowDefinitionVersionId);

            if (descriptorToRemove is not null)
                registry.Remove(_providerType, descriptorToRemove);
        }
        finally
        {
            RegistryLock.Release();
        }
    }
}
