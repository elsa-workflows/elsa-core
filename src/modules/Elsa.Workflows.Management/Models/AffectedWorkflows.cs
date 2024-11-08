using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Models;

public record AffectedWorkflows(ICollection<WorkflowDefinition> WorkflowDefinitions);