using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Services.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(ProcessExecutionContext processExecutionContext, Variable? input = null)
        {
            ProcessExecutionContext = processExecutionContext;
            Input = input;
        }
        
        public ProcessExecutionContext ProcessExecutionContext { get; }
        public Variable? Input { get; }

        public Task<object> EvaluateAsync(IWorkflowExpression expression, CancellationToken cancellationToken = default) 
            => ProcessExecutionContext.EvaluateAsync(expression, this, cancellationToken);
        
        public Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, CancellationToken cancellationToken = default) 
            => ProcessExecutionContext.EvaluateAsync(expression, this, cancellationToken);

        public void SetVariable(string name, object value) => ProcessExecutionContext.SetVariable(name, value);
        public T GetVariable<T>(string name) => ProcessExecutionContext.GetVariable<T>(name);
        public object GetVariable(string name) => ProcessExecutionContext.GetVariable(name);
    }
}
