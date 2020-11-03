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
            var activityBlueprints = workflowDefinition.Activities.SelectMany(CreateBlueprints).Distinct().ToDictionary(x => x.Id);
            var compositeActivityBlueprints = activityBlueprints.Values.Where(x => x is ICompositeActivityBlueprint).Cast<ICompositeActivityBlueprint>().ToList(); 
            var connections = compositeActivityBlueprints.SelectMany(x => x.Connections).Distinct().ToList();
            var propertyProviders = compositeActivityBlueprints.SelectMany(x => x.ActivityPropertyProviders).ToList();
            
            connections.AddRange(workflowDefinition.Connections.Select(x => ResolveConnection(x, activityBlueprints)));
            propertyProviders.AddRange(CreatePropertyProviders(workflowDefinition));

            return new WorkflowBlueprint(
                workflowDefinition.WorkflowDefinitionId,
                workflowDefinition.Version,
                workflowDefinition.IsSingleton,
                workflowDefinition.IsEnabled,
                workflowDefinition.Name,
                workflowDefinition.Description,
                workflowDefinition.IsLatest,
                workflowDefinition.IsPublished,
                workflowDefinition.Variables,
                workflowDefinition.ContextOptions,
                workflowDefinition.PersistenceBehavior,
                workflowDefinition.DeleteCompletedInstances,
                activityBlueprints.Values,
                connections,
                new ActivityPropertyProviders(propertyProviders.ToDictionary(x => x.Key, x => x.Value))
            );
        }

        private static ActivityPropertyProviders CreatePropertyProviders(CompositeActivityDefinition workflowDefinition)
        {
            var propertyProviders = new ActivityPropertyProviders();
            var activityDefinitions = workflowDefinition.Activities;

            foreach (var activityDefinition in activityDefinitions)
            {
                foreach (var property in activityDefinition.Properties)
                {
                    var provider = new ExpressionActivityPropertyValueProvider(property.Value.Expression, property.Value.Syntax, property.Value.Type);
                    propertyProviders.AddProvider(activityDefinition.ActivityId, property.Key, provider);
                }
            }

            return propertyProviders;
        }

        private static IConnection ResolveConnection(
            ConnectionDefinition connectionDefinition,
            IReadOnlyDictionary<string, IActivityBlueprint> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId!];
            var target = activityDictionary[connectionDefinition.TargetActivityId!];
            var outcome = connectionDefinition.Outcome;

            return new Connection(source, target, outcome!);
        }

        private static IEnumerable<IActivityBlueprint> CreateBlueprints(ActivityDefinition activityDefinition)
        {
            if (activityDefinition is CompositeActivityDefinition compositeActivityDefinition)
            {
                var activityBlueprints = compositeActivityDefinition.Activities.SelectMany(CreateBlueprints).ToDictionary(x => x.Id);

                foreach (var activityBlueprint in activityBlueprints.Values)
                    yield return activityBlueprint;
                
                yield return new CompositeActivityBlueprint
                {
                    Id = activityDefinition.ActivityId,
                    Type = activityDefinition.Type,
                    CreateActivityAsync = (context, cancellationToken) => CreateActivityAsync(activityDefinition, context, cancellationToken),
                    Activities = activityBlueprints.Values,
                    Connections = compositeActivityDefinition.Connections.Select(x => ResolveConnection(x, activityBlueprints)).ToList(),
                    ActivityPropertyProviders = CreatePropertyProviders(compositeActivityDefinition)
                };
            }
            else
            {
                yield return new ActivityBlueprint
                {
                    Id = activityDefinition.ActivityId,
                    Type = activityDefinition.Type,
                    CreateActivityAsync = (context, cancellationToken) => CreateActivityAsync(activityDefinition, context, cancellationToken)
                };    
            }
        }

        private static async ValueTask<IActivity> CreateActivityAsync(ActivityDefinition activityDefinition, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var activity = context.ActivateActivity();
            activity.Description = activityDefinition.Description;
            activity.Id = activityDefinition.ActivityId;
            activity.Name = activityDefinition.Name;
            activity.DisplayName = activityDefinition.DisplayName;
            activity.PersistWorkflow = activityDefinition.PersistWorkflow;
            await context.SetActivityPropertiesAsync(activity, cancellationToken);

            return activity;
        }
    }
}