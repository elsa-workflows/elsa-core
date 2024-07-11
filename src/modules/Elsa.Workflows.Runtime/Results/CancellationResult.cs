namespace Elsa.Workflows.Runtime.Results;

/// <summary>
/// Represents the result of a cancellation operation.
/// </summary>
/// <param name="Success">True if the operation was successful; otherwise, false.</param>
/// <param name="Reason">The reason for the failure, if any.</param>
public record CancellationResult(bool Success, CancellationFailureReason? Reason = default);