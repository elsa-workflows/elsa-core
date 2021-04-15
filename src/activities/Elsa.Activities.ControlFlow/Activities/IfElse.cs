using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Scripting.JavaScript.Services;
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
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public IfElse(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "The expression to evaluate. The evaluated value will be used to switch on.")]
        public WorkflowExpression<bool> ConditionExpression
        {
            get => GetState(() => new WorkflowExpression<bool>(JavaScriptExpressionEvaluator.SyntaxName, "true"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var result = await expressionEvaluator.EvaluateAsync(
                ConditionExpression,
                workflowContext,
                cancellationToken
            );
            return Outcome(result ? OutcomeNames.True : OutcomeNames.False);
        }
    }
}