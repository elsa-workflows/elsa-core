namespace Elsa.Models;

/// <summary>
/// Represents the current workflow status.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// The workflow is in its initial state and has not yet run,
    /// </summary>
    Idle,
    
    /// <summary>
    /// The workflow is running (although it may be waiting for external stimuli).   
    /// </summary>
    Running,
    
    /// <summary>
    /// The workflow completed and is no longer running.
    /// </summary>
    Finished,
    
    /// <summary>
    /// The workflow was cancelled and is no longer running.
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// The workflow has faulted and is no longer running.
    /// </summary>
    Faulted,
}