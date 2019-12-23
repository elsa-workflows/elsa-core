using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
        RuntimeDescription = "x => !!x.state.expression ? `Evaluate <strong>${ x.state.expression.expression }</strong> and continue execution depending on the result.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
    )]
    public class IfElse : Activity
    {
        [ActivityProperty(Hint = "The expression to evaluate. The evaluated value will be used to switch on.")]
        public IWorkflowExpression<bool> Condition
        {
            get => GetState<IWorkflowExpression<bool>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var result = await context.EvaluateAsync(Condition, cancellationToken);
            return Outcome(result ? OutcomeNames.True : OutcomeNames.False);
        }
    }
}