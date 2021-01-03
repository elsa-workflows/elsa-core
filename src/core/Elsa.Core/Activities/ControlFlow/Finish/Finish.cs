using System.Linq;
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
        
        [ActivityProperty(Hint = "The outcome to set on the container activity")]
        public string? OutcomeName { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var parentBlueprint = context.ActivityBlueprint.Parent;
            var blockingActivities = context.WorkflowExecutionContext.WorkflowInstance.BlockingActivities;
            var blockingActivityIds = blockingActivities.Select(x => x.ActivityId).ToList();
            var containedBlockingActivityIds = parentBlueprint == null ? blockingActivityIds : parentBlueprint.Activities.Where(x => blockingActivityIds.Contains(x.Id)).Select(x => x.Id).ToList();

            blockingActivities.RemoveWhere(x => containedBlockingActivityIds.Contains(x.ActivityId));
            var output = new FinishOutput(OutputValue, OutcomeName);
            context.WorkflowExecutionContext.WorkflowInstance.Output = output;
            return Done(output);
        }
    }
}