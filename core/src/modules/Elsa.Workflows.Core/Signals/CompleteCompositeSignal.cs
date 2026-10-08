namespace Elsa.Workflows.Signals;

/// <summary>
/// Sent by child activities to notify their composite container that it should complete.
/// </summary>
public record CompleteCompositeSignal(object? Value = default);