namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a sink for storing activity execution log records.
/// </summary>
public interface IActivityExecutionLogSink
{
    /// <summary>
    /// Persists the activity logs of a workflow.
    /// </summary>
    Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}