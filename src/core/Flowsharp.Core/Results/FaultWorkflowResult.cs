using System;
using Flowsharp.Models;

namespace Flowsharp.Results
{
    public class FaultWorkflowResult : ActivityExecutionResult
    {
        private readonly string errorMessage;

        public FaultWorkflowResult(Exception exception) : this(exception.Message)
        {
        }
        
        public FaultWorkflowResult(string errorMessage)
        {
            this.errorMessage = errorMessage;
        }
        
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            var activity = workflowContext.CurrentActivity;
            workflowContext.Fault(errorMessage, activity);
        }
    }
}