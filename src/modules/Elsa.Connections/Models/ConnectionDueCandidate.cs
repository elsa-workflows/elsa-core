namespace Elsa.Connections.Models;

/// <summary>Nonsecret pointer to lifecycle work that may be due. The reconciler must re-read and claim through existing fenced APIs.</summary>
public sealed record ConnectionDueCandidate(
    string TenantId,
    string EnvironmentId,
    string ConnectionId,
    ConnectionDueCandidateKind Kind,
    string CandidateId,
    DateTimeOffset DueAt);

public enum ConnectionDueCandidateKind
{
    OAuthRefresh,
    ExpiredConnectionOperation,
    RecoveryRequired,
    GenerationCleanup,
    Offboarding
}
