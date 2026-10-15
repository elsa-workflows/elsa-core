using Elsa.Connections.Models;

namespace Elsa.Connections.Contracts;

/// <summary>Host-owned remote offboarding actions with explicit outcome and retry guarantees.</summary>
/// <remarks>
/// Implementations must report <see cref="SupportsStableOperationIdIdempotency"/> as true for an operation kind only when the remote system
/// guarantees at-most-once effects for repeated calls with the same operation ID. Without that guarantee, unknown outcomes
/// remain durable and are not automatically replayed. A retryable failure guarantees the provider knows no remote effect occurred.
/// </remarks>
public interface IConnectionOffboardingProvider
{
    /// <summary>Whether repeated calls with the same operation ID are guaranteed to produce at most one remote effect.</summary>
    bool SupportsStableOperationIdIdempotency(ConnectionOffboardingOperationKind kind) => false;

    Task<ConnectionOffboardingProviderResult> RevokeTokenPairAsync(
        string providerId,
        string providerAccountId,
        string operationId,
        CredentialMaterial credentials,
        CancellationToken cancellationToken = default);

    /// <summary>Uses provider-owned host credentials; this operation does not receive or resolve a Connections generation.</summary>
    Task<ConnectionOffboardingProviderResult> UninstallInstallationAsync(
        string providerId,
        string providerAccountId,
        string operationId,
        CancellationToken cancellationToken = default);
}

public enum ConnectionOffboardingProviderResult
{
    Succeeded,
    /// <summary>The provider confirms that no remote effect occurred, so a later retry is safe without idempotency support.</summary>
    RetryableFailure,
    TerminalFailure,
    UnknownOutcome
}
