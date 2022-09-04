using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowStateStore
{
    ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default);
    ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default);
}