namespace Elsa.Connections.Models;

/// <summary>Durable metadata for one local disconnect or provider offboarding action. It never contains credential values.</summary>
public sealed class ConnectionOffboardingOperation
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string EnvironmentId { get; set; } = default!;
    public string ConnectionId { get; set; } = default!;
    public string ProviderId { get; set; } = default!;
    public string ProviderAccountId { get; set; } = default!;
    public ConnectionOffboardingOperationKind Kind { get; set; }
    public string? GenerationId { get; set; }
    public ConnectionOffboardingOperationStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public long Fence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public string? LastSafeErrorCode { get; set; }

    public override string ToString() => $"ConnectionOffboardingOperation {{ Id = {Id}, Kind = {Kind}, Status = {Status}, Redacted = true }}";
}

public enum ConnectionOffboardingOperationKind
{
    LocalDisconnect,
    TokenPairRevocation,
    InstallationUninstall
}

public enum ConnectionOffboardingOperationStatus
{
    Pending,
    Claimed,
    ProviderCallStarted,
    RetryScheduled,
    UnknownOutcome,
    Completed,
    TerminalFailure
}
