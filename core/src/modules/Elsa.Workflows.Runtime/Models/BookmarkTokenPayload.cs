using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents the payload for a bookmark token, including the bookmark identifier and the associated workflow instance identifier.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record BookmarkTokenPayload([property: JsonRequired] string BookmarkId, [property: JsonRequired] string WorkflowInstanceId);