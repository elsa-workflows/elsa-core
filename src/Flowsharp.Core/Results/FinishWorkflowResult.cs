using Flowsharp.Models;
using Flowsharp.Services;

namespace Flowsharp.Results
{
    public class FinishWorkflowResult : ActivityExecutionResult
    {
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            workflowContext.Finish();
        }
    }
}