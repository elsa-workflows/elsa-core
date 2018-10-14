using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Scripting
{
    public interface IScriptEvaluator
    {
        Task<T> EvaluateAsync<T>(ScriptExpression<T> script, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken);
    }
}