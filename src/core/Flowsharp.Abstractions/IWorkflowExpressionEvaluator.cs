using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Expressions;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IWorkflowExpressionEvaluator
    {
        Task<T> EvaluateAsync<T>(WorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}