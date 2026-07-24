using System.Text.Json;

namespace Elsa.ExternalAuthentication.Models;

public enum ConnectionSourceOwnership
{
    Configuration,
    Database
}

public enum ConnectionScopeKind
{
    Host,
    DefaultTenant,
    Tenant
}

public enum ConnectionLifecycle
{
    Draft,
    Disabled,
    Enabled,
    Archived
}

public enum ConnectionValidity
{
    Unknown,
    Valid,
    Invalid
}

public enum AuthenticationClientType
{
    Confidential,
    Public
}

public enum BrokerTransactionPurpose
{
    ExternalSignIn,
    LocalSignIn,
    Preview,
    UpstreamLogout
}

public enum UpstreamLogoutMode
{
    Disabled,
    UserChoice,
    Always
}

public enum ConnectionObservationStatus
{
    Succeeded,
    Failed,
    Warning
}

public enum LoginMethodKind
{
    Local,
    External
}

public enum BrowserCredentialPersistence
{
    Memory,
    SessionStorage,
    DurableStorage
}

public sealed record ConnectionScope(ConnectionScopeKind Kind, string TenantId)
{
    public const string HostTenantId = "*";
    public const string DefaultTenantId = "";

    public static ConnectionScope Host { get; } = new(ConnectionScopeKind.Host, HostTenantId);
    public static ConnectionScope DefaultTenant { get; } = new(ConnectionScopeKind.DefaultTenant, DefaultTenantId);
}

public sealed record SecretBinding(string ResolverType, string Reference, string? ExpectedType = null, string? ExpectedScope = null);

public sealed record SecretBindingState(bool IsConfigured, bool IsResolvable);

public sealed record PolicySelection(string Type, int SettingsVersion, JsonElement Settings);

public sealed record GrantSourceSelection(string Type, int SettingsVersion, JsonElement Settings, int Order);

public sealed record ClaimProjection(
    IReadOnlySet<string> AllowedClaimTypes,
    IReadOnlySet<string> RedactedClaimTypes,
    int MaximumClaimCount,
    int MaximumValueLength,
    int MaximumTotalBytes)
{
    public static ClaimProjection Empty { get; } = new(new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal), 0, 0, 0);
}

public sealed class IdentityProviderConnection
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string AdapterType { get; set; } = null!;
    public int AdapterSettingsVersion { get; set; }
    public JsonElement AdapterSettings { get; set; }
    public IDictionary<string, SecretBinding> SecretBindings { get; set; } = new Dictionary<string, SecretBinding>(StringComparer.Ordinal);
    public string DisplayName { get; set; } = null!;
    public string? IconId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public PolicySelection? UnlinkedPolicy { get; set; }
    public ICollection<GrantSourceSelection> PermissionGrantSources { get; set; } = new List<GrantSourceSelection>();
    public ClaimProjection ClaimProjection { get; set; } = ClaimProjection.Empty;
    public UpstreamLogoutMode UpstreamLogoutMode { get; set; }
    public long Revision { get; set; }
    public string MaterialRevision { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed record ExternalIdentity(string Issuer, string Subject, IReadOnlyDictionary<string, IReadOnlyCollection<string>> Claims);

public sealed record AuthenticationClient(
    string ClientId,
    string DisplayName,
    AuthenticationClientType ClientType,
    IReadOnlySet<Uri> CallbackUris,
    IReadOnlySet<Uri> LogoutCallbackUris,
    IReadOnlySet<string> AllowedOrigins,
    IReadOnlySet<string> AllowedReturnPathPrefixes,
    SecretBinding? SecretBinding,
    bool IsEnabled);

public sealed class BrokerTransaction
{
    public string HandleHash { get; set; } = null!;
    public BrokerTransactionPurpose Purpose { get; set; }
    public string ClientId { get; set; } = null!;
    public Uri CallbackUri { get; set; } = null!;
    public string ReturnPath { get; set; } = null!;
    /// <summary>
    /// Caller-owned opaque state returned only to the registered callback URI.
    /// </summary>
    public string? ClientState { get; set; }
    public string TenantId { get; set; } = null!;
    public string? ConnectionId { get; set; }
    public string? ConnectionMaterialRevision { get; set; }
    public string? SecretGenerationFingerprint { get; set; }
    public string PkceChallenge { get; set; } = null!;
    public string? ProviderNonce { get; set; }
    public byte[] ProtectedPayload { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

public sealed class AuthorizationGrant
{
    public string CodeHash { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public Uri CallbackUri { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? ExternalSessionId { get; set; }
    public string PkceChallenge { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

public sealed class ExternalAuthenticationSession
{
    public string Id { get; set; } = null!;
    public string AuthenticationClientId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public string ConnectionMaterialRevision { get; set; } = null!;
    public string? SecretGenerationFingerprint { get; set; }
    public string Issuer { get; set; } = null!;
    public string SubjectHash { get; set; } = null!;
    public IReadOnlyCollection<PermissionGrant> ExternalGrants { get; set; } = [];
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastRefreshedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RefreshExpiresAt { get; set; }
    public string CurrentRefreshTokenHash { get; set; } = null!;
    public long RefreshGeneration { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
}

/// <summary>Restricts administrative session queries to a single tenant and safe metadata fields.</summary>
public sealed class ExternalAuthenticationSessionFilter
{
    public string TenantId { get; set; } = null!;
    public string? UserId { get; set; }
    public string? ConnectionId { get; set; }
    /// <summary><c>active</c>, <c>revoked</c>, or null for both.</summary>
    public string? Status { get; set; }
}

public sealed record ConnectionObservation(
    string ConnectionId,
    string TestedMaterialRevision,
    DateTimeOffset ObservedAt,
    ConnectionObservationStatus Status,
    string Category,
    TimeSpan Duration,
    string Summary,
    IReadOnlyCollection<string> Warnings,
    string CorrelationId);

public sealed record LoginMethod(string Id, string Key, LoginMethodKind Kind, string DisplayName, string? IconId, int Order, bool IsDefault, Uri InitiationUri);

public sealed record ExternalIdentityLink(string Id, string TenantId, string ConnectionId, string Issuer, string SubjectHash, string? SubjectHint, string UserId, DateTimeOffset CreatedAt, DateTimeOffset? LastSignedInAt);

public sealed class ExternalIdentityLinkFilter
{
    public string TenantId { get; set; } = null!;
    public string? UserId { get; set; }
    public string? ConnectionId { get; set; }
}

public sealed record PreviewResult(
    string HandleHash,
    string AdministratorId,
    string TenantId,
    string ConnectionId,
    string MaterialRevision,
    string Issuer,
    string MaskedSubject,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims,
    string PolicyDecision,
    IReadOnlyCollection<PermissionGrant> PermissionProjection,
    IReadOnlyCollection<string> Warnings,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt);

public sealed class ConnectionFilter
{
    public string? Search { get; set; }
    public ConnectionSourceOwnership? Ownership { get; set; }
    public ConnectionScope? Scope { get; set; }
    public string? AdapterType { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsArchived { get; set; }
}

public abstract record ConnectionMutationResult
{
    private ConnectionMutationResult() { }
    public sealed record Created(IdentityProviderConnection Connection) : ConnectionMutationResult;
    public sealed record Updated(IdentityProviderConnection Connection) : ConnectionMutationResult;
    public sealed record NotFound : ConnectionMutationResult;
    public sealed record DuplicateKey : ConnectionMutationResult;
    public sealed record RevisionConflict(long CurrentRevision) : ConnectionMutationResult;
}
