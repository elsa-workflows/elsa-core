using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowExecutionContextFactory
{
    Task<WorkflowExecutionContext> CreateAsync(
        Workflow workflow,
        string instanceId,
        WorkflowState? workflowState,
        IDictionary<string, object>? input = default,
        string? correlationId = default,
        ExecuteActivityDelegate? executeActivityDelegate = default,
        CancellationToken cancellationToken = default);
}