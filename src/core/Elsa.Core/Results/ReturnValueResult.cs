using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Results
{
    public class ReturnValueResult : ActivityExecutionResult
    {
        private readonly object value;

        public ReturnValueResult(object value)
        {
            this.value = value;
        }
        
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            workflowContext.SetLastResult(value);
        }
    }
}