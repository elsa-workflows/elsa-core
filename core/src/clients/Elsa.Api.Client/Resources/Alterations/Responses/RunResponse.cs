using Elsa.Api.Client.Resources.Alterations.Models;

namespace Elsa.Api.Client.Resources.Alterations.Responses;

/// <summary>
/// The response to the Run endpoint
/// </summary>
public class RunResponse
{
    /// <summary>
    /// The alteration results of a Run request
    /// </summary>
    private ICollection<RunAlterationsResult> Results { get; set; } = new List<RunAlterationsResult>();
}