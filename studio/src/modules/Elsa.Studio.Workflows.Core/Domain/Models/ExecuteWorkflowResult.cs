namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents the result of a workflow execution request.
/// </summary>
/// <param name="WorkflowInstanceId">The identifier of the workflow instance that was started, if any.</param>
/// <param name="CannotStart">Indicates whether the workflow could not be started.</param>
public record ExecuteWorkflowResult(string? WorkflowInstanceId, bool CannotStart);