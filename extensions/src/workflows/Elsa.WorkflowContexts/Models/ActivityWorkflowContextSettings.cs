namespace Elsa.WorkflowContexts.Models;

/// <summary>
/// Provides activity-specific settings for workflow context providers.
/// </summary>
public class ActivityWorkflowContextSettings
{
    /// <summary>
    /// Whether to load the context before executing the activity.
    /// </summary>
    public bool Load { get; set; }
    
    /// <summary>
    /// Whether to save the context after executing the activity.
    /// </summary>
    public bool Save { get; set; }
}