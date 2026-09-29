namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Filter state shared by recent REST requests, live SignalR subscriptions, and URL query state.
/// </summary>
public class ConsoleLogFilter
{
    /// <summary>
    /// Gets or sets the stable backend source ID.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Gets or sets the selected stream. A null value means both stdout and stderr.
    /// </summary>
    public ConsoleLogStream? Stream { get; set; }

    /// <summary>
    /// Gets or sets the server-side query filter.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the workflow instance ID scope filter.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the activity execution instance ID scope filter.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the logical activity ID scope filter.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the activity node ID scope filter.
    /// </summary>
    public string? ActivityNodeId { get; set; }

    /// <summary>
    /// Gets or sets the inclusive UTC start time.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Gets or sets the inclusive UTC end time.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Gets or sets the requested line count.
    /// </summary>
    public int? Limit { get; set; }
}
