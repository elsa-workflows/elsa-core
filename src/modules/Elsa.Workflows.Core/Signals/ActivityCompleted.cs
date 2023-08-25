namespace Elsa.Workflows.Core.Signals;

/// <summary>
/// Signaled when an activity has completed.
/// </summary>
/// <param name="Result">An optional result.</param>
public record ActivityCompleted(object? Result = default);