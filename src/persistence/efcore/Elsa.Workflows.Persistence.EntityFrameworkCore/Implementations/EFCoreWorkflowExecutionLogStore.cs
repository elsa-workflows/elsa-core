using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Services;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly IStore<WorkflowExecutionLogRecord> _store;
    public EFCoreWorkflowExecutionLogStore(IStore<WorkflowExecutionLogRecord> store) => _store = store;
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);
}