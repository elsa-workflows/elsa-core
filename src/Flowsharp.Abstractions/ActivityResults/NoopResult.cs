using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    /// <summary>
    /// A result that does nothing.
    /// </summary>
    public class NoopResult : ActivityExecutionResult
    {
        protected override void Execute(WorkflowExecutionContext workflowContext)
        {
            // Noop.
        }
    }
}
