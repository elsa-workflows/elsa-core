namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Controls how the durable stimulus queue behaves when its configured maximum depth is exceeded while the runtime is paused.
/// </summary>
public enum StimulusQueueOverflowPolicy
{
    /// <summary>
    /// Continue accepting new stimuli. A degraded readiness signal is surfaced but no writes are rejected. This is the default
    /// so upstream transports are not forced into redelivery loops during routine administrative pauses.
    /// </summary>
    Buffer,

    /// <summary>
    /// Reject new stimuli with a typed error once the threshold is exceeded. Upstream transports can translate the error into
    /// their own back-pressure primitive.
    /// </summary>
    Reject,
}
