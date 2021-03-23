using System.Linq;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Break out of a While, For or ForEach loop.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Break : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            // Find first scope that supports breaking.
            var supportedScopeTypes = new[] { nameof(For), nameof(While), nameof(ForEach) }.ToHashSet();
            
            var query =
                from scope in context.WorkflowInstance.Scopes
                let scopeActivity = context.WorkflowExecutionContext.GetActivityBlueprintById(scope.ActivityId)!
                let scopeType = scopeActivity.Type
                let supportsBreak = supportedScopeTypes.Contains(scopeType)
                where supportsBreak
                select scope;

            var supportedScope = query.FirstOrDefault();
            
            if(supportedScope != null)
                context.GetActivityData(supportedScope.ActivityId).SetState("Break", true);

            return Done();
        }
    }
}