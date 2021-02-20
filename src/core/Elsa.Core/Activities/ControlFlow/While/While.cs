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
        
        private bool Break
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (Break)
            {
                Break = false;
                return Done();
            }
            
            var loop = Condition;

            if (!loop) 
                return Done();
            
            context.CreateScope();
            return Outcome(OutcomeNames.Iterate);
        }
    }
}