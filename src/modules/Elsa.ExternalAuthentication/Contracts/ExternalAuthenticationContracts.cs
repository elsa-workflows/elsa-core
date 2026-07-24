using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Contracts;

public interface IExternalAuthenticationAdapter
{
    string Type { get; }
    ExternalAuthenticationAdapterDescriptor Describe();
    ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default);
    ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default);
    ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default);
}

public interface IExternalAuthenticationAdapterRegistry
{
    IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors();
    bool TryGet(string type, out IExternalAuthenticationAdapter adapter);
}

public interface IUnlinkedIdentityPolicyRegistry
{
    IReadOnlyCollection<UnlinkedIdentityPolicyDescriptor> ListDescriptors();
    bool TryGet(string type, out IUnlinkedIdentityPolicy policy);
}

public interface IPermissionGrantSourceRegistry
{
    IReadOnlyCollection<PermissionGrantSourceDescriptor> ListDescriptors();
    bool TryGet(string type, out IPermissionGrantSource source);
}

public interface IAdapterSettingsMigration
{
    string AdapterType { get; }
    int FromVersion { get; }
    int ToVersion { get; }
    ValueTask<JsonElement> MigrateAsync(JsonElement settings, CancellationToken cancellationToken = default);
}

public interface IAdapterSettingsMigrationService
{
    ValueTask<AdapterSettingsMigrationResult> MigrateAsync(
        string adapterType,
        int settingsVersion,
        JsonElement settings,
        CancellationToken cancellationToken = default);
}

public interface IIdentityProviderConnectionSource
{
    string Name { get; }
    ConnectionSourceOwnership Ownership { get; }
    ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default);
}

public interface IIdentityProviderConnectionRegistry
{
    ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default);
    ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default);
    ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default);
}

public interface IIdentityProviderConnectionStore
{
    ValueTask<Page<IdentityProviderConnection>> FindAsync(ConnectionFilter filter, CancellationToken cancellationToken = default);
    ValueTask<IdentityProviderConnection?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    ValueTask<ConnectionMutationResult> CreateAsync(IdentityProviderConnection connection, CancellationToken cancellationToken = default);
    ValueTask<ConnectionMutationResult> UpdateAsync(IdentityProviderConnection connection, long expectedRevision, CancellationToken cancellationToken = default);
}

public interface ISecretBindingResolver
{
    string Type { get; }
    ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default);
    ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default);
}

/// <summary>Creates keyed, non-reversible storage keys for opaque browser handles.</summary>
public interface IExternalAuthenticationHandleHasher
{
    string Hash(string value);
}

public interface IUnlinkedIdentityPolicy
{
    string Type { get; }
    UnlinkedIdentityPolicyDescriptor Describe();
    ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default);
}

public interface IExternalIdentityResolver
{
    ValueTask<ExternalIdentityResolution> ResolveAsync(ExternalIdentityResolutionContext context, CancellationToken cancellationToken = default);
}

public interface IExternalIdentityProvisioner
{
    /// <summary>
    /// Finds the link for a normalized external identity without exposing its persisted subject representation.
    /// </summary>
    ValueTask<ExternalIdentityLink?> FindLinkAsync(string tenantId, string connectionId, ExternalIdentity identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically creates the requested link and, when requested, its credential-less user, or returns the winner of a concurrent operation.
    /// </summary>
    ValueTask<ProvisioningResult> CreateLinkOrGetExistingAsync(ProvisioningRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides tenant-bounded administration of existing external identity links.
/// Creation remains on <see cref="IExternalIdentityProvisioner"/> so administrator prelinks and JIT provisioning use the same atomic tuple operation.
/// </summary>
public interface IExternalIdentityLinkManagementStore
{
    ValueTask<Page<ExternalIdentityLink>> FindAsync(ExternalIdentityLinkFilter filter, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(string tenantId, string linkId, CancellationToken cancellationToken = default);
}

public interface IPermissionGrantSource
{
    string Type { get; }
    PermissionGrantSourceDescriptor Describe();
    ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default);
}

public interface IPermissionDelegationAuthorizer
{
    ValueTask<PermissionDelegationResult> AuthorizeAsync(ClaimsPrincipal actor, IReadOnlyCollection<GrantSourceSelection> selections, CancellationToken cancellationToken = default);
}

public interface IPermissionGrantResolver
{
    ValueTask<PermissionGrantResult> ResolveAsync(PermissionGrantResolutionContext context, CancellationToken cancellationToken = default);
}

public interface IPermissionDescriptorProvider
{
    IEnumerable<PermissionDescriptor> GetDescriptors();
}

public interface IPermissionDescriptorRegistry
{
    IReadOnlyCollection<PermissionDescriptor> List();
}

public interface IExternalAuthenticationStateStore
{
    ValueTask PutAsync<T>(string purpose, string handleHash, T value, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    ValueTask<TakeResult<T>> TryTakeAsync<T>(string purpose, string handleHash, CancellationToken cancellationToken = default);
}

public interface IAuthorizationGrantStore
{
    ValueTask SaveAsync(AuthorizationGrant grant, CancellationToken cancellationToken = default);
    ValueTask<TakeResult<AuthorizationGrant>> TryTakeAsync(string codeHash, CancellationToken cancellationToken = default);
}

public interface IExternalAuthenticationSessionStore
{
    ValueTask<IReadOnlyCollection<ExternalAuthenticationSession>> FindAsync(ExternalAuthenticationSessionFilter filter, CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthenticationSession?> FindByIdAsync(string sessionId, CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthenticationSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default);
    ValueTask<ExternalAuthenticationSessionRotationResult> TryRotateRefreshTokenAsync(string sessionId, string refreshTokenHash, long expectedGeneration, string nextRefreshTokenHash, DateTimeOffset refreshedAt, CancellationToken cancellationToken = default);
    ValueTask<bool> RevokeAsync(string sessionId, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);
}

public interface IPreviewResultStore
{
    ValueTask SaveAsync(PreviewResult result, CancellationToken cancellationToken = default);
    ValueTask<TakeResult<PreviewResult>> TryTakeAsync(string handleHash, string administratorId, CancellationToken cancellationToken = default);
}

public interface IConnectionObservationStore
{
    ValueTask<ConnectionObservation?> FindLatestAsync(string connectionId, CancellationToken cancellationToken = default);
    ValueTask SaveLatestAsync(ConnectionObservation observation, CancellationToken cancellationToken = default);
}

public interface IConnectionRegistryVersionStore
{
    ValueTask<long> GetVersionAsync(CancellationToken cancellationToken = default);
    ValueTask<long> AdvanceAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> IsCurrentAsync(long version, CancellationToken cancellationToken = default);
}

public interface IExternalAuthenticationTokenIssuer
{
    ValueTask<ExternalTokenResponse> IssueAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default);
    ValueTask<ExternalTokenResponse> RefreshAsync(string clientId, SensitiveString refreshToken, CancellationToken cancellationToken = default);
}

public sealed record ResolvedSecretBinding(SensitiveString Value, string GenerationFingerprint);

public sealed record ConnectionSourceSnapshot(ConnectionScope Scope, string Version, IReadOnlyCollection<IdentityProviderConnection> Connections);

public sealed record EffectiveIdentityProviderConnection(
    IdentityProviderConnection Connection,
    ConnectionSourceOwnership Ownership,
    ConnectionScope Scope,
    ConnectionValidity Validity,
    bool IsShadowed,
    string SourceName);

public sealed record EffectiveConnectionRegistry(
    IReadOnlyCollection<EffectiveIdentityProviderConnection> Connections,
    IReadOnlyCollection<LoginMethod> LoginMethods,
    string SourceVersion);

public sealed record ConnectionValidationContext(EffectiveIdentityProviderConnection Connection, IReadOnlyDictionary<string, ResolvedSecretBinding> Secrets, ISystemClock Clock);

public sealed record ExternalAuthorizationContext(EffectiveIdentityProviderConnection Connection, IReadOnlyDictionary<string, ResolvedSecretBinding> Secrets, BrokerTransaction Transaction, string CorrelationState, ISystemClock Clock);

public sealed record ExternalCallbackContext(EffectiveIdentityProviderConnection Connection, IReadOnlyDictionary<string, ResolvedSecretBinding> Secrets, BrokerTransaction Transaction, string CorrelationState, IReadOnlyDictionary<string, IReadOnlyCollection<string>> Parameters, ISystemClock Clock);

public sealed record ConnectionTestContext(EffectiveIdentityProviderConnection Connection, IReadOnlyDictionary<string, ResolvedSecretBinding> Secrets, ISystemClock Clock);

public sealed record ExternalLogoutContext(EffectiveIdentityProviderConnection Connection, IReadOnlyDictionary<string, ResolvedSecretBinding> Secrets, BrokerTransaction Transaction, string CorrelationState, ISystemClock Clock);

public sealed record UnlinkedIdentityContext(string TargetTenantId, EffectiveIdentityProviderConnection Connection, ExternalIdentity Identity, IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims, JsonElement Settings);

public abstract record UnlinkedIdentityDecision
{
    private UnlinkedIdentityDecision() { }
    public sealed record Reject(string SafeReason) : UnlinkedIdentityDecision;
    public sealed record CreateUser(UserCreationProposal Proposal) : UnlinkedIdentityDecision;
    public sealed record LinkExistingUser(string UserId, string AuthorizationBasis) : UnlinkedIdentityDecision;
}

public sealed record ExternalIdentityResolutionContext(string TargetTenantId, EffectiveIdentityProviderConnection Connection, ExternalIdentity Identity, IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims);

public sealed record PermissionGrantContext(string TargetTenantId, string UserId, EffectiveIdentityProviderConnection Connection, ExternalIdentity? Identity, IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims, GrantSourceSelection Selection);
public sealed record PermissionGrantResolutionContext(string TargetTenantId, string UserId, EffectiveIdentityProviderConnection Connection, ExternalIdentity? Identity, IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims);

public abstract record ExternalAuthenticationSessionRotationResult
{
    private ExternalAuthenticationSessionRotationResult() { }
    public sealed record Rotated(ExternalAuthenticationSession Session) : ExternalAuthenticationSessionRotationResult;
    public sealed record NotFound : ExternalAuthenticationSessionRotationResult;
    public sealed record Expired : ExternalAuthenticationSessionRotationResult;
    public sealed record Reused : ExternalAuthenticationSessionRotationResult;
    public sealed record Revoked : ExternalAuthenticationSessionRotationResult;
}
