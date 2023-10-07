namespace Elsa.Alterations.Core.Enums;

/// <summary>
/// The status of an alteration plan.
/// </summary>
public enum AlterationPlanStatus
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
    /// The plan has failed.
    /// </summary>
    Failed
}