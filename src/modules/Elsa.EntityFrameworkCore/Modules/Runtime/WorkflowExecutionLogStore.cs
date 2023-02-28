using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> _store;
    
    /// <summary>
    /// Constructor
    /// </summary>
    
    public EFCoreWorkflowExecutionLogStore(EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store) => _store = store;

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
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