using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
        RuntimeDescription = "x => !!x.state.expression ? `Evaluate <strong>${ x.state.expression.expression }</strong> and continue execution depending on the result.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False, OutcomeNames.Done }
    )]
    public class IfElse : Activity
    {
        public IfElse()
        {
        }

        public IfElse(IWorkflowExpression<bool> condition)
        {
            Condition = condition;
        }

        public IfElse(Func<ActivityExecutionContext, bool> condition)
        {
            Condition = new CodeExpression<bool>(condition);
        }

        [ActivityProperty(Hint = "The expression to evaluate. The evaluated value will be used to switch on.")]
        public IWorkflowExpression<bool>? Condition
        {
            get => GetState<IWorkflowExpression<bool>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var result = await context.EvaluateAsync(Condition, cancellationToken);
            var outcome = result ? OutcomeNames.True : OutcomeNames.False;

            return Done(OutcomeNames.Done, outcome);
        }
    }
}