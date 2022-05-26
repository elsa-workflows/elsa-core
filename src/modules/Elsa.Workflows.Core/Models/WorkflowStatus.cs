namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents the current workflow status.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// The workflow is running (although it may be waiting for external stimuli).   
    /// </summary>
    Running,
    
    /// <summary>
    /// The workflow completed and is no longer running.
    /// </summary>
    Finished
}

/// <summary>
/// Represents the workflow sub-status.
/// </summary>
public enum WorkflowSubStatus
{
    /// <summary>
    /// The workflow is currently executing.   
    /// </summary>
    Executing,
    
    /// <summary>
    /// The workflow is currently suspended and waiting for external stimuli to resume.   
    /// </summary>
    Suspended,
    
    /// <summary>
    /// The workflow completed successfully.
    /// </summary>
    Finished,
    
    /// <summary>
    /// The workflow was cancelled.
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// The workflow has faulted.
    /// </summary>
    Faulted,
}