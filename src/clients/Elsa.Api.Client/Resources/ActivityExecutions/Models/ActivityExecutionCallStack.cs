namespace Elsa.Api.Client.Resources.ActivityExecutions.Models;

/// <summary>
/// Represents a call stack (execution chain) for a given activity execution.
/// </summary>
public class ActivityExecutionCallStack
{
    /// <summary>
    /// The ID of the activity execution that was requested.
    /// </summary>
    public string ActivityExecutionId { get; set; } = null!;

    /// <summary>
    /// The activity execution records in the call stack (ordered from root to current activity).
    /// </summary>
    public ICollection<ActivityExecutionRecord> Items { get; set; } = new List<ActivityExecutionRecord>();

    /// <summary>
    /// The total number of items in the full call stack chain.
    /// </summary>
    public long TotalCount { get; set; }
}
