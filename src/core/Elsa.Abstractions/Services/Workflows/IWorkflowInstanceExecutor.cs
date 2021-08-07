using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Executes a single workflow instance for the specified activityId.
    /// </summary>
    public interface IWorkflowInstanceExecutor
    {
        Task<RunWorkflowResult> ExecuteAsync(string workflowInstanceId, string? activityId, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        Task<RunWorkflowResult> ExecuteAsync(WorkflowInstance workflowInstance, string? activityId, WorkflowInput? input = default, CancellationToken cancellationToken = default);
    }
}