using System;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
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
        [ActivityProperty(Hint = "The starting number.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public long Start { get; set; }

        [ActivityProperty(Hint = "The ending number.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public long End { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to the incrementing number on each step.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public long Step { get; set; } = 1;

        [ActivityProperty(Hint = "The operator to use when comparing the current value against the end value.", SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public Operator Operator { get; set; } = Operator.LessThan;

        internal long? CurrentValue
        {
            get => GetState<long?>();
            set => SetState(value);
        }

        private bool Break
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (Break)
            {
                CurrentValue = null;
                Break = false;
                return Done();
            }

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
                var scope = context.CreateScope();

                scope.Variables.Set(nameof(CurrentValue), currentValue);
                return Outcome(OutcomeNames.Iterate, currentValue);
            }

            CurrentValue = null;
            return Done();
        }
    }
}