using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a workflow definition and its workflow.
/// </summary>
/// <param name="Definition">The workflow definition.</param>
/// <param name="Workflow">The workflow materialized from its workflow definition.</param>
public record WorkflowDefinitionResult(WorkflowDefinition Definition, Workflow Workflow);