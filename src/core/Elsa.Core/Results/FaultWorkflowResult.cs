using System;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Results
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
            workflowContext.Fault(workflowContext.CurrentActivity, errorMessage);
        }
    }
}