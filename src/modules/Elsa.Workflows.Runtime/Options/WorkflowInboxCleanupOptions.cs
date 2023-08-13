namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for cleaning up the workflow inbox.
/// </summary>
public class WorkflowInboxCleanupOptions
{
    /// <summary>
    /// The sweep interval at which to clean up the workflow inbox.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The number of messages to clean up per sweep.
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}