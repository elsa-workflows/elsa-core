using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Primitives.Activities;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;

namespace Flowsharp.Activities.Primitives.Handlers
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
            return TriggerEndpoint(result ? "True" : "False");
        }
    }
}