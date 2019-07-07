using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Activities.Primitives
{
    public class IfElse : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public IfElse(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public WorkflowExpression<bool> ConditionExpression
        {
            get => GetState(() => new JavaScriptExpression<bool>("true"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var result = await expressionEvaluator.EvaluateAsync(ConditionExpression, workflowContext, cancellationToken);
            return Outcome(result ? OutcomeNames.True: OutcomeNames.False);
        }
    }
}