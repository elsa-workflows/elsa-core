using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public interface IWorkflowExpressionEvaluator
    {
        Task<object> EvaluateAsync(IWorkflowExpression expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);
        Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);
    }
}