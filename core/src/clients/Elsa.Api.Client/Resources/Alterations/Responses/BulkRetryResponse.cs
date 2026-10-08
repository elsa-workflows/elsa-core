using Elsa.Api.Client.Resources.Alterations.Models;

namespace Elsa.Api.Client.Resources.Alterations.Responses;

/// <summary>
/// Represents a response to bulk retry workflow instances.
/// </summary>
public class BulkRetryResponse
{
    /// <summary>
    /// The alterations that resulted from the bulk retry request
    /// </summary>
    public ICollection<RunAlterationsResult> Results { get;set; } = new List<RunAlterationsResult>();
}