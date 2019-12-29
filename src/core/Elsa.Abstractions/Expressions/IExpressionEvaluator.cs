using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public interface IExpressionEvaluator
    {
        Task<object> EvaluateAsync(IWorkflowExpression expression, ActivityExecutionContext context, CancellationToken cancellationToken = default);
        Task<T> EvaluateAsync<T>(IWorkflowExpression<T> expression, ActivityExecutionContext context, CancellationToken cancellationToken = default);
    }
}