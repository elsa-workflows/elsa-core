using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Revives a faulted workflow.
    /// </summary>
    public interface IWorkflowReviver
    {
        Task<WorkflowInstance> ReviveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task<RunWorkflowResult> ReviveAndRunAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> ReviveAndQueueAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
    }
}