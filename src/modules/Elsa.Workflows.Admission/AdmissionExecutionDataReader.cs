using System.Text.Json;

namespace Elsa.Workflows.Admission;

internal sealed class AdmissionExecutionDataReader(IAdmissionStore store, AdmissionAuthorityRegistry authorities) : IAdmissionExecutionDataReader
{
    public async ValueTask<AdmissionExecutionData> ReadConsumedEventAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var invocation = authorities.DemandConsumedInvocation(context);
        var binding = invocation.EventBinding;
        AdmissionRecord? record;
        try
        {
            record = await store.FindAsync(binding.AdmissionId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("The admitted event data is unavailable.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        // The asynchronous lookup may outlive its original owner. Require the very same private
        // consumed binding again; persisted StartAuthorized is never sufficient for this read.
        if (!ReferenceEquals(authorities.DemandConsumedInvocation(context), invocation) || record == null ||
            record.State != AdmissionState.StartAuthorized || !record.AuthorityOutstanding ||
            record.Id != binding.AdmissionId || record.WorkflowInstanceId != binding.InstanceId ||
            record.Revision != binding.Revision || record.AttemptId != binding.AttemptId ||
            record.SubscriptionId != binding.SubscriptionId || record.ConfigurationFingerprint != binding.ConfigurationFingerprint ||
            record.AdmittedConfigurationJson != binding.ConfigurationJson || record.ActivationEpoch != binding.ActivationEpoch ||
            record.ProviderEventId != binding.ProviderEventId || record.EventOccurredAt != binding.OccurredAt ||
            record.Payload == null || record.PayloadFingerprint != binding.PayloadFingerprint ||
            record.EventFingerprint != binding.EventFingerprint || AdmissionHash.Compute(record.Payload) != binding.PayloadFingerprint)
        {
            throw new InvalidOperationException("The admitted event data does not match its live owner.");
        }
        var configuration = JsonSerializer.Deserialize<AdmissionSubscriptionConfiguration>(binding.ConfigurationJson)
            ?? throw new InvalidOperationException("The admitted event binding is unavailable.");
        configuration.Validate();
        if (configuration.ConfigurationFingerprint != binding.ConfigurationFingerprint || configuration.Id != binding.SubscriptionId ||
            configuration.DefinitionId != binding.DefinitionId || configuration.DefinitionVersionId != binding.DefinitionVersionId ||
            configuration.DefinitionVersion != binding.DefinitionVersion)
        {
            throw new InvalidOperationException("The admitted event binding changed.");
        }
        return new(record.Id, binding.InstanceId, configuration, binding.ActivationEpoch,
            binding.ProviderEventId, binding.OccurredAt, record.Payload, binding.PayloadFingerprint, binding.EventFingerprint);
    }
}
