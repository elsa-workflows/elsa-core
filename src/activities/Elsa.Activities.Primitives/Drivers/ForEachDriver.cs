using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Primitives.Drivers
{
    public class ForEachDriver : ActivityDriver<ForEach>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public ForEachDriver(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(ForEach activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return Endpoint("Done");
        }
    }
}