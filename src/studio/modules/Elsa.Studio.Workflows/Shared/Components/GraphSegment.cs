using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Shared.Components;

/// <summary>
/// Represents the graph segment record.
/// </summary>
public record GraphSegment(JsonObject Activity, string PortName, JsonObject? EmbeddedActivity = default);