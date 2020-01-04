using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Expressions
{
    public interface IExpressionHandler
    {
        string Type { get; }
        Task<object> EvaluateAsync(
            IWorkflowExpression expression,
            WorkflowExecutionContext workflowExecutionContext,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken);
    }
}