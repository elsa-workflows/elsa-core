using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Execute while a given condition is true.",
        Outcomes = new[] { OutcomeNames.Iterate, OutcomeNames.Done }
    )]
    public class While : Activity
    {
        [ActivityProperty(Hint = "The condition to evaluate.")]
        public bool Condition { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var loop = Condition;

            if (loop)
            {
                context.WorkflowInstance.Scopes.Push(Id);
                    
                return Outcome(OutcomeNames.Iterate);
            }
            
            return Done();
        }
    }
}