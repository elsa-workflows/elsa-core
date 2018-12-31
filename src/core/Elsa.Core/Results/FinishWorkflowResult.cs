using Elsa.Models;

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