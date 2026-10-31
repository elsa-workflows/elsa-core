using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Models;

public record WorkflowGraphFindResult(WorkflowDefinition? WorkflowDefinition, WorkflowGraph? WorkflowGraph)
{
    public bool WorkflowDefinitionExists => WorkflowDefinition != null;
    public bool WorkflowGraphExists => WorkflowGraph != null;
}