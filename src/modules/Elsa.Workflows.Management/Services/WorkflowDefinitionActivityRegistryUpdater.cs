using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Service responsible for updating the activity registry based on activity providers.
/// </summary>
public class WorkflowDefinitionActivityRegistryUpdater(WorkflowDefinitionActivityProvider provider, IActivityRegistry registry) : IWorkflowDefinitionActivityRegistryUpdater
{
    private readonly Type _providerType = typeof(WorkflowDefinitionActivityProvider);
    
    /// <inheritdoc />
    public async Task AddToRegistry(string workflowDefinitionVersionId, CancellationToken cancellationToken)
    {
        var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
        var descriptorToAdd = descriptors
            .FirstOrDefault(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) &&
                val.ToString() == workflowDefinitionVersionId);
        
        if (descriptorToAdd is not null)
            registry.Add(_providerType, descriptorToAdd);
    }

    /// <inheritdoc />
    public void RemoveDefinitionFromRegistry(string workflowDefinitionId)
    {
        var providerDescriptors = registry.ListByProvider(_providerType);
        
        var descriptorsToRemove = providerDescriptors
            .Where(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionId", out var val) &&
                val.ToString() == workflowDefinitionId).ToList();

        foreach (ActivityDescriptor activityDescriptor in descriptorsToRemove)
        {
            registry.Remove(_providerType, activityDescriptor);
        }
    }

    /// <inheritdoc />
    public void RemoveDefinitionVersionFromRegistry(string workflowDefinitionVersionId)
    {
        var providerDescriptors = registry.ListByProvider(_providerType);
        
        var descriptorToRemove = providerDescriptors
            .FirstOrDefault(d =>
                d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) &&
                val.ToString() == workflowDefinitionVersionId);

        if (descriptorToRemove is not null)
            registry.Remove(_providerType, descriptorToRemove);
    }
}