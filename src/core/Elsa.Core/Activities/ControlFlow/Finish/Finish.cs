using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Workflows",
        Description = "Removes any blocking activities from the current container (workflow or composite activity)."
    )]
    public class Finish : Activity
    {
        [ActivityProperty(Hint = "The value to set as the workflow's output")]
        public object? OutputValue { get; set; }

        [ActivityProperty(Hint = "The outcomes to set on the container activity")]
        public IEnumerable<string> OutcomeNames { get; set; } = new[] { Elsa.OutcomeNames.Done };

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var parentBlueprint = context.ActivityBlueprint.Parent;
            
            // Remove any blocking activities within the scope of the composite activity.
            var blockingActivities = context.WorkflowExecutionContext.WorkflowInstance.BlockingActivities;
            var blockingActivityIds = blockingActivities.Select(x => x.ActivityId).ToList();
            var containedBlockingActivityIds = parentBlueprint == null ? blockingActivityIds : parentBlueprint.Activities.Where(x => blockingActivityIds.Contains(x.Id)).Select(x => x.Id).ToList();
            var containedBlockingActivities = blockingActivities.Where(x => containedBlockingActivityIds.Contains(x.ActivityId));
            
            foreach (var blockingActivity in containedBlockingActivities) 
                await context.WorkflowExecutionContext.RemoveBlockingActivityAsync(blockingActivity);
            
            // Evict & remove any scope activities within the scope of the composite activity.
            var scopes = context.WorkflowInstance.Scopes.Select(x => x).Reverse().ToList();
            var scopeIds = scopes.Select(x => x.ActivityId).ToList();
            var containedScopeActivityIds = parentBlueprint == null ? scopeIds : parentBlueprint.Activities.Where(x => scopeIds.Contains(x.Id)).Select(x => x.Id).ToList();

            foreach (var scopeId in containedScopeActivityIds)
            {
                var scopeActivity = context.WorkflowExecutionContext.GetActivityBlueprintById(scopeId)!;
                await context.WorkflowExecutionContext.EvictScopeAsync(scopeActivity);
                scopes.RemoveAll(x => x.ActivityId == scopeId);
            }
            
            context.WorkflowInstance.Scopes = new SimpleStack<ActivityScope>(scopes.AsEnumerable().Reverse());
            
            // Return output
            var output = new FinishOutput(OutputValue, OutcomeNames);

            return Output(output);
        }
    }
}