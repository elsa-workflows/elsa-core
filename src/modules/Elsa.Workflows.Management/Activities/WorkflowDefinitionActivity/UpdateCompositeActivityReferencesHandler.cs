using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Updates the composite activity references in consuming workflows whenever an <see cref="WorkflowDefinition"/> is published.
/// </summary>
[PublicAPI]
public class UpdateCompositeActivityReferencesHandler : INotificationHandler<WorkflowDefinitionPublished>
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IActivityVisitor _activityVisitor;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCompositeActivityReferencesHandler"/> class.
    /// </summary>
    public UpdateCompositeActivityReferencesHandler(IWorkflowDefinitionStore store, IActivitySerializer activitySerializer, IActivityVisitor activityVisitor)
    {
        _store = store;
        _activitySerializer = activitySerializer;
        _activityVisitor = activityVisitor;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        if (!workflowDefinition.UsableAsActivity is true || !workflowDefinition.Options?.AutoUpdateConsumingWorkflows is true) return;
        
        await FindAndUpdateConsumingWorkflowsAsync(workflowDefinition, cancellationToken);
    }

    private async Task FindAndUpdateConsumingWorkflowsAsync(WorkflowDefinition compositeActivity, CancellationToken cancellationToken)
    {
        var updatedWorkflowDefinitions = new List<WorkflowDefinition>();
        
        var publishedWorkflowDefinitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
        {
            VersionOptions = VersionOptions.Published
        }, cancellationToken)).ToList();

        foreach (var definition in publishedWorkflowDefinitions)
        {
            var root = _activitySerializer.Deserialize(definition.StringData!);
            var graph = await _activityVisitor.VisitAsync(root, cancellationToken);
            var flattenedList = graph.Flatten().ToList();
            
            var consumingWorkflowActivities = flattenedList.Where(x => x.Activity is WorkflowDefinitionActivity workflowDefinitionActivity && workflowDefinitionActivity.WorkflowDefinitionId == compositeActivity.DefinitionId).ToList();

            foreach (var activity in consumingWorkflowActivities.Where(activity => activity.Activity.Version < compositeActivity.Version))
            {
                activity.Activity.Version = compositeActivity.Version;

                if (!updatedWorkflowDefinitions.Contains(definition))
                    updatedWorkflowDefinitions.Add(definition);
            }

            if (updatedWorkflowDefinitions.Contains(definition))
            {
                var serializedData = _activitySerializer.Serialize(root);
                definition.StringData = serializedData;
            }
        }
        
        if(updatedWorkflowDefinitions.Any())
            await _store.SaveManyAsync(updatedWorkflowDefinitions, cancellationToken);
    }
}