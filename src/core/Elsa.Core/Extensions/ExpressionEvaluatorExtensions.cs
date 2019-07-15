using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class ExpressionEvaluatorExtensions
    {
        public static async Task<T> EvaluateAsync<T>(this IExpressionEvaluator evaluator, string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            return (T)await evaluator.EvaluateAsync(expression, typeof(T), workflowExecutionContext, cancellationToken);
        }
    }
}