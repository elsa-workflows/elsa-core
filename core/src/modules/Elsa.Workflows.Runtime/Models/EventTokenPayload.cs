using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents the payload of an event, serialized as a secured token.
/// </summary>
/// <param name="EventName">The name of the event.</param>
/// <param name="WorkflowInstanceId">The ID of the workflow instance to trigger with the event.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record EventTokenPayload([property: JsonRequired] string EventName, [property: JsonRequired] string WorkflowInstanceId);