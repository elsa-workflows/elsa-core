namespace Elsa.Workflows.Core;

/// <summary>
/// Represents the status of an activity.
/// </summary>
public enum ActivityStatus
{
    /// <summary>
    /// The activity is in the Running state. Note that event if an activity is running, it may not be executing.
    /// </summary>
    Running,
    
    /// <summary>
    /// The activity is in the Completed state.
    /// </summary>
    Completed,
    
    /// <summary>
    /// The activity is in the Canceled state.
    /// </summary>
    Canceled,
    
    /// <summary>
    /// The activity is in the Faulted state.
    /// </summary>
    Faulted
}