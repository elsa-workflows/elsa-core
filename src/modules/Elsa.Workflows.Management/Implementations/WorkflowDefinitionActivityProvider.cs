using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// Provides activity descriptors based on <see cref="WorkflowDefinition"/>s stored in the database. 
/// </summary>
public class WorkflowDefinitionActivityProvider : IActivityProvider
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IActivityFactory _activityFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionActivityProvider(IWorkflowDefinitionStore store, IActivityFactory activityFactory)
    {
        _store = store;
        _activityFactory = activityFactory;
    }

    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = (await _store.FindByPredicateAsync(w => w.UsableAsActivity == true, VersionOptions.Published, cancellationToken)).ToList();
        var descriptors = CreateDescriptors(definitions);
        return descriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<WorkflowDefinition> definitions) => definitions.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(WorkflowDefinition definition)
    {
        return new()
        {
            TypeName = definition.DefinitionId,
            Version = definition.Version,
            DisplayName = definition.Name,
            Description = definition.Description,
            Category = "Workflows",
            Kind = ActivityKind.Action,
            IsBrowsable = definition.IsPublished,
            ActivityType = typeof(WorkflowDefinitionActivity),
            Constructor = context =>
            {
                var activity = (WorkflowDefinitionActivity)_activityFactory.Create(typeof(WorkflowDefinitionActivity), context);
                activity.Type = definition.DefinitionId;
                activity.Version = definition.Version;
                return activity;
            }
        };
    }
}