using System.Collections.Generic;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Models;

public class WorkflowDefinitionRequest
{
    public string? DefinitionId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; } = default!;
    public int? Version { get; set; }
    public ICollection<VariableDefinition>? Variables { get; set; } = default!;
    public IDictionary<string, object>? Metadata { get; set; } = default!;
    public IDictionary<string, object>? ApplicationProperties { get; set; } = default!;
    public IActivity? Root { get; set; } = default!;
    public bool Publish { get; set; }
}