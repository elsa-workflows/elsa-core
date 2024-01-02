using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Models;

/// <summary>
/// Contains information about a workflow run, such as <see cref="WorkflowState"/>.
/// </summary>
public record RunWorkflowResult(WorkflowState WorkflowState, Workflow Workflow, object? Result);

/// <summary>
/// Contains information about a workflow run, such as <see cref="WorkflowState"/>.
/// </summary>
public record RunWorkflowResult<TResult>(WorkflowState WorkflowState, Workflow Workflow, TResult Result);