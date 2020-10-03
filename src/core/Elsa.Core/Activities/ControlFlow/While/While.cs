using System.Threading;
using System.Threading.Tasks;
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
        private readonly IExpressionEvaluator expressionEvaluator;

        public While(IExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }
        
        [ActivityProperty(Hint = "Enter an expression that evaluates to a boolean value.")]
        public IWorkflowExpression<bool> Condition { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var loop = await expressionEvaluator.EvaluateAsync(Condition, context, cancellationToken);

            if (loop)
                return Combine(Schedule(this), Done(OutcomeNames.Iterate));

            return Done();
        }
    }
}