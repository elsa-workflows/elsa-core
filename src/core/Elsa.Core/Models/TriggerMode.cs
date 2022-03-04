namespace Elsa.Models;

/// <summary>
/// A set of trigger modes. 
/// </summary>
public enum TriggerMode
{
    /// <summary>
    /// The trigger will create a new workflow instance and execute said instance.
    /// </summary>
    WorkflowDefinition,
    
    /// <summary>
    /// The trigger will schedule the trigger for execution within the context of the workflow instance.
    /// </summary>
    WorkflowInstance
}