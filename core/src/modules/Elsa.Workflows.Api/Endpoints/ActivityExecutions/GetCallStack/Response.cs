using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Api.Endpoints.ActivityExecutions.GetCallStack;

/// <summary>
/// Response containing the call stack for an activity execution with pagination support.
/// </summary>
public class Response
{
    /// <summary>
    /// The ID of the activity execution that was requested.
    /// </summary>
    public string ActivityExecutionId { get; set; } = null!;

    /// <summary>
    /// The activity execution records in the call stack (ordered from root to current activity).
    /// This may be a subset if pagination is applied.
    /// </summary>
    public ICollection<ActivityExecutionRecord> Items { get; set; } = new List<ActivityExecutionRecord>();

    /// <summary>
    /// The total number of items in the full call stack chain.
    /// </summary>
    public long TotalCount { get; set; }
}
