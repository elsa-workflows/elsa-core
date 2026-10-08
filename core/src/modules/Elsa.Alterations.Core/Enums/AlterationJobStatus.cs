namespace Elsa.Alterations.Core.Enums;

/// <summary>
/// The status of an alteration plan for a workflow instance.
/// </summary>
public enum AlterationJobStatus
{
    /// <summary>
    /// The plan is pending execution.
    /// </summary>
    Pending,
    
    /// <summary>
    /// The plan is currently being executed.
    /// </summary>
    Running,
    
    /// <summary>
    /// The plan has been completed.
    /// </summary>
    Completed,
    
    /// <summary>
    /// The job has failed.
    /// </summary>
    Failed
}