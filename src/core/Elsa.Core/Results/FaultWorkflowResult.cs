using System;
using Elsa.Models;
using NodaTime;

namespace Elsa.Results
{
    public class FaultWorkflowResult : ActivityExecutionResult
    {
        private readonly string errorMessage;
        private readonly Instant instant;

        public FaultWorkflowResult(Exception exception, Instant instant) : this(exception.Message, instant)
        {
        }
        
        public FaultWorkflowResult(string errorMessage, Instant instant)
        {
            this.errorMessage = errorMessage;
            this.instant = instant;
        }
        
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            var activity = workflowContext.CurrentActivity;
            workflowContext.Fault(errorMessage, activity, instant);
        }
    }
}