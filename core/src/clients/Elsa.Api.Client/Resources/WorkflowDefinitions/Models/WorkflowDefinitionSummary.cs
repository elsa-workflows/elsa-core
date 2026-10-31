using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// A summary of a workflow definition.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionSummary : LinkedEntity
{
    public string DefinitionId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string MaterializerName { get; set; } = null!;
    public bool IsMaterializerAvailable { get; set; }
}