using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class SetVariableResult : IActivityExecutionResult
    {
        public SetVariableResult(string name, object value)
        {
            Name = name;
            Value = value;
        }
        
        public string Name { get; }
        public object Value { get; }
        
        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            workflowExecutionContext.SetVariable(Name, Value);
            return Task.CompletedTask;
        }
    }
}