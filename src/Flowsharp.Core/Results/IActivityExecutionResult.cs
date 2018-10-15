using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Services;

namespace Flowsharp.Results
{
    public interface IActivityExecutionResult
    {
        Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken);
    }
}
