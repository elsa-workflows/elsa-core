using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Results
{
    public class FinishWorkflowResult : ActivityExecutionResult
    {
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            workflowContext.Finish();
        }
    }
}