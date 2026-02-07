using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// A summary of a workflow definition.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionSummary : LinkedEntity
{
    public string DefinitionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string MaterializerName { get; set; } = default!;
}