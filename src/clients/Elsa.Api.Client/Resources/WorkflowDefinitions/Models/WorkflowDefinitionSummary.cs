using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// A summary of a workflow definition.
/// </summary>
[PublicAPI]
public record WorkflowDefinitionSummary : LinkedEntity
{
    public string Id { get; set; }
    public string DefinitionId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public string MaterializerName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}