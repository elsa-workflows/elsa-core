using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly Store<RuntimeElsaDbContext, WorkflowExecutionLogRecord> _store;
    public EFCoreWorkflowExecutionLogStore(Store<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store) => _store = store;
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