using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Services
{
    public interface IWorkflowInvoker
    {
        Task InvokeAsync(WorkflowExecutionContext workflowContext, string startActivityId, CancellationToken cancellationToken);
    }
}
