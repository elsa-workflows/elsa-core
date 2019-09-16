using Elsa.Services;
using Elsa.Services.Models;

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