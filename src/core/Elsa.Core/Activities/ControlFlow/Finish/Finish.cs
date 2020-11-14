using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Workflows",
        Description = "Removes any blocking activities and sets the status of the workflow to Completed."
    )]
    public class Finish : Activity
    {
        [ActivityProperty(Hint = "The value to set as the workflow's output'")]
        public object? OutputValue { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            context.WorkflowExecutionContext.WorkflowInstance.BlockingActivities.Clear();
            context.WorkflowExecutionContext.Complete(OutputValue);

            return Combine(Done(OutputValue));
        }
    }
}