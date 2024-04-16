namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a reason for a cancellation failure.
/// </summary>
public enum CancellationFailureReason
{
    /// <summary>
    /// The workflow instance was not found.
    /// </summary>
    NotFound,
    /// <summary>
    /// The workflow instance is already being canceled remotely.
    /// </summary>
    CancellationAlreadyInProgress,
    
    /// <summary>
    /// The workflow instance is already canceled.
    /// </summary>
    AlreadyFinished,
}