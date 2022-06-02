using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly IStore<WorkflowsDbContext, WorkflowExecutionLogRecord> _store;
    public EFCoreWorkflowExecutionLogStore(IStore<WorkflowsDbContext, WorkflowExecutionLogRecord> store) => _store = store;
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    public async Task<Page<WorkflowExecutionLogRecord>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(
            x => x.WorkflowInstanceId == workflowInstanceId,
            x => x.Timestamp,
            OrderDirection.Ascending,
            pageArgs,
            cancellationToken);

        return records;
    }
}