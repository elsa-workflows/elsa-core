using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        Task RunAsync(string workflowInstanceId, string? activityId = default, object? input = default, CancellationToken cancellationToken = default);
        
        // /// <summary>
        // /// Run a registered workflow by its ID.
        // /// </summary>
        // Task RunAsync(string workflowDefinitionId, string activityId, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
        //
        // /// <summary>
        // /// Resume a workflow instance.
        // /// </summary>
        // Task ResumeAsync(string workflowInstanceId, string activityId, object? input = default, CancellationToken cancellationToken = default);
    }
}