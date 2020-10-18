using System;
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
        public int Step { get; set; } = 1;

        [ActivityProperty(Hint = "The operator to use when comparing the current value against the end value.")]
        public Operator Operator { get; set; } = Operator.LessThan;

        private int? CurrentValue
        {
            get => GetState<int?>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var currentValue = CurrentValue ?? Start;
            
            var loop = Operator switch
            {
                Operator.LessThan => currentValue < End,
                Operator.LessThanOrEqual => currentValue <= End,
                Operator.GreaterThan => currentValue > End,
                Operator.GreaterThanOrEqual => currentValue >= End,
                _ => throw new NotSupportedException()
            };

            if (loop)
            {
                CurrentValue = currentValue + Step;
                return Combine(Schedule(Id), Output(currentValue), Outcome(OutcomeNames.Iterate));
            }

            CurrentValue = null;
            return Done();
        }
    }
}