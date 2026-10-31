namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides access to the workflow execution context currently running on the async call path.
/// </summary>
public interface IWorkflowDispatchOutboxAccessor
{
    /// <summary>
    /// Gets or sets the current workflow execution context.
    /// </summary>
    WorkflowExecutionContext? WorkflowExecutionContext { get; set; }
}
