using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Services;

public class MemoryWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly MemoryStore<WorkflowExecutionLogRecord> _store;

    public MemoryWorkflowExecutionLogStore(MemoryStore<WorkflowExecutionLogRecord> store)
    {
        _store = store;
    }
    
    public Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
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