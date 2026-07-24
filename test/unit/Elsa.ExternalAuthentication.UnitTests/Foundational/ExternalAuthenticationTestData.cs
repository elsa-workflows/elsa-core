using System.Text.Json;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

internal static class ExternalAuthenticationTestData
{
    public static IdentityProviderConnection CreateConnection(
        string id = "connection-a",
        string tenantId = "tenant-a",
        string key = "oidc",
        string displayName = "OpenID Connect",
        int displayOrder = 0,
        bool isDefault = false,
        bool isEnabled = true) => new()
    {
        Id = id,
        TenantId = tenantId,
        Key = key,
        AdapterType = "oidc",
        AdapterSettingsVersion = 1,
        AdapterSettings = JsonSerializer.SerializeToElement(new { authority = "https://issuer.example" }),
        SecretBindings = new Dictionary<string, SecretBinding>(StringComparer.Ordinal)
        {
            ["clientSecret"] = new("test", "secret-a")
        },
        DisplayName = displayName,
        DisplayOrder = displayOrder,
        IsDefault = isDefault,
        IsEnabled = isEnabled,
        ClaimProjection = new ClaimProjection(
            new HashSet<string>(StringComparer.Ordinal) { "email", "name" },
            new HashSet<string>(StringComparer.Ordinal) { "email" },
            64,
            1_024,
            16 * 1_024),
        UpstreamLogoutMode = UpstreamLogoutMode.Disabled
    };

    public static AuthorizationGrant CreateGrant(DateTimeOffset expiresAt) => new()
    {
        CodeHash = "code-hash",
        ClientId = "client",
        CallbackUri = new Uri("https://studio.example/callback"),
        TenantId = "tenant-a",
        UserId = "user-a",
        PkceChallenge = "challenge",
        ExpiresAt = expiresAt
    };

    public static ExternalAuthenticationSession CreateSession(DateTimeOffset now) => new()
    {
        Id = "session-a",
        TenantId = "tenant-a",
        UserId = "user-a",
        ConnectionId = "connection-a",
        ConnectionMaterialRevision = "revision-a",
        Issuer = "https://issuer.example",
        SubjectHash = "subject-hash",
        StartedAt = now,
        LastRefreshedAt = now,
        ExpiresAt = now.AddHours(1),
        RefreshExpiresAt = now.AddMinutes(30),
        CurrentRefreshTokenHash = "refresh-1"
    };

    public static PreviewResult CreatePreview(DateTimeOffset expiresAt) => new(
        "preview-hash",
        "administrator-a",
        "tenant-a",
        "connection-a",
        "revision-a",
        "https://issuer.example",
        "su***ct",
        new Dictionary<string, IReadOnlyCollection<string>>(),
        "reject",
        [],
        [],
        expiresAt,
        null);
}
