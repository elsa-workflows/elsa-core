using System.Collections.Concurrent;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class MemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _workflowStates = new();

    /// <inheritdoc />
    public ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        _workflowStates.AddOrUpdate(id, state, (_, _) => state);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        var state = _workflowStates.TryGetValue(id, out var value) ? value : default;
        return new(state);
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default)
    {
        var query = _workflowStates.Values.AsQueryable().Where(x => x.Status == WorkflowStatus.Running);

        if (args.DefinitionId != null) query = query.Where(x => x.DefinitionId == args.DefinitionId);
        if (args.Version != null) query = query.Where(x => x.DefinitionVersion == args.Version);
        if (args.CorrelationId != null) query = query.Where(x => x.CorrelationId == args.CorrelationId);

        return new(query.Count());
    }
}