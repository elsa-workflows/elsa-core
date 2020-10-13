using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Iterate between two numbers.",
        Icon = "far fa-circle",
        Outcomes = new[] { OutcomeNames.Iterate, OutcomeNames.Done }
    )]
    public class For : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the starting number.")]
        public int Start { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the ending number.")]
        public int End { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the incrementing number on each step.")]
        public int Step { get; set; }

        private int? CurrentValue { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var currentValue = CurrentValue ?? Start;

            if (currentValue < End)
            {
                currentValue += Step;
                CurrentValue = currentValue;
                return Combine(Schedule(Id), Output(currentValue), Done(OutcomeNames.Iterate));
            }

            CurrentValue = null;
            return Done();
        }
    }
}