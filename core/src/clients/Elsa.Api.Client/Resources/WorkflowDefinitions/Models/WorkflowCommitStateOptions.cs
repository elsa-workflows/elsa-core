namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

public class WorkflowCommitStateOptions
{
    /// <summary>
    /// Commit workflow state before the workflow starts.
    /// </summary>
    public bool Starting { get; set; }

    /// <summary>
    /// Commit workflow state before an activity executes, unless the activity is configured to not commit state.
    /// </summary>
    public bool ActivityExecuting { get; set; }
    
    /// <summary>
    /// Commit workflow state after an activity executes, unless the activity is configured to not commit state.
    /// </summary>
    public bool ActivityExecuted { get; set; }
}