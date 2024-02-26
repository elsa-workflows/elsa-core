using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Requests;

/// <summary>
/// Represents a request to bulk cancel workflow instances.
/// </summary>
public class BulkCancelWorkflowInstancesRequest
{
    /// <summary>
    /// Gets or sets the IDs of the workflow instances to delete.
    /// </summary>
    public ICollection<string>? Ids { get; set; }

    /// <summary>
    /// Gets or sets the definition version id.
    /// </summary>
    public string? DefinitionVersionId { get; set; }

    /// <summary>
    /// Represents the ID of a workflow definition.
    /// </summary>
    /// <remarks>This should be used in combination with <see cref="VersionOptions"/></remarks>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// Represents the version options for getting a specific version.
    /// </summary>
    /// <remarks>This should be used in combination with <see cref="DefinitionId"/></remarks>
    public VersionOptions? VersionOptions { get; set; }
    
}
