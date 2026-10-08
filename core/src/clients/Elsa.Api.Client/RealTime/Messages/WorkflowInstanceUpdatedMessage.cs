namespace Elsa.Api.Client.RealTime.Messages;

/// <summary>
/// Represents a message that is sent when the workflow instance is updated.
/// </summary>
/// <param name="WorkflowInstanceId">The ID of the workflow instance that was updated.</param>
public record WorkflowInstanceUpdatedMessage(string WorkflowInstanceId);