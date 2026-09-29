namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

/// <summary>
/// Describes an edited State Machine activity slot without changing the graph model in the presentation component.
/// </summary>
public sealed record StateMachineSlotValueChange(string SlotName, string? Value);
