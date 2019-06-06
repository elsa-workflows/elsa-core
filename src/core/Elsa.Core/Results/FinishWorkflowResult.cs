using Elsa.Models;
using Elsa.Results;
using NodaTime;

namespace Elsa.Core.Results
{
    public class FinishWorkflowResult : ActivityExecutionResult
    {
        private readonly Instant instant;

        public FinishWorkflowResult(Instant instant)
        {
            this.instant = instant;
        }
        
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            workflowContext.Finish(instant);
        }
    }
}