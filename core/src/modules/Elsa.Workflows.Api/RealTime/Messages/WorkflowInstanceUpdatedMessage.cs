namespace Elsa.Workflows.Api.RealTime.Messages;

/// <summary>
/// A message representing that a workflow instance was updated. 
/// </summary>
/// <param name="WorkflowInstanceId">The ID of the workflow instance that was updated.</param>
public record WorkflowInstanceUpdatedMessage(string WorkflowInstanceId);