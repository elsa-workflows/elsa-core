namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// A renderer-neutral projection of a StateMachine editor session.
/// </summary>
public sealed class StateMachineCanvasGraph
{
    /// <summary>
    /// Gets or sets the initial state name.
    /// </summary>
    public string? InitialState { get; init; }

    /// <summary>
    /// Gets or sets the current state name.
    /// </summary>
    public string? CurrentState { get; init; }

    /// <summary>
    /// Gets the state nodes in their semantic source order.
    /// </summary>
    public IReadOnlyList<StateMachineCanvasState> States { get; init; } = [];

    /// <summary>
    /// Gets the transition edges in their semantic source order.
    /// </summary>
    public IReadOnlyList<StateMachineCanvasTransition> Transitions { get; init; } = [];
}

/// <summary>
/// A state projected for a visual canvas.
/// </summary>
public sealed class StateMachineCanvasState
{
    public required string VisualId { get; init; }
    public required string Name { get; init; }
    public required int SemanticIndex { get; init; }
    public required StateMachineCanvasPosition Position { get; init; }
    public bool IsInitial { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsTerminal { get; init; }
    public bool HasEntry { get; init; }
    public bool HasExit { get; init; }
    public int ValidationIssueCount { get; init; }
    public bool HasValidationErrors { get; init; }
}

/// <summary>
/// A transition projected for a visual canvas.
/// </summary>
public sealed class StateMachineCanvasTransition
{
    public required string VisualId { get; init; }
    public required int SemanticIndex { get; init; }
    public string? Name { get; init; }
    public string? DisplayName { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public string? SourceStateVisualId { get; init; }
    public string? TargetStateVisualId { get; init; }
    public bool HasTrigger { get; init; }
    public bool HasCondition { get; init; }
    public bool HasAction { get; init; }
    public int ValidationIssueCount { get; init; }
    public bool HasValidationErrors { get; init; }
    public IReadOnlyList<StateMachineCanvasPosition> Vertices { get; init; } = [];
}

/// <summary>
/// A session-only visual position. It is deliberately separate from runtime StateMachine JSON.
/// </summary>
public sealed record StateMachineCanvasPosition(double X, double Y);

/// <summary>
/// An activity-bearing state slot.
/// </summary>
public enum StateMachineStateSlot
{
    Entry,
    Exit
}

/// <summary>
/// A transition slot.
/// </summary>
public enum StateMachineTransitionSlot
{
    Trigger,
    Condition,
    Action
}
