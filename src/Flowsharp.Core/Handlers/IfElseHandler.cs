using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Expressions;
using Flowsharp.Models;
using Flowsharp.Results;
using Flowsharp.Services;

namespace Flowsharp.Handlers
{
    public class IfElseHandler : ActivityHandler<IfElse>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public IfElseHandler(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(IfElse activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var result = await expressionEvaluator.EvaluateAsync(activity.ConditionExpression, workflowContext, cancellationToken);
            return ActivateEndpoint(result ? "True" : "False");
        }
    }
}