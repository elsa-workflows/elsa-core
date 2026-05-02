using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Typed payload recorded in the per-instance workflow execution log whenever an execution cycle is force-cancelled
/// by the runtime. Stored under <see cref="WorkflowInterruptedEventName"/>.
/// </summary>
/// <remarks>
/// The <see cref="ExecutionCycleDuration"/> property is serialized under the JSON key <c>"BurstDuration"</c> for
/// backwards compatibility — that key was already present in records persisted by pre-merge testers of this PR
/// before the execution cycle → execution-cycle rename, and we preserve it so existing rows continue to deserialize. The C#
/// property name uses the new vocabulary; the wire format keeps the old one.
/// </remarks>
public sealed record WorkflowInterruptedPayload(
    DateTimeOffset InterruptedAt,
    string Reason,
    string GenerationId,
    string? LastActivityId,
    string? LastActivityNodeId,
    string? IngressSourceName,
    [property: JsonPropertyName("BurstDuration")] TimeSpan ExecutionCycleDuration)
{
    /// <summary>Stable event name used on <c>WorkflowExecutionLogRecord.EventName</c>.</summary>
    public const string WorkflowInterruptedEventName = "WorkflowInterrupted";

    /// <summary>Reason discriminator: a drain deadline elapsed while execution cycles were still running.</summary>
    public const string ReasonDeadlineBreach = "DeadlineBreach";

    /// <summary>Reason discriminator: an operator invoked the admin force-drain endpoint.</summary>
    public const string ReasonOperatorForce = "OperatorForce";

    /// <summary>Reason discriminator: the persistence layer was unavailable when the drain tried to commit the cycle's terminal state.</summary>
    public const string ReasonPersistenceFailure = "PersistenceFailure";
}
