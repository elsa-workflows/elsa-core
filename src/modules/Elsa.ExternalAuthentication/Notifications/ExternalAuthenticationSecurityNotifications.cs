using Elsa.Mediator.Contracts;

namespace Elsa.ExternalAuthentication.Notifications;

public enum SecurityEventOutcome
{
    Succeeded,
    Failed,
    Rejected
}

/// <summary>
/// Safe context shared by External Authentication security notifications.
/// It intentionally contains no secret, token, raw subject, or unrestricted claim value.
/// </summary>
public sealed record SecurityEventContext(
    string? ActorId,
    string? TenantId,
    string? ConnectionId,
    string? UserId,
    DateTimeOffset OccurredAt,
    SecurityEventOutcome Outcome,
    string CorrelationId,
    string Summary);

public sealed record IdentityProviderConnectionChanged(SecurityEventContext Context, string Operation, long Revision, string MaterialRevision) : INotification;
public sealed record IdentityProviderConnectionLifecycleChanged(SecurityEventContext Context, string PreviousLifecycle, string CurrentLifecycle, long Revision) : INotification;
public sealed record IdentityProviderConnectionSecretBindingChanged(SecurityEventContext Context, string FieldName, string ResolverType, bool IsConfigured) : INotification;
public sealed record IdentityProviderConnectionTested(SecurityEventContext Context, string TestedMaterialRevision, string Status, string Category, TimeSpan Duration) : INotification;
public sealed record IdentityProviderConnectionPreviewed(SecurityEventContext Context, string MaterialRevision) : INotification;
public sealed record ExternalIdentityLinkChanged(SecurityEventContext Context, string Operation, string LinkId) : INotification;
public sealed record ExternalAuthenticationSessionRevoked(SecurityEventContext Context, string SessionId, string Reason) : INotification;
public sealed record ExternalSignInCompleted(SecurityEventContext Context, string? SessionId, string? AdapterType) : INotification;
/// <summary>Safe terminal broker outcome. Flow and stage are fixed internal vocabulary; no provider detail is included.</summary>
public sealed record ExternalAuthenticationOutcomeRecorded(SecurityEventContext Context, string Flow, string Stage, string Category) : INotification;
