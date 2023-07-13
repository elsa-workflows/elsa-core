namespace Elsa.Workflows.Core;

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