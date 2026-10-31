namespace Elsa.Workflows.Runtime;

/// <summary>
/// Thrown by an <c>IBookmarkQueue</c> decorator when the queue depth exceeds the configured threshold while the
/// runtime is paused AND <c>GracefulShutdownOptions.OverflowPolicy</c> is set to <c>Reject</c>. Upstream transports
/// should translate this into their own back-pressure primitive (e.g., NACK + retry-after on a message broker).
/// See FR-026 and research R6.
/// </summary>
public sealed class StimulusQueueOverflowException : Exception
{
    public StimulusQueueOverflowException(string message) : base(message)
    {
    }

    public StimulusQueueOverflowException(string message, Exception inner) : base(message, inner)
    {
    }
}
