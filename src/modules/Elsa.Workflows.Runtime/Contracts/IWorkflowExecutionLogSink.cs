namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a sink for storing workflow execution log records.
/// </summary>
public interface IWorkflowExecutionLogSink
{
    /// <summary>
    /// Persists the execution logs of a workflow.
    /// </summary>
    Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}