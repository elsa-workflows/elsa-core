namespace Elsa.Workflows.Runtime;

/// <summary>
/// Processes pending workflow dispatch outbox items.
/// </summary>
public interface IWorkflowDispatchOutboxProcessor
{
    /// <summary>
    /// Processes pending outbox items.
    /// </summary>
    Task ProcessAsync(CancellationToken cancellationToken = default);
}
