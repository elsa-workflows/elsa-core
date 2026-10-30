namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;

/// <summary>
/// Options controlling automatic polling behavior for workflow instance lists.
/// </summary>
public class WorkflowInstanceListPollingOptions
{
    /// <summary>
    /// Whether polling is enabled by default.
    /// </summary>
    public bool IsEnabledByDefault { get; set; }

    /// <summary>
    /// Interval in seconds between automatic refreshes.
    /// </summary>
    public int IntervalSeconds { get; set; }
}