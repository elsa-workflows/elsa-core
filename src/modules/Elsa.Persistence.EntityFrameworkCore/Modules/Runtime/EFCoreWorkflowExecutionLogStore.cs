using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly Store<RuntimeDbContext, WorkflowExecutionLogRecord> _store;
    public EFCoreWorkflowExecutionLogStore(Store<RuntimeDbContext, WorkflowExecutionLogRecord> store) => _store = store;
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