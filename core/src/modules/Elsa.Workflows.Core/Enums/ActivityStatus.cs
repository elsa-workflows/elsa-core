namespace Elsa.Workflows;

/// <summary>
/// Represents the status of an activity.
/// </summary>
public enum ActivityStatus
{
    /// <summary>
    /// The activity is in the Pending state.
    /// </summary>
    Pending,
    
    /// <summary>
    /// The activity is in the Running state. While in this state, the activity is not necessarily being actively executed. This state represents a logical status rather than a physical action.
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