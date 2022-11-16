using System.Collections.Concurrent;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class MemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _workflowStates = new();

    public ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        _workflowStates.AddOrUpdate(id, state, (_, _) => state);
        return ValueTask.CompletedTask;
    }

    public ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        var state = _workflowStates.TryGetValue(id, out var value) ? value : default;
        return new(state);
    }
}