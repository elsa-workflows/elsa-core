using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Expressions
{
    public interface IWorkflowExpressionEvaluator
    {
        Task<T> EvaluateAsync<T>(WorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}