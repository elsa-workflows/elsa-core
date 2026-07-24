namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

/// <summary>Marker type used to locate the external-authentication EF model configurations.</summary>
public sealed class ExternalAuthenticationPersistenceMarker;

public sealed class PersistedIdentityProviderConnection
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string AdapterType { get; set; } = null!;
    public int AdapterSettingsVersion { get; set; }
    public string AdapterSettingsJson { get; set; } = null!;
    public string SecretBindingsJson { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? IconId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public string? UnlinkedPolicyJson { get; set; }
    public string PermissionGrantSourcesJson { get; set; } = null!;
    public string ClaimProjectionJson { get; set; } = null!;
    public int UpstreamLogoutMode { get; set; }
    public long Revision { get; set; }
    public string MaterialRevision { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PersistedExternalIdentityLink
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string SubjectHash { get; set; } = null!;
    public string? SubjectHint { get; set; }
    public string UserId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSignedInAt { get; set; }
}

public sealed class PersistedAuthenticationClient
{
    public string ClientId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int ClientType { get; set; }
    public string CallbackUrisJson { get; set; } = null!;
    public string LogoutCallbackUrisJson { get; set; } = null!;
    public string AllowedOriginsJson { get; set; } = null!;
    public string AllowedReturnPathPrefixesJson { get; set; } = null!;
    public string? SecretBindingJson { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class PersistedBrokerTransaction
{
    public string HandleHash { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string CallbackUri { get; set; } = null!;
    public string ReturnPath { get; set; } = null!;
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

public sealed class PersistedAuthorizationGrant
{
    public string CodeHash { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string CallbackUri { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? ExternalSessionId { get; set; }
    public string PkceChallenge { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

public sealed class PersistedExternalAuthenticationSession
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
    public string ExternalGrantsJson { get; set; } = null!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastRefreshedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RefreshExpiresAt { get; set; }
    public string CurrentRefreshTokenHash { get; set; } = null!;
    public long RefreshGeneration { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
}

public sealed class PersistedConnectionObservation
{
    public string ConnectionId { get; set; } = null!;
    public string TestedMaterialRevision { get; set; } = null!;
    public DateTimeOffset ObservedAt { get; set; }
    public int Status { get; set; }
    public string Category { get; set; } = null!;
    public long DurationTicks { get; set; }
    public string Summary { get; set; } = null!;
    public string WarningsJson { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
}

public sealed class PersistedPreviewResult
{
    public string HandleHash { get; set; } = null!;
    public string AdministratorId { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public string MaterialRevision { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string MaskedSubject { get; set; } = null!;
    public string ProjectedClaimsJson { get; set; } = null!;
    public string PolicyDecision { get; set; } = null!;
    public string PermissionProjectionJson { get; set; } = null!;
    public string WarningsJson { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

public sealed class ExternalAuthenticationRegistryVersion
{
    public int Id { get; set; }
    public long Version { get; set; }
}
