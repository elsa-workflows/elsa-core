using Elsa.Workflows;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Retention.Options;

/// <summary>
/// Retention options
/// </summary>
public class CleanupOptions
{
    /// <summary>
    /// Controls how often the database is checked for workflow instances and execution log records to remove. 
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// The maximum age a workflow instance is allowed to exist before being removed.
    /// </summary>
    public TimeSpan TimeToLive { get; set; }
    
    /// <summary>
    /// The workflow instance filter. Defaults to all finished workflows.
    /// </summary>
    public WorkflowInstanceFilter WorkflowInstanceFilter { get; set; } =
        new()
        {
            WorkflowStatus = WorkflowStatus.Finished
        };
}