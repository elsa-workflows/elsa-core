using Flowsharp.Models;

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