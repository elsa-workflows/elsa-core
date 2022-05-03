using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    public Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
}