using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Provides activity descriptors based on <see cref="WorkflowDefinition"/>s stored in the database. 
/// </summary>
public class WorkflowDefinitionActivityProvider(IWorkflowDefinitionStore store, WorkflowDefinitionActivityDescriptorFactory workflowDefinitionActivityDescriptorFactory, ITenantAccessor tenantAccessor) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            UsableAsActivity = true,
            VersionOptions = VersionOptions.All,
            // Explicitly set TenantAgnostic to false to ensure tenant filtering is applied.
            // This prevents workflow activities from leaking across tenant boundaries.
            TenantAgnostic = false
        };
        
        var definitions = (await store.FindManyAsync(filter, cancellationToken)).ToList();
        
        // Additional safety: Filter by current tenant ID to ensure no cross-tenant leakage.
        // This is a defense-in-depth measure in case the query filter isn't applied correctly.
        var currentTenantId = tenantAccessor.Tenant?.Id;
        if (currentTenantId != null)
        {
            definitions = definitions.Where(d => d.TenantId == currentTenantId).ToList();
        }
        else
        {
            // If there's no tenant context, only return definitions without a tenant ID
            definitions = definitions.Where(d => string.IsNullOrEmpty(d.TenantId)).ToList();
        }
        
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