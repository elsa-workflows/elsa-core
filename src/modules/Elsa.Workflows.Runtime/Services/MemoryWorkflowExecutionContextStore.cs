using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Stores <see cref="WorkflowExecutionContext"/> in memory.
/// </summary>
public class MemoryWorkflowExecutionContextStore : IWorkflowExecutionContextStore
{
    private readonly MemoryStore<WorkflowExecutionContext> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryActivityExecutionStore"/> class.
    /// </summary>
    public MemoryWorkflowExecutionContextStore(MemoryStore<WorkflowExecutionContext> store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public Task SaveAsync(WorkflowExecutionContext context)
    {
        _store.Save(context, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionContext?> FindAsync(string workflowExecutionContextId)
    {
        var result = _store.Find((context) => context.Id == workflowExecutionContextId);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string workflowExecutionContextId)
    {
        _store.Delete(workflowExecutionContextId);
        return Task.CompletedTask;
    }
}