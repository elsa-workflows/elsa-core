using System;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        Category = "Control Flow",
        Description = "Iterate between two numbers.",
        Outcomes = new[] { OutcomeNames.Iterate, OutcomeNames.Done }
    )]
    public class For : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the starting number.")]
        public long Start { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the ending number.")]
        public long End { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the incrementing number on each step.")]
        public long Step { get; set; } = 1;

        [ActivityProperty(Hint = "The operator to use when comparing the current value against the end value.")]
        public Operator Operator { get; set; } = Operator.LessThan;

        private long? CurrentValue
        {
            get => GetState<long?>();
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
                context.WorkflowInstance.Scopes.Push(Id);
                CurrentValue = currentValue + Step;
                return Outcome(OutcomeNames.Iterate, currentValue);
            }

            CurrentValue = null;
            return Done();
        }
    }
}