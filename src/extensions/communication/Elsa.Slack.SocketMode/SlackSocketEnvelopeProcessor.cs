using System.Text.Json;
using Elsa.Slack.SocketMode.Events;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode;

internal enum SlackSocketBatchOutcome { Committed, Inactive, CapacityExceeded, Rejected }

/// <summary>Definite per-member results only. These IDs select durable work, never carry execution authority.</summary>
internal sealed class SlackSocketAdmissionBatch(SlackSocketBatchOutcome outcome, IReadOnlyList<string> admissionIds,
    int admitted, int discarded, int duplicates)
{
    internal SlackSocketBatchOutcome Outcome { get; } = outcome;
    internal IReadOnlyList<string> AdmissionIds { get; } = admissionIds;
    internal int Admitted { get; } = admitted;
    internal int Discarded { get; } = discarded;
    internal int Duplicates { get; } = duplicates;
}

/// <summary>The complete captured subscription set is fixed before intake. Partial commits are durable and retried as duplicates.</summary>
internal sealed class SlackSocketEnvelopeProcessor(SlackSocketModeConfiguration configuration, IAdmissionStore store,
    AdmissionExecutionService execution, ISlackSocketDiscardStore discards, TimeProvider timeProvider)
{
    internal async Task DemandCurrentBindingAsync(CancellationToken cancellationToken)
    {
        foreach (var captured in configuration.Subscriptions)
        {
            var subscription = await store.FindSubscriptionAsync(captured.Configuration.Id, cancellationToken);
            if (subscription is not { Active: true, Retired: false, BootstrapVerified: true, ReconciliationCode: null } ||
                subscription.ConfigurationFingerprint != captured.Configuration.ConfigurationFingerprint ||
                subscription.ActivationEpoch != captured.ActivationEpoch ||
                subscription.Configuration.TenantId != configuration.TenantId ||
                subscription.Configuration.EnvironmentId != configuration.EnvironmentId)
            {
                throw new InvalidOperationException("socket_subscription_binding_unavailable");
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal async Task<SlackSocketAdmissionBatch> ProcessAsync(SlackSocketEventPayload payload, CancellationToken cancellationToken)
    {
        if (payload.BindingFingerprint != configuration.BindingFingerprint ||
            !Enum.TryParse<SlackSocketEventKind>(payload.Kind, out var kind) || !Enum.IsDefined(kind))
        {
            throw new InvalidOperationException("socket_event_binding_invalid");
        }
        await DemandCurrentBindingAsync(cancellationToken);
        var normalized = JsonSerializer.Serialize(payload);
        var ids = new List<string>(configuration.Subscriptions.Count);
        var admitted = 0;
        var discarded = 0;
        var duplicates = 0;
        var outcome = SlackSocketBatchOutcome.Committed;
        foreach (var captured in configuration.Subscriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = new AdmissionEvent(captured.Configuration.Id, configuration.InstallationId, configuration.ChannelId,
                payload.ProviderEventId, payload.OccurredAt, kind == SlackSocketEventKind.Human,
                kind is SlackSocketEventKind.Bot or SlackSocketEventKind.Self, normalized);
            if (kind == SlackSocketEventKind.Human)
            {
                // The store compares this captured binding under the SAME lock as duplicate detection/insertion.
                // A preliminary read alone cannot protect against withdrawal and reactivation between these operations.
                message = message with
                {
                    ExpectedConfigurationFingerprint = captured.Configuration.ConfigurationFingerprint,
                    ExpectedActivationEpoch = captured.ActivationEpoch
                };
                var result = await execution.AdmitAsync(message, cancellationToken);
                if (!result.AcknowledgementEligible || result.AdmissionId is null)
                {
                    outcome = result.Outcome switch
                    {
                        AdmissionOutcome.Inactive => SlackSocketBatchOutcome.Inactive,
                        AdmissionOutcome.CapacityExceeded => SlackSocketBatchOutcome.CapacityExceeded,
                        _ => SlackSocketBatchOutcome.Rejected
                    };
                    break;
                }
                ids.Add(result.AdmissionId);
                if (result.Outcome == AdmissionOutcome.Committed)
                {
                    admitted++;
                }
                else
                {
                    duplicates++;
                }
            }
            else
            {
                var reason = kind switch
                {
                    SlackSocketEventKind.Bot => SlackSocketDiscardReason.Bot,
                    SlackSocketEventKind.Self => SlackSocketDiscardReason.Self,
                    SlackSocketEventKind.Edit => SlackSocketDiscardReason.Edit,
                    SlackSocketEventKind.Delete => SlackSocketDiscardReason.Delete,
                    SlackSocketEventKind.UnsupportedMessageSubtype => SlackSocketDiscardReason.UnsupportedMessageSubtype,
                    _ => throw new InvalidOperationException("socket_discard_classification_invalid")
                };
                var result = await discards.RecordDiscardAsync(new(message, reason, configuration.BindingFingerprint,
                    captured.Configuration.ConfigurationFingerprint, captured.ActivationEpoch), timeProvider.GetUtcNow(), cancellationToken);
                if (!result.AcknowledgementEligible)
                {
                    outcome = result.Outcome switch
                    {
                        SlackSocketDiscardOutcome.Inactive => SlackSocketBatchOutcome.Inactive,
                        SlackSocketDiscardOutcome.CapacityExceeded => SlackSocketBatchOutcome.CapacityExceeded,
                        _ => SlackSocketBatchOutcome.Rejected
                    };
                    break;
                }
                if (result.Outcome == SlackSocketDiscardOutcome.Committed)
                {
                    discarded++;
                }
                else
                {
                    duplicates++;
                }
            }
        }
        return new(outcome, ids.AsReadOnly(), admitted, discarded, duplicates);
    }
}
