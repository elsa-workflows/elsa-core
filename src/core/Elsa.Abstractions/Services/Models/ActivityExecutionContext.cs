using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(WorkflowExecutionContext workflowExecutionContext, Variable? input = null)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            Input = input;
        }
        
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public Variable? Input { get; }

        public Task<object> EvaluateAsync(IWorkflowExpression expression, CancellationToken cancellationToken = default) 
            => WorkflowExecutionContext.EvaluateAsync(expression, this, cancellationToken);
        
        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, CancellationToken cancellationToken = default) 
            => WorkflowExecutionContext.EvaluateAsync(expression, this, cancellationToken);

        public void SetVariable(string name, object value) => WorkflowExecutionContext.SetVariable(name, value);
        public T GetVariable<T>(string name) => WorkflowExecutionContext.GetVariable<T>(name);
        public object GetVariable(string name) => WorkflowExecutionContext.GetVariable(name);
    }
}
