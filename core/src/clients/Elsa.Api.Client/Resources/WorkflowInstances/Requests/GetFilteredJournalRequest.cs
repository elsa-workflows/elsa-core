namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

/// <summary>
/// Represents a request to list journal records.
/// </summary>
public class GetFilteredJournalRequest
{
    /// <summary>
    /// Gets or sets the filter to apply.
    /// </summary>
    public JournalFilter? Filter { get; set; }
}