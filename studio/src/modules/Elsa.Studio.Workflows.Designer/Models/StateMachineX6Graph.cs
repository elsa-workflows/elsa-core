using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// X6-only projection of a State Machine editor session.
/// </summary>
public sealed class StateMachineX6Graph
{
    public IReadOnlyCollection<StateMachineX6Node> Nodes { get; init; } = [];
    public IReadOnlyCollection<StateMachineX6Edge> Edges { get; init; } = [];
}

public sealed class StateMachineX6Node
{
    public required string Id { get; init; }
    public string Shape { get; init; } = "elsa-state-machine-state";
    public required X6Position Position { get; init; }
    public X6Size Size { get; init; } = new() { Width = 220, Height = 76 };
    public required JsonObject Attrs { get; init; }
    public required JsonObject Data { get; init; }
    public X6Ports Ports { get; init; } = new()
    {
        Items =
        [
            new() { Id = "in", Group = "in" },
            new() { Id = "out", Group = "out" }
        ]
    };
}

public sealed class StateMachineX6Edge
{
    public required string Id { get; init; }
    public string Shape { get; init; } = "elsa-state-machine-transition";
    public required X6Endpoint Source { get; init; }
    public required X6Endpoint Target { get; init; }
    public required JsonObject Data { get; init; }
    public JsonObject Attrs { get; init; } = [];
    public IReadOnlyCollection<StateMachineX6Label> Labels { get; init; } = [];
    public IReadOnlyCollection<X6Position> Vertices { get; init; } = [];
}

public sealed class StateMachineX6Label
{
    public required JsonObject Attrs { get; init; }
    public double Position { get; init; } = .5;
}
