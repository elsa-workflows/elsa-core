namespace Elsa.Workflows.Runtime;

/// <summary>
/// Processes pending workflow dispatch outbox items.
/// </summary>
public interface IWorkflowDispatchOutboxProcessor
{
    /// <summary>
    /// Processes pending outbox items.
    /// </summary>
    /// <remarks>Implementations must lock per tenant themselves; the recurring task takes no lock.</remarks>
    Task ProcessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to process pending outbox items without waiting for the processor lock.
    /// </summary>
    Task<bool> TryProcessAsync(CancellationToken cancellationToken = default);
}
