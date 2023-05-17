using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a request to list workflow definitions.
/// </summary>
public class ListWorkflowDefinitionsRequest
{
    /// <summary>
    /// The page number.
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// The page size.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// The IDs of the workflow definitions to filter by.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }

    /// <summary>
    /// The name of the workflow materializer to filter by.
    /// </summary>
    [AliasAs("materializer")]
    public string? MaterializerName { get; set; }

    /// <summary>
    /// The labels to filter by.
    /// </summary>
    [AliasAs("label")]
    public string[]? Labels { get; set; }
}