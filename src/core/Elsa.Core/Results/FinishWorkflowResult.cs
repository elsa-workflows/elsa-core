using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
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