using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Services;

/// <summary>
/// Service responsible for updating the activity registry based on activity providers.
/// </summary>
public class ActivityRegistryUpdateService(IEnumerable<IActivityProvider> providers, IActivityRegistry registry) : IActivityRegistryUpdateService
{
    /// <inheritdoc />
    public async Task AddToRegistry(Type providerType, string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var provider = providers.First(x => x.GetType() == providerType);
        var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
        var descriptorToAdd = descriptors
            .SingleOrDefault(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) &&
                val.ToString() == workflowDefinitionVersionId);
        
        if (descriptorToAdd is not null)
            registry.Add(providerType, descriptorToAdd);
    }

    /// <inheritdoc />
    public void RemoveDefinitionFromRegistry(Type providerType, string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var providerDescriptors = registry.ListByProvider(providerType);
        
        var descriptorsToRemove = providerDescriptors
            .Where(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionId", out var val) &&
                val.ToString() == workflowDefinitionId).ToList();

        foreach (ActivityDescriptor activityDescriptor in descriptorsToRemove)
        {
            registry.Remove(providerType, activityDescriptor);
        }
    }

    /// <inheritdoc />
    public void RemoveDefinitionVersionFromRegistry(Type providerType, string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var providerDescriptors = registry.ListByProvider(providerType);
        
        var descriptorToRemove = providerDescriptors
            .SingleOrDefault(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) &&
                val.ToString() == workflowDefinitionVersionId);

        if (descriptorToRemove is not null)
            registry.Remove(providerType, descriptorToRemove);
    }
}