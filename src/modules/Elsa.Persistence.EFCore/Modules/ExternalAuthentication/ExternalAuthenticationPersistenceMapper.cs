using System.Text.Json;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

internal static class ExternalAuthenticationPersistenceMapper
{
    public static PersistedIdentityProviderConnection ToPersisted(this IdentityProviderConnection connection) => new()
    {
        Id = connection.Id,
        TenantId = connection.TenantId,
        Key = connection.Key,
        AdapterType = connection.AdapterType,
        AdapterSettingsVersion = connection.AdapterSettingsVersion,
        AdapterSettingsJson = connection.AdapterSettings.GetRawText(),
        SecretBindingsJson = ExternalAuthenticationJsonSerializer.Serialize(connection.SecretBindings),
        DisplayName = connection.DisplayName,
        IconId = connection.IconId,
        DisplayOrder = connection.DisplayOrder,
        IsDefault = connection.IsDefault,
        IsEnabled = connection.IsEnabled,
        ArchivedAt = connection.ArchivedAt,
        UnlinkedPolicyJson = connection.UnlinkedPolicy is null ? null : ExternalAuthenticationJsonSerializer.Serialize(connection.UnlinkedPolicy),
        PermissionGrantSourcesJson = ExternalAuthenticationJsonSerializer.Serialize(connection.PermissionGrantSources),
        ClaimProjectionJson = ExternalAuthenticationJsonSerializer.Serialize(ClaimProjectionDocument.FromModel(connection.ClaimProjection)),
        UpstreamLogoutMode = (int)connection.UpstreamLogoutMode,
        Revision = connection.Revision,
        MaterialRevision = connection.MaterialRevision,
        CreatedAt = connection.CreatedAt,
        UpdatedAt = connection.UpdatedAt
    };

    public static IdentityProviderConnection ToModel(this PersistedIdentityProviderConnection connection) => new()
    {
        Id = connection.Id,
        TenantId = connection.TenantId,
        Key = connection.Key,
        AdapterType = connection.AdapterType,
        AdapterSettingsVersion = connection.AdapterSettingsVersion,
        AdapterSettings = JsonDocument.Parse(connection.AdapterSettingsJson).RootElement.Clone(),
        SecretBindings = ExternalAuthenticationJsonSerializer.Deserialize<Dictionary<string, SecretBinding>>(connection.SecretBindingsJson),
        DisplayName = connection.DisplayName,
        IconId = connection.IconId,
        DisplayOrder = connection.DisplayOrder,
        IsDefault = connection.IsDefault,
        IsEnabled = connection.IsEnabled,
        ArchivedAt = connection.ArchivedAt,
        UnlinkedPolicy = connection.UnlinkedPolicyJson is null ? null : ExternalAuthenticationJsonSerializer.Deserialize<PolicySelection>(connection.UnlinkedPolicyJson),
        PermissionGrantSources = ExternalAuthenticationJsonSerializer.Deserialize<List<GrantSourceSelection>>(connection.PermissionGrantSourcesJson),
        ClaimProjection = ExternalAuthenticationJsonSerializer.Deserialize<ClaimProjectionDocument>(connection.ClaimProjectionJson).ToModel(),
        UpstreamLogoutMode = (UpstreamLogoutMode)connection.UpstreamLogoutMode,
        Revision = connection.Revision,
        MaterialRevision = connection.MaterialRevision,
        CreatedAt = connection.CreatedAt,
        UpdatedAt = connection.UpdatedAt
    };

    public static PersistedBrokerTransaction ToPersisted(this BrokerTransaction transaction, string purpose) => new()
    {
        HandleHash = transaction.HandleHash,
        Purpose = purpose,
        ClientId = transaction.ClientId,
        CallbackUri = transaction.CallbackUri.AbsoluteUri,
        ReturnPath = transaction.ReturnPath,
        ClientState = transaction.ClientState,
        TenantId = transaction.TenantId,
        ConnectionId = transaction.ConnectionId,
        ConnectionMaterialRevision = transaction.ConnectionMaterialRevision,
        SecretGenerationFingerprint = transaction.SecretGenerationFingerprint,
        PkceChallenge = transaction.PkceChallenge,
        ProviderNonce = transaction.ProviderNonce,
        ProtectedPayload = transaction.ProtectedPayload,
        ExpiresAt = transaction.ExpiresAt,
        ConsumedAt = transaction.ConsumedAt
    };

    public static BrokerTransaction ToModel(this PersistedBrokerTransaction transaction) => new()
    {
        HandleHash = transaction.HandleHash,
        Purpose = Enum.Parse<BrokerTransactionPurpose>(transaction.Purpose, ignoreCase: false),
        ClientId = transaction.ClientId,
        CallbackUri = new Uri(transaction.CallbackUri, UriKind.Absolute),
        ReturnPath = transaction.ReturnPath,
        ClientState = transaction.ClientState,
        TenantId = transaction.TenantId,
        ConnectionId = transaction.ConnectionId,
        ConnectionMaterialRevision = transaction.ConnectionMaterialRevision,
        SecretGenerationFingerprint = transaction.SecretGenerationFingerprint,
        PkceChallenge = transaction.PkceChallenge,
        ProviderNonce = transaction.ProviderNonce,
        ProtectedPayload = transaction.ProtectedPayload,
        ExpiresAt = transaction.ExpiresAt,
        ConsumedAt = transaction.ConsumedAt
    };

    public static PersistedAuthorizationGrant ToPersisted(this AuthorizationGrant grant) => new()
    {
        CodeHash = grant.CodeHash,
        ClientId = grant.ClientId,
        CallbackUri = grant.CallbackUri.AbsoluteUri,
        TenantId = grant.TenantId,
        UserId = grant.UserId,
        ExternalSessionId = grant.ExternalSessionId,
        PkceChallenge = grant.PkceChallenge,
        ExpiresAt = grant.ExpiresAt,
        ConsumedAt = grant.ConsumedAt
    };

    public static AuthorizationGrant ToModel(this PersistedAuthorizationGrant grant) => new()
    {
        CodeHash = grant.CodeHash,
        ClientId = grant.ClientId,
        CallbackUri = new Uri(grant.CallbackUri, UriKind.Absolute),
        TenantId = grant.TenantId,
        UserId = grant.UserId,
        ExternalSessionId = grant.ExternalSessionId,
        PkceChallenge = grant.PkceChallenge,
        ExpiresAt = grant.ExpiresAt,
        ConsumedAt = grant.ConsumedAt
    };

    public static PersistedExternalAuthenticationSession ToPersisted(this ExternalAuthenticationSession session) => new()
    {
        Id = session.Id,
        AuthenticationClientId = session.AuthenticationClientId,
        TenantId = session.TenantId,
        UserId = session.UserId,
        ConnectionId = session.ConnectionId,
        ConnectionMaterialRevision = session.ConnectionMaterialRevision,
        SecretGenerationFingerprint = session.SecretGenerationFingerprint,
        Issuer = session.Issuer,
        SubjectHash = session.SubjectHash,
        ExternalGrantsJson = ExternalAuthenticationJsonSerializer.Serialize(session.ExternalGrants),
        StartedAt = session.StartedAt,
        LastRefreshedAt = session.LastRefreshedAt,
        ExpiresAt = session.ExpiresAt,
        RefreshExpiresAt = session.RefreshExpiresAt,
        CurrentRefreshTokenHash = session.CurrentRefreshTokenHash,
        RefreshGeneration = session.RefreshGeneration,
        RevokedAt = session.RevokedAt,
        RevocationReason = session.RevocationReason
    };

    public static ExternalAuthenticationSession ToModel(this PersistedExternalAuthenticationSession session) => new()
    {
        Id = session.Id,
        AuthenticationClientId = session.AuthenticationClientId,
        TenantId = session.TenantId,
        UserId = session.UserId,
        ConnectionId = session.ConnectionId,
        ConnectionMaterialRevision = session.ConnectionMaterialRevision,
        SecretGenerationFingerprint = session.SecretGenerationFingerprint,
        Issuer = session.Issuer,
        SubjectHash = session.SubjectHash,
        ExternalGrants = ExternalAuthenticationJsonSerializer.Deserialize<PermissionGrant[]>(session.ExternalGrantsJson),
        StartedAt = session.StartedAt,
        LastRefreshedAt = session.LastRefreshedAt,
        ExpiresAt = session.ExpiresAt,
        RefreshExpiresAt = session.RefreshExpiresAt,
        CurrentRefreshTokenHash = session.CurrentRefreshTokenHash,
        RefreshGeneration = session.RefreshGeneration,
        RevokedAt = session.RevokedAt,
        RevocationReason = session.RevocationReason
    };

    public static PersistedPreviewResult ToPersisted(this PreviewResult result) => new()
    {
        HandleHash = result.HandleHash,
        AdministratorId = result.AdministratorId,
        TenantId = result.TenantId,
        ConnectionId = result.ConnectionId,
        MaterialRevision = result.MaterialRevision,
        Issuer = result.Issuer,
        MaskedSubject = result.MaskedSubject,
        ProjectedClaimsJson = ExternalAuthenticationJsonSerializer.Serialize(result.ProjectedClaims),
        PolicyDecision = result.PolicyDecision,
        PermissionProjectionJson = ExternalAuthenticationJsonSerializer.Serialize(result.PermissionProjection),
        WarningsJson = ExternalAuthenticationJsonSerializer.Serialize(result.Warnings),
        ExpiresAt = result.ExpiresAt,
        ConsumedAt = result.ConsumedAt
    };

    public static PreviewResult ToModel(this PersistedPreviewResult result) => new(
        result.HandleHash,
        result.AdministratorId,
        result.TenantId,
        result.ConnectionId,
        result.MaterialRevision,
        result.Issuer,
        result.MaskedSubject,
        ExternalAuthenticationJsonSerializer.Deserialize<Dictionary<string, IReadOnlyCollection<string>>>(result.ProjectedClaimsJson),
        result.PolicyDecision,
        ExternalAuthenticationJsonSerializer.Deserialize<PermissionGrant[]>(result.PermissionProjectionJson),
        ExternalAuthenticationJsonSerializer.Deserialize<string[]>(result.WarningsJson),
        result.ExpiresAt,
        result.ConsumedAt);

    private sealed record ClaimProjectionDocument(
        string[] AllowedClaimTypes,
        string[] RedactedClaimTypes,
        int MaximumClaimCount,
        int MaximumValueLength,
        int MaximumTotalBytes)
    {
        public static ClaimProjectionDocument FromModel(ClaimProjection projection) => new(
            projection.AllowedClaimTypes.Order(StringComparer.Ordinal).ToArray(),
            projection.RedactedClaimTypes.Order(StringComparer.Ordinal).ToArray(),
            projection.MaximumClaimCount,
            projection.MaximumValueLength,
            projection.MaximumTotalBytes);

        public ClaimProjection ToModel() => new(
            new HashSet<string>(AllowedClaimTypes, StringComparer.Ordinal),
            new HashSet<string>(RedactedClaimTypes, StringComparer.Ordinal),
            MaximumClaimCount,
            MaximumValueLength,
            MaximumTotalBytes);
    }
}
