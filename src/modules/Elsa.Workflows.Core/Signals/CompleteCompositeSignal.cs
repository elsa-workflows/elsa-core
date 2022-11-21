namespace Elsa.Workflows.Core.Signals;

/// <summary>
/// Sent by child activities to notify their composite root that it should complete.
/// </summary>
public record CompleteCompositeSignal(object? Result = default);