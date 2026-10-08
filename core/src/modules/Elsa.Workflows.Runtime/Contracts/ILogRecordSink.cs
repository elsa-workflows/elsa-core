using Elsa.Common;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a sink for storing log records.
/// </summary>
public interface ILogRecordSink<in T> where T : ILogRecord
{
    /// <summary>
    /// Persists the execution logs of a workflow.
    /// </summary>
    Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}