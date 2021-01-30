using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Revives a faulted workflow
    /// </summary>
    public interface IWorkflowReviver
    {
        Task<WorkflowInstance> ReviveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> ReviveAndRunAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> ReviveAndQueueAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
    }
}