namespace Elsa.Workflows;

/// <summary>
/// Represents the workflow sub-status.
/// </summary>
public enum WorkflowSubStatus
{
    /// <summary>
    /// The workflow is pending execution.
    /// </summary>
    Pending,
    
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

    /// <summary>
    /// The workflow's last execution cycle was force-cancelled by the runtime during a graceful drain
    /// (deadline breach or operator force). The instance is resumable and is picked up by the shell-activation
    /// recovery scan on the next runtime generation. Distinct from <see cref="Cancelled"/> (user-initiated) and
    /// from instances recovered by the timeout-based crash-recovery task, which remain in the <see cref="Executing"/>
    /// sub-status with a stale liveness timestamp.
    /// </summary>
    Interrupted,
}