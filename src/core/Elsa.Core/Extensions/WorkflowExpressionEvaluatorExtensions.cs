using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Extensions
{
    public static class WorkflowExpressionEvaluatorExtensions
    {
        public static async Task<T> EvaluateAsync<T>(this IWorkflowExpressionEvaluator evaluator, IWorkflowExpression<T> expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            return (T)await evaluator.EvaluateAsync(expression, typeof(T), workflowExecutionContext, cancellationToken);
        }
    }
}