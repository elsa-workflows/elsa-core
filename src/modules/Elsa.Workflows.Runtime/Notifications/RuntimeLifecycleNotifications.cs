using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Published when an authorised operator effectively transitions the runtime into <see cref="QuiescenceReason.AdministrativePause"/>.
/// Idempotent re-invocations do NOT publish this notification (SC-007).
/// </summary>
public record RuntimePauseRequested(string? RequestedBy, string? Reason, DateTimeOffset Timestamp) : INotification;

/// <summary>
/// Published when an authorised operator effectively clears <see cref="QuiescenceReason.AdministrativePause"/>.
/// Idempotent re-invocations do NOT publish this notification (SC-007).
/// </summary>
public record RuntimeResumeRequested(string? RequestedBy, DateTimeOffset Timestamp) : INotification;

/// <summary>
/// Published when an authorised operator invokes the force-drain endpoint and a force operation actually runs
/// (a force call against an already-completed drain returns the cached outcome and does NOT publish this notification).
/// </summary>
public record RuntimeForceDrainRequested(string? RequestedBy, string? Reason, DateTimeOffset Timestamp, DrainOutcome Outcome) : INotification;
