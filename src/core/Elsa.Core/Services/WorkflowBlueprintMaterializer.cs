using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowBlueprintMaterializer : IWorkflowBlueprintMaterializer
    {
        public IWorkflowBlueprint CreateWorkflowBlueprint(WorkflowDefinition workflowDefinition)
        {
            var activityBlueprints = workflowDefinition.Activities.Select(CreateBlueprint).ToDictionary(x => x.Id);

            return new WorkflowBlueprint(
                workflowDefinition.WorkflowDefinitionVersionId,
                workflowDefinition.Version,
                workflowDefinition.IsSingleton,
                workflowDefinition.IsEnabled,
                workflowDefinition.Name,
                workflowDefinition.Description,
                workflowDefinition.IsLatest,
                workflowDefinition.IsPublished,
                workflowDefinition.PersistenceBehavior,
                workflowDefinition.DeleteCompletedInstances,
                activityBlueprints.Values,
                workflowDefinition.Connections.Select(x => ResolveConnection(x, activityBlueprints)).ToList(),
                CreatePropertyProviders(workflowDefinition)
            );
        }

        private static ActivityPropertyProviders CreatePropertyProviders(WorkflowDefinition workflowDefinition)
        {
            var propertyProviders = new ActivityPropertyProviders();
            var activityDefinitions = workflowDefinition.Activities;

            foreach (var activityDefinition in activityDefinitions)
            {
                foreach (var property in activityDefinition.Properties)
                {
                    var provider = new ExpressionActivityPropertyValueProvider(property.Value.Expression, property.Value.Syntax, property.Value.Type);
                    propertyProviders.AddProvider(activityDefinition.Id, property.Key, provider);
                }
            }

            return propertyProviders;
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

        private static IActivityBlueprint CreateBlueprint(ActivityDefinition activityDefinition)
        {
            return new ActivityBlueprint
            {
                Id = activityDefinition.Id,
                Type = activityDefinition.Type,
                CreateActivityAsync = (context, cancellationToken) => CreateActivityAsync(activityDefinition, context, cancellationToken)
            };
        }

        private static async ValueTask<IActivity> CreateActivityAsync(ActivityDefinition activityDefinition, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var activity = context.ActivateActivity(activityDefinition.Type);
            activity.Description = activityDefinition.Description;
            activity.Id = activityDefinition.Id;
            activity.Name = activityDefinition.Name;
            activity.DisplayName = activityDefinition.DisplayName;
            activity.PersistWorkflow = activityDefinition.PersistWorkflow;
            await context.SetActivityPropertiesAsync(activity, cancellationToken);
            
            return activity;
        }
    }
}