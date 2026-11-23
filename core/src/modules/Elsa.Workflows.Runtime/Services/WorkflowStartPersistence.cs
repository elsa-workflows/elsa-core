using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Mappers;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Atomically inserts the prepared execution at the same key its subsequent commits will update.
/// </summary>
public class WorkflowStartPersistence(
    IWorkflowInstanceStore instanceStore,
    IWorkflowStateExtractor stateExtractor,
    WorkflowStateMapper stateMapper) : IWorkflowStartPersistence
{
    public async Task CreateAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        var initialState = stateExtractor.Extract(context);

        // The first activity has already been scheduled. If the process stops before its first
        // commit, interrupted execution recovery can dispatch this persisted state.
        initialState.IsExecuting = true;
        var instance = stateMapper.Map(initialState)!;
        await instanceStore.AddAsync(instance, cancellationToken);
    }
}
