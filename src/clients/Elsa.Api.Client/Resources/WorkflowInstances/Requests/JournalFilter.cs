namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

/// <summary>
/// Represents a request to list journal records.
/// </summary>
public class JournalFilter
{
    /// <summary>
    /// Gets or sets the activity ids to filter by.
    /// </summary>
    public ICollection<string>? ActivityIds { get; set; }

    /// <summary>
    /// Gets or sets the event types to filter by.
    /// </summary>
    public ICollection<string>? EventNames { get; set; }
}