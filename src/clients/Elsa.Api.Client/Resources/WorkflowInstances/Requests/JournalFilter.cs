namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

/// Represents a request to list journal records.
public class JournalFilter
{
    /// Gets or sets the activity IDs to filter by.
    public ICollection<string>? ActivityIds { get; set; }

    /// Gets or sets the activity node IDs to filter by.
    public ICollection<string>? ActivityNodeIds { get; set; }

    /// Gets or sets the activity types to filter out.
    public ICollection<string>? ExcludedActivityTypes { get; set; }

    /// Gets or sets the event types to filter by.
    public ICollection<string>? EventNames { get; set; }
}