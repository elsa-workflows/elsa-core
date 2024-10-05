using Elsa.Common;

namespace Elsa.Workflows.Runtime;

/// Represents a sink for storing log records.
public interface ILogRecordSink<in T> where T : ILogRecord
{
    /// Persists the execution logs of a workflow.
    Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}