using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Data.Services
{
    /// <summary>
    /// Provides workflows from the workflow definition store.
    /// </summary>
    public class DatabaseWorkflowProvider : IWorkflowProvider
    {
        private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
        private readonly IActivityActivator _activityActivator;

        public DatabaseWorkflowProvider(
            IWorkflowDefinitionManager workflowDefinitionManager,
            IActivityActivator activityActivator)
        {
            _workflowDefinitionManager = workflowDefinitionManager;
            _activityActivator = activityActivator;
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> GetWorkflowsAsync(CancellationToken cancellationToken)
        {
            var workflowDefinitions = await _workflowDefinitionManager.ListAsync(cancellationToken);
            return workflowDefinitions.Select(CreateWorkflow);
        }

        private WorkflowBlueprint CreateWorkflow(WorkflowDefinition definition)
        {
            var activityBlueprints = definition.Activities.Select(CreateBlueprint).ToDictionary(x => x.Id);

            var workflow = new WorkflowBlueprint(
                definition.WorkflowDefinitionVersionId,
                definition.Version,
                definition.IsSingleton,
                definition.IsEnabled,
                definition.Name,
                definition.Description,
                definition.IsLatest,
                definition.IsPublished,
                definition.PersistenceBehavior,
                definition.DeleteCompletedInstances,
                activityBlueprints.Values,
                definition.Connections.Select(x => ResolveConnection(x, activityBlueprints)).ToList(),
                new ActivityPropertyProviders()
            );

            return workflow;
        }

        private static Connection ResolveConnection(
            ConnectionDefinition connectionDefinition,
            IReadOnlyDictionary<string, IActivityBlueprint> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId!];
            var target = activityDictionary[connectionDefinition.TargetActivityId!];
            var outcome = connectionDefinition.Outcome;

            return new Connection(source, target, outcome!);
        }

        private IActivityBlueprint CreateBlueprint(ActivityDefinition activityDefinition)
        {
            return new ActivityBlueprint
            {
                Id = activityDefinition.Id,
                Type = activityDefinition.Type,
                CreateActivityAsync = (context, cancellationToken) =>
                    CreateActivityAsync(activityDefinition, context, cancellationToken)
            };
        }

        private ValueTask<IActivity> CreateActivityAsync(
            ActivityDefinition activityDefinition,
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            var activity = context.ActivateActivity(activityDefinition.Type);
            activity.Description = activityDefinition.Description;
            activity.Id = activityDefinition.Id;
            activity.Name = activityDefinition.Name;
            activity.DisplayName = activityDefinition.DisplayName;
            activity.PersistWorkflow = activityDefinition.PersistWorkflow;
            
            // TODO: Initialize each activity property with data from activity definition.

            return new ValueTask<IActivity>(activity);
        }
    }
}