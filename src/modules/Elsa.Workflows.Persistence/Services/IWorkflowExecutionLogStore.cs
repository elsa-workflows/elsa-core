using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Persistence.Services;

public interface IWorkflowExecutionLogStore
{
    Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default);
}