using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    public class FinishWorkflowResult : ActivityExecutionResult
    {
        protected override void Execute(WorkflowExecutionContext workflowContext)
        {
            workflowContext.Finish();
        }
    }
}