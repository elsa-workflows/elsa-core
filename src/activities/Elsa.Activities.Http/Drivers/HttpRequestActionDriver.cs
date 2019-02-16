using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Http.Drivers
{
    public class HttpRequestActionDriver : ActivityDriver<HttpRequestAction>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public HttpRequestActionDriver(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(HttpRequestAction activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return Endpoint("Done");
        }
    }
}