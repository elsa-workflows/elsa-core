using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.Implementations;

public class MemoryWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly MemoryStore<WorkflowExecutionLogRecord> _store;

    public MemoryWorkflowExecutionLogStore(MemoryStore<WorkflowExecutionLogRecord> store)
    {
        _store = store;
    }
    
    public Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records);
        return Task.CompletedTask;
    }

    public Task<Page<WorkflowExecutionLogRecord>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var page = _store
            .FindMany(
                x => x.WorkflowInstanceId == workflowInstanceId, 
                x => x.Timestamp)
            .Paginate();
        
        return Task.FromResult(page);
    }
}