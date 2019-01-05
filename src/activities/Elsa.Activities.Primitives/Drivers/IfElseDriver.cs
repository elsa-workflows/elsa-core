using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Primitives.Drivers
{
    public class IfElseDriver : ActivityDriver<IfElse>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public IfElseDriver(IWorkflowExpressionEvaluator expressionEvaluator)
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