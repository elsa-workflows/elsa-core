namespace Elsa.Workflows.Api.Models;

/// <summary>
/// Represents a path segment in a workflow. For example, the following is a list of segments: ForEach:Body -> Container:Body -> MyCustomActivity:Root
/// </summary>
/// <param name="ActivityNodeId">The node ID of the activity in the graph.</param>
/// <param name="ActivityId">The ID of the activity.</param>
/// <param name="ActivityType">The type of the activity.</param>
/// <param name="PortName">The name of the port.</param>
/// <param name="ActivityName">The name of the activity.</param>
public record ActivityPathSegment(string ActivityNodeId, string ActivityId, string ActivityType, string PortName, string ActivityName);