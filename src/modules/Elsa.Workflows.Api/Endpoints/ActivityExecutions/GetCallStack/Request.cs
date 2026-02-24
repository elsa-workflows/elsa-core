namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.GetCallStack;

/// <summary>
/// Request for retrieving the call stack of an activity execution.
/// </summary>
public class Request
{
    /// <summary>
    /// If true (default), includes parent workflow activities across workflow boundaries.
    /// If false, stops at workflow boundaries.
    /// </summary>
    public bool? IncludeCrossWorkflowChain { get; set; }

    /// <summary>
    /// The number of items to skip (for pagination).
    /// Applied after ordering from root to leaf.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// The maximum number of items to return (for pagination).
    /// Recommended maximum: 100.
    /// </summary>
    public int? Take { get; set; }
}
