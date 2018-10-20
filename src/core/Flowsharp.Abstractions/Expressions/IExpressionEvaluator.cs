using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Expressions
{
    public interface IExpressionEvaluator
    {
        string Syntax { get; }
        Task<T> EvaluateAsync<T>(string expression, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}