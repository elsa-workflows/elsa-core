using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class OutputResult : ActivityExecutionResult
    {
        public OutputResult(Variable value)
        {
            Value = value;
        }
        
        public Variable Value { get; }
        
        protected override void Execute(IWorkflowRunner runner, WorkflowExecutionContext workflowContext)
        {
            workflowContext.CurrentActivity.Output = Value;
        }
    }
}