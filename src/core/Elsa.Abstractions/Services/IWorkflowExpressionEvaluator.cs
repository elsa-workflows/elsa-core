using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowExpressionEvaluator
    {
        Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}