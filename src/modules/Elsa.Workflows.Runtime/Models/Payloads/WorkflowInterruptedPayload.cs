namespace Elsa.Workflows.Runtime;

/// <summary>
/// Typed payload recorded in the per-instance workflow execution log whenever a burst is force-cancelled by the runtime.
/// Stored under <see cref="WorkflowInterruptedEventName"/>.
/// </summary>
public sealed record WorkflowInterruptedPayload(
    DateTimeOffset InterruptedAt,
    string Reason,
    string GenerationId,
    string? LastActivityId,
    string? LastActivityNodeId,
    string? IngressSourceName,
    TimeSpan BurstDuration)
{
    /// <summary>Stable event name used on <c>WorkflowExecutionLogRecord.EventName</c>.</summary>
    public const string WorkflowInterruptedEventName = "WorkflowInterrupted";

    /// <summary>Reason discriminator: a drain deadline elapsed while bursts were still running.</summary>
    public const string ReasonDeadlineBreach = "DeadlineBreach";

    /// <summary>Reason discriminator: an operator invoked the admin force endpoint.</summary>
    public const string ReasonOperatorForce = "OperatorForce";

    /// <summary>Reason discriminator: the persistence layer was unavailable when the drain tried to commit the burst's terminal state.</summary>
    public const string ReasonPersistenceFailure = "PersistenceFailure";
}
