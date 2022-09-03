using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.Implementations;

public class MemoryWorkflowStateStore : IWorkflowStateStore
{
    public MemoryWorkflowStateStore()
    {
        
    }
    
    public Task<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(WorkflowState state, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}