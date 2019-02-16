using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    /// <summary>
    /// Implement this in order to receive various events related to workflow execution
    /// </summary>
    public interface IWorkflowEventHandler
    {
        Task ActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, CancellationToken cancellationToken);
    }
}