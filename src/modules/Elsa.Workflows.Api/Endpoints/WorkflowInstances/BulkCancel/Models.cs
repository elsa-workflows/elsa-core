using System.Text.Json.Serialization;
using Elsa.Common.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.BulkCancel;

public class Request
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

public class Response(int cancelledCount)
{
    [JsonPropertyName("cancelled")] public int CancelledCount { get; } = cancelledCount;
}