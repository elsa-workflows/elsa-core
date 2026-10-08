using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode.Persistence;

internal enum SlackSocketDiscardReason { Bot, Self, Edit, Delete, UnsupportedMessageSubtype }
internal enum SlackSocketDiscardOutcome { Committed, Duplicate, Inactive, CapacityExceeded, Rejected, Quarantined }

internal sealed record SlackSocketDiscardRequest(AdmissionEvent Event, SlackSocketDiscardReason Reason,
    string BindingFingerprint, string ExpectedConfigurationFingerprint, long ExpectedActivationEpoch);

internal sealed record SlackSocketDiscardResult(SlackSocketDiscardOutcome Outcome, string? ReceiptId, long? Revision)
{
    public bool AcknowledgementEligible => Outcome is SlackSocketDiscardOutcome.Committed or SlackSocketDiscardOutcome.Duplicate;
}

internal sealed record SlackSocketDiscardCleanupCandidate(string Id, long Revision);

/// <summary>Final classifications only. No payload text, workflow or execution authority is retained.</summary>
internal sealed class SlackSocketDiscardReceipt
{
    public string Id { get; set; } = null!;
    public string SubscriptionId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string EnvironmentId { get; set; } = null!;
    public string IdentityHash { get; set; } = null!;
    public string ProviderEventId { get; set; } = null!;
    public SlackSocketDiscardReason Reason { get; set; }
    public string BindingFingerprint { get; set; } = null!;
    public string ConfigurationFingerprint { get; set; } = null!;
    public string ConfigurationJson { get; set; } = null!;
    public long ActivationEpoch { get; set; }
    public string PayloadFingerprint { get; set; } = null!;
    public string EventFingerprint { get; set; } = null!;
    public DateTimeOffset EventOccurredAt { get; set; }
    public DateTimeOffset DecisionAt { get; set; }
    public long Revision { get; set; }
}

// A key-only reference to an existing table; the receipt migration never provisions it.
internal sealed class SlackSocketReceiptSubscriptionKey
{
    public string Id { get; set; } = null!;
}
