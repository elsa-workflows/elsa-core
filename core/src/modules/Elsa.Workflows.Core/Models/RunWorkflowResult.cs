using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Models;

/// <summary>
/// Contains information about a workflow run, such as <see cref="WorkflowState"/>.
/// </summary>
public record RunWorkflowResult(WorkflowExecutionContext WorkflowExecutionContext, WorkflowState WorkflowState, Workflow Workflow, object? Result, Journal Journal);

/// <summary>
/// Contains information about a workflow run, such as <see cref="WorkflowState"/>.
/// </summary>
public record RunWorkflowResult<TResult>(WorkflowExecutionContext WorkflowExecutionContext, WorkflowState WorkflowState, Workflow Workflow, TResult Result, Journal Journal);

public record Journal(ICollection<WorkflowExecutionLogEntry> WorkflowExecutionLogEntries, ICollection<ActivityExecutionContext> ActivityExecutionContexts)
{
    public static Journal Empty => new([], []);   
}