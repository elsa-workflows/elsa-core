using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Core.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Primitives.Drivers
{
    public class SwitchDriver : ActivityDriver<Switch>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SwitchDriver(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(Switch activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var result = await expressionEvaluator.EvaluateAsync(activity.Expression, workflowContext, cancellationToken);
            return Endpoint(result);
        }
    }
}