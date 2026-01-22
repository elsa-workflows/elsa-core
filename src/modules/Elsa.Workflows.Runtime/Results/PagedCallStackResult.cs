using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Results;

/// <summary>
/// Represents a paginated result containing activity execution call stack records.
/// </summary>
public class PagedCallStackResult
{
    /// <summary>
    /// The activity execution records in the call stack (ordered from root to current activity).
    /// This may be a subset if pagination is applied.
    /// </summary>
    public ICollection<ActivityExecutionRecord> Items { get; set; } = new List<ActivityExecutionRecord>();

    /// <summary>
    /// The total number of items in the full call stack chain.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The number of items skipped (for pagination).
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// The maximum number of items returned (for pagination).
    /// </summary>
    public int? Take { get; set; }
}
