namespace Elsa.Workflows.Core.Signals;

/// <summary>
/// Sent by child activities to notify their composite container that it should complete.
/// </summary>
public record CompleteCompositeSignal(object? Value = default);