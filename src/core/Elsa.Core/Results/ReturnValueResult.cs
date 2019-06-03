using Elsa.Models;
using Elsa.Results;

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