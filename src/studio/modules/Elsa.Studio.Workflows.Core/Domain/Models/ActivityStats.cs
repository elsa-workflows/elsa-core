namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents a report of activity executions.
/// </summary>
public class ActivityStats
{
    /// <summary>
    /// The number of times the activity has been started.
    /// </summary>
    public long Started { get; set; }
    
    /// <summary>
    /// The number of times the activity has been completed.
    /// </summary>
    public long Completed { get; set; }
    
    /// <summary>
    /// The number of times the activity has been uncompleted.
    /// </summary>
    public long Uncompleted { get; set; }
    
    /// <summary>
    /// Whether the activity is blocked.
    /// </summary>
    public bool Blocked { get; set; }
    
    /// <summary>
    /// Whether the activity has faulted.
    /// </summary>
    public bool Faulted { get; set; }

    /// <summary>
    /// A collection of metadata associated with the activity statistics.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }
}