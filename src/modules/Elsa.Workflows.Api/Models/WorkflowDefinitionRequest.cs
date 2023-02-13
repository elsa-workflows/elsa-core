using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

internal class WorkflowDefinitionRequest
{
    public string? DefinitionId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; } = default!;
    public int? Version { get; set; }
    public ICollection<VariableDefinition>? Variables { get; set; } = default!;
    public ICollection<InputDefinition>? Inputs { get; set; } = default!;
    public ICollection<OutputDefinition>? Outputs { get; set; } = default!;
    public IDictionary<string, object>? CustomProperties { get; set; } = default!;
    public IActivity? Root { get; set; } = default!;
    public WorkflowOptions? Options { get; set; }
    public bool Publish { get; set; }
    public bool? UsableAsActivity { get; set; }
}