using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        ValueTask<WorkflowInstance> RunWorkflowAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowDefinition,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync<T>(
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow;

        ValueTask<WorkflowInstance> RunWorkflowAsync<T>(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default) where T : IWorkflow;

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default);
        
        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflow workflow,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default);
    }
}