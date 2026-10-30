using System.Text.Json;

namespace Elsa.Studio.ExternalAuthentication.Models;

/// <summary>Redacted latest result from an on-demand provider connection test.</summary>
public sealed class ConnectionTestResult
{
    public string Status { get; set; } = "unknown";
    public DateTimeOffset ObservedAt { get; set; }
    public string TestedMaterialRevision { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public ICollection<string> Warnings { get; set; } = [];
    public TimeSpan Duration { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    public ConnectionObservation ToObservation() => new()
    {
        Status = Status,
        ObservedAt = ObservedAt,
        TestedMaterialRevision = TestedMaterialRevision,
        IsStale = false,
        Category = Category,
        Summary = Summary
    };
}

/// <summary>One-time preview flow start response. The navigation URL is an Elsa-owned route.</summary>
public sealed class PreviewInitiation
{
    public string NavigationUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Redacted, one-time preview result. It never represents a normal Elsa session or credential.</summary>
public sealed class PreviewResultDocument
{
    public string ConnectionId { get; set; } = string.Empty;
    public string MaterialRevision { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string MaskedSubject { get; set; } = string.Empty;
    public Dictionary<string, ICollection<string>> ProjectedClaims { get; set; } = new(StringComparer.Ordinal);
    public string PolicyDecision { get; set; } = string.Empty;
    public ICollection<PermissionProjection> PermissionProjection { get; set; } = [];
    public ICollection<string> Warnings { get; set; } = [];

    public PreviewSignInResult ToPreviewSignInResult() => new()
    {
        MaterialRevision = MaterialRevision,
        Issuer = Issuer,
        MaskedSubject = MaskedSubject,
        ProjectedClaims = ProjectedClaims.ToDictionary(
            pair => pair.Key,
            pair => JsonSerializer.SerializeToElement(pair.Value),
            StringComparer.Ordinal),
        PolicyDecision = JsonSerializer.SerializeToElement(PolicyDecision),
        ProposedAction = PolicyDecision,
        PermissionProjection = PermissionProjection,
        Warnings = Warnings
    };
}

public sealed class ExternalAuthenticationSessionSummary
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastRefreshedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ListExternalAuthenticationSessionsResponse
{
    public ICollection<ExternalAuthenticationSessionSummary> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class RevokeExternalAuthenticationSessionRequest
{
    public string? Reason { get; set; }
}
