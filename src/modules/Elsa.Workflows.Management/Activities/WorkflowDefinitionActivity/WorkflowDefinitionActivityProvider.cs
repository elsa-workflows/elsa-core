using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Provides activity descriptors based on <see cref="WorkflowDefinition"/>s stored in the database. 
/// </summary>
public class WorkflowDefinitionActivityProvider(IWorkflowDefinitionStore store, WorkflowDefinitionActivityDescriptorFactory workflowDefinitionActivityDescriptorFactory) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            UsableAsActivity = true,
            VersionOptions = VersionOptions.All
        };
        
        var definitions = (await store.FindManyAsync(filter, cancellationToken)).ToList();
        return CreateDescriptors(definitions).ToList();
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(ICollection<WorkflowDefinition> definitions)
    {
        return definitions.Select(x => CreateDescriptor(x, definitions));
    }

    private ActivityDescriptor CreateDescriptor(WorkflowDefinition definition, ICollection<WorkflowDefinition> allDefinitions)
    {
        var latestPublishedVersion = allDefinitions
            .Where(x => x.DefinitionId == definition.DefinitionId && x.IsPublished)
            .MaxBy(x => x.Version);
        return workflowDefinitionActivityDescriptorFactory.CreateDescriptor(definition, latestPublishedVersion);
    }
}