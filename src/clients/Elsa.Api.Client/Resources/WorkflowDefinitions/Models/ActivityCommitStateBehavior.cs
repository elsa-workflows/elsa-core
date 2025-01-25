namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

public enum ActivityCommitStateBehavior
{
    /// <summary>
    /// Never commit state, regardless of the workflow commit state options.
    /// </summary>
    Never,
    
    /// <summary>
    /// Look at the workflow commit state options to determine if state should be committed.
    /// </summary>
    Default,
    
    /// <summary>
    /// Commit state before the activity starts.
    /// </summary>
    Executing,
    
    /// <summary>
    /// Commit state after the activity executes.
    /// </summary>
    Executed,
    
    /// <summary>
    /// Commit state before the activity starts and after the activity executes.
    /// </summary>
    BeforeAndAfterExecution
}