using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives.Activities;
using Elsa.Core.Handlers;
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

        protected override Task<ActivityExecutionResult> OnExecuteAsync(ForEach activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var endpoint = Endpoint("Done");
            return Task.FromResult<ActivityExecutionResult>(endpoint);
        }
    }
}