namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a path segment in a workflow.
/// </summary>
/// <param name="ActivityNodeId">The node ID of the activity in the graph.</param>
/// <param name="ActivityId">The ID of the activity.</param>
/// <param name="ActivityType">The type of the activity.</param>
/// <param name="PortName">The name of the port.</param>
public record ActivityPathSegment(string ActivityNodeId, string ActivityId, string ActivityType, string PortName);