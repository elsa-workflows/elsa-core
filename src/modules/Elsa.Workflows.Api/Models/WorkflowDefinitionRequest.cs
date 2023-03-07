using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

internal class WorkflowDefinitionRequest
{
    public string? DefinitionId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Version { get; set; }
    public ICollection<VariableDefinition>? Variables { get; set; }
    public ICollection<InputDefinition>? Inputs { get; set; }
    public ICollection<OutputDefinition>? Outputs { get; set; }
    public ICollection<string>? Outcomes { get; set;  }
    public IDictionary<string, object>? CustomProperties { get; set; }
    public IActivity? Root { get; set; }
    public WorkflowOptions? Options { get; set; }
    public bool Publish { get; set; }
    public bool? UsableAsActivity { get; set; }
}