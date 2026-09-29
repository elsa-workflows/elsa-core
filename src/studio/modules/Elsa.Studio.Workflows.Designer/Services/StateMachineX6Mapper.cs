using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// Maps the renderer-neutral State Machine projection to native X6 cells.
/// </summary>
public static class StateMachineX6Mapper
{
    public static StateMachineX6Graph Map(StateMachineCanvasGraph graph)
    {
        var nodes = graph.States.Select(MapState).ToList();
        var edges = new List<StateMachineX6Edge>();

        foreach (var transition in graph.Transitions)
        {
            var sourceId = transition.SourceStateVisualId ?? $"missing-{transition.VisualId}-source";
            var targetId = transition.TargetStateVisualId ?? $"missing-{transition.VisualId}-target";
            if (transition.SourceStateVisualId == null)
                nodes.Add(MapMissingState(sourceId, transition.VisualId, transition.From, transition.SemanticIndex, false));
            if (transition.TargetStateVisualId == null)
                nodes.Add(MapMissingState(targetId, transition.VisualId, transition.To, transition.SemanticIndex, true));
            edges.Add(MapTransition(transition, sourceId, targetId));
        }

        return new() { Nodes = nodes, Edges = edges };
    }

    private static StateMachineX6Node MapState(StateMachineCanvasState state)
    {
        var statuses = new List<string>();
        if (state.IsInitial) statuses.Add("Initial");
        if (state.IsCurrent) statuses.Add("Current");
        if (state.IsTerminal) statuses.Add("Terminal");
        if (state.HasEntry) statuses.Add("Entry");
        if (state.HasExit) statuses.Add("Exit");
        if (state.ValidationIssueCount > 0) statuses.Add(FormatIssueCount(state.ValidationIssueCount));

        return new()
        {
            Id = state.VisualId,
            Position = new() { X = state.Position.X, Y = state.Position.Y },
            Attrs = new JsonObject
            {
                ["body"] = state.HasValidationErrors
                    ? new JsonObject
                    {
                        ["stroke"] = "var(--mud-palette-error)",
                        ["strokeWidth"] = 2,
                        ["strokeDasharray"] = "5 3"
                    }
                    : new JsonObject(),
                ["title"] = new JsonObject { ["text"] = Display(state.Name) },
                ["meta"] = new JsonObject { ["text"] = statuses.Count == 0 ? "State" : string.Join(" · ", statuses) },
                ["status"] = new JsonObject
                {
                    ["fill"] = state.IsCurrent
                        ? "var(--elsa-designer-node-current)"
                        : state.IsInitial
                            ? "var(--elsa-designer-node-initial)"
                            : "var(--elsa-designer-node-accent)"
                }
            },
            Data = new JsonObject
            {
                ["kind"] = "state",
                ["visualId"] = state.VisualId,
                ["name"] = state.Name,
                ["accessibleName"] = BuildAccessibleName(state, statuses)
            }
        };
    }

    private static StateMachineX6Node MapMissingState(string visualId, string transitionVisualId, string? name, int transitionIndex, bool target) => new()
    {
        Id = visualId,
        Position = new()
        {
            X = target ? 560 : -280,
            Y = 140 + transitionIndex * 110
        },
        Attrs = new JsonObject
        {
            ["body"] = new JsonObject { ["stroke"] = "var(--mud-palette-error)", ["strokeDasharray"] = "5 4" },
            ["status"] = new JsonObject { ["fill"] = "var(--mud-palette-error)" },
            ["title"] = new JsonObject { ["text"] = Display(name) },
            ["meta"] = new JsonObject { ["text"] = "Unresolved endpoint" }
        },
        Data = new JsonObject
        {
            ["kind"] = "missing-state",
            ["synthetic"] = true,
            ["transitionVisualId"] = transitionVisualId,
            ["accessibleName"] = $"Unresolved state endpoint {Display(name)}, select transition to repair"
        }
    };

    private static StateMachineX6Edge MapTransition(StateMachineCanvasTransition transition, string sourceId, string targetId)
    {
        var label = !string.IsNullOrWhiteSpace(transition.DisplayName)
            ? transition.DisplayName
            : transition.Name;
        if (transition.ValidationIssueCount > 0)
            label = $"⚠ {label ?? "Transition"}";

        return new()
        {
            Id = transition.VisualId,
            Source = new() { Cell = sourceId, Port = "out" },
            Target = new() { Cell = targetId, Port = "in" },
            Data = new JsonObject
            {
                ["kind"] = "transition",
                ["visualId"] = transition.VisualId,
                ["name"] = transition.Name,
                ["from"] = transition.From,
                ["to"] = transition.To,
                ["accessibleName"] = BuildTransitionAccessibleName(transition)
            },
            Attrs = transition.HasValidationErrors
                ? new JsonObject
                {
                    ["line"] = new JsonObject
                    {
                        ["stroke"] = "var(--mud-palette-error)",
                        ["strokeWidth"] = 2,
                        ["strokeDasharray"] = "6 4"
                    }
                }
                : new JsonObject(),
            Vertices = transition.Vertices.Select(x => new X6Position { X = x.X, Y = x.Y }).ToList(),
            Labels = string.IsNullOrWhiteSpace(label)
                ? []
                :
                [
                    new()
                    {
                        Attrs = new JsonObject
                        {
                            ["label"] = new JsonObject
                            {
                                ["text"] = label,
                                ["fill"] = "var(--elsa-designer-edge-label-text)"
                            },
                            ["body"] = new JsonObject
                            {
                                ["fill"] = "var(--elsa-designer-edge-label-surface)",
                                ["stroke"] = "var(--elsa-designer-edge-label-border)",
                                ["strokeWidth"] = 1,
                                ["rx"] = 4,
                                ["ry"] = 4
                            }
                        }
                    }
                ]
        };
    }

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "Unnamed state" : value;
    private static string DisplayTransition(string? value) => string.IsNullOrWhiteSpace(value) ? "Unnamed transition" : value;

    private static string BuildAccessibleName(StateMachineCanvasState state, IReadOnlyCollection<string> statuses) =>
        statuses.Count == 0
            ? $"{Display(state.Name)}, state"
            : $"{Display(state.Name)}, {string.Join(", ", statuses)} state";

    private static string BuildTransitionAccessibleName(StateMachineCanvasTransition transition) =>
        transition.ValidationIssueCount == 0
            ? $"{DisplayTransition(transition.Name)}, transition from {Display(transition.From)} to {Display(transition.To)}"
            : $"{DisplayTransition(transition.Name)}, transition from {Display(transition.From)} to {Display(transition.To)}, {FormatIssueCount(transition.ValidationIssueCount)}";

    private static string FormatIssueCount(int count) => count == 1 ? "1 validation issue" : $"{count} validation issues";
}
