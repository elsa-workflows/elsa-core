using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        Task<WorkflowExecutionContext> RunWorkflowAsync(Workflow workflow, string? activityId = default, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default);

        Task<WorkflowExecutionContext?> RunWorkflowInstanceAsync(string workflowInstanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);

        Task<WorkflowExecutionContext?> RunWorkflowInstanceAsync(WorkflowInstance workflowInstance, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Run a registered workflow by its ID.
        /// </summary>
        Task<WorkflowExecutionContext> RunWorkflowDefinitionAsync(string workflowDefinitionId, string? activityId = default, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default);

        // /// <summary>
        // /// Resume a workflow instance.
        // /// </summary>
        // Task ResumeAsync(string workflowInstanceId, string activityId, object? input = default, CancellationToken cancellationToken = default);
    }
}