using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Contains information about a workflow run, such as <see cref="WorkflowState"/>.
/// </summary>
public record RunWorkflowResult(WorkflowState WorkflowState);