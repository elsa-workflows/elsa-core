using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    [Activity(
        DisplayName = "Set Context ID",
        Description = "Set context ID on the workflow.",
        Category = "Primitives",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SetContextId : Activity
    {
        [ActivityProperty(Hint = "The context ID to set.")]
        public string ContextId { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            context.WorkflowExecutionContext.WorkflowInstance.ContextId = ContextId;
            return Done();
        }
    }
}