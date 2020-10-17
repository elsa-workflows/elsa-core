using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Control Flow",
        Description = "Execute while a given condition is true.",
        Icon = "far fa-circle",
        Outcomes = new[] { OutcomeNames.Iterate, OutcomeNames.Done }
    )]
    public class While : Activity
    {
        private readonly IExpressionEvaluator _expressionEvaluator;

        public While(IExpressionEvaluator expressionEvaluator)
        {
            _expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "The condition to evaluate.")]
        public bool Condition { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var loop = Condition;

            if (loop)
                return Combine(Schedule(Id), Done(OutcomeNames.Iterate));

            return Done();
        }
    }
}