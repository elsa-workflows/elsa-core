namespace Elsa.Http.Models;

/// <summary>
/// Represents the payload of an event, serialized as a secured token.
/// </summary>
/// <param name="EventName">The name of the event.</param>
/// <param name="WorkflowInstanceId">The ID of the workflow instance to trigger with the event.</param>
public record EventTokenPayload(string EventName, string WorkflowInstanceId);