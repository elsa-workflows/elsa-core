using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    public class IfElse : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public IfElse(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public WorkflowExpression<bool> Expression
        {
            get => GetState(() => new JavaScriptExpression<bool>("true"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var result = await expressionEvaluator.EvaluateAsync(Expression, workflowContext, cancellationToken);
            return Outcome(result ? OutcomeNames.True: OutcomeNames.False);
        }
    }
}