using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ListConsumingWorkflows;

/// <summary>
/// Request to list consuming workflows for a given workflow definition.
/// </summary>
internal class Request
{
    /// <summary>
    /// The workflow definition ID.
    /// </summary>
    public string? DefinitionId { get; set; }
    
    /// <summary>
    /// The version options (e.g., "Latest", "Published", "LatestOrPublished").
    /// </summary>
    public string? VersionOptions { get; set; }
    
    /// <summary>
    /// The workflow definition version ID (takes precedence over DefinitionId + VersionOptions).
    /// </summary>
    public string? DefinitionVersionId { get; set; }
}

/// <summary>
/// Response containing the list of consuming workflow definitions.
/// </summary>
internal class Response
{
    /// <summary>
    /// The list of workflow definition summaries that consume the specified workflow.
    /// </summary>
    public ICollection<WorkflowDefinitionSummary> ConsumingWorkflows { get; set; } = new List<WorkflowDefinitionSummary>();
}
