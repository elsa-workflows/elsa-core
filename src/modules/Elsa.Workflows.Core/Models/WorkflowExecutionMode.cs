namespace Elsa.Workflows.Core.Models;

/// <summary>
/// The runtime mode to use when running thr workflow.
/// </summary>
public enum WorkflowExecutionMode
{
    /// <summary>
    /// The workflow will be executed synchronously, blocking the current thread until the workflow completes or gets suspended.
    /// </summary>
    Synchronous,
    
    /// <summary>
    /// The workflow will be executed asynchronously, returning immediately.
    /// </summary>
    Asynchronous
}