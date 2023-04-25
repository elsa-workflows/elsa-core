using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class ConsumingWorkflowProvider : IConsumingWorkflowProvider
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IActivityVisitor _activityVisitor;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumingWorkflowProvider"/> class.
    /// </summary>
    public ConsumingWorkflowProvider(IWorkflowDefinitionStore store, IActivitySerializer activitySerializer, IActivityVisitor activityVisitor)
    {
        _store = store;
        _activitySerializer = activitySerializer;
        _activityVisitor = activityVisitor;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindWorkflowsWithOutdatedCompositeActivitiesAsync(WorkflowDefinition compositeActivity, CancellationToken cancellationToken = default)
    {
        var workflowList = new List<WorkflowDefinition>();
        
        var publishedWorkflowDefinitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
        {
            VersionOptions = VersionOptions.Published
        }, cancellationToken)).ToList();

        foreach (var definition in publishedWorkflowDefinitions)
        {
            var root = _activitySerializer.Deserialize(definition.StringData!);
            var graph = await _activityVisitor.VisitAsync(root, cancellationToken);
            var flattenedList = graph.Flatten().ToList();

            if (flattenedList.Any(x =>
                    x.Activity is WorkflowDefinitionActivity workflowDefinitionActivity &&
                    workflowDefinitionActivity.WorkflowDefinitionId == compositeActivity.DefinitionId && 
                    workflowDefinitionActivity.Version < compositeActivity.Version))
            {
                workflowList.Add(definition);
            }
        }

        return workflowList;
    }
}