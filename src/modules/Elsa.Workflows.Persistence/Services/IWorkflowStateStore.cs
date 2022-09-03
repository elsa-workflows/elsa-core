using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Persistence.Services;

public interface IWorkflowStateStore
{
    Task<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default);
    Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default);
}