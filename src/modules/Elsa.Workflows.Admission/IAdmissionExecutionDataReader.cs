namespace Elsa.Workflows.Admission;

/// <summary>
/// Reads event data for an exact live context whose private admission authority was already consumed.
/// This contract cannot create, consume, return or invoke execution authority. A durable row alone,
/// copied context, prepared-but-unconsumed context or retired invocation cannot authorize a read.
/// </summary>
public interface IAdmissionExecutionDataReader
{
    ValueTask<AdmissionExecutionData> ReadConsumedEventAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>Immutable event data and its admitted binding, with no credential or invocable capability.</summary>
public sealed record AdmissionExecutionData(string AdmissionId, string WorkflowInstanceId,
    AdmissionSubscriptionConfiguration Configuration, long ActivationEpoch, string ProviderEventId,
    DateTimeOffset OccurredAt, string Payload, string PayloadFingerprint, string EventFingerprint)
{
    public override string ToString() => "AdmissionExecutionData { Redacted = true }";
}
