using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Responses;

public sealed class ListExternalAuthenticationConnectionsResponse
{
    public ICollection<ExternalAuthenticationConnection> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class ValidateExternalAuthenticationConnectionResponse
{
    public bool Valid { get; set; }
    public ICollection<ExternalAuthenticationConnectionValidationError> Errors { get; set; } = [];
    public ICollection<string> Warnings { get; set; } = [];
}

public sealed class ExternalAuthenticationConnectionValidationError
{
    public string Field { get; set; } = "";
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class TestExternalAuthenticationConnectionResponse
{
    public string Status { get; set; } = "";
    public DateTimeOffset ObservedAt { get; set; }
    public string TestedMaterialRevision { get; set; } = "";
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public ICollection<string> Warnings { get; set; } = [];
    public TimeSpan Duration { get; set; }
    public string CorrelationId { get; set; } = "";
}

public sealed class InitiateExternalAuthenticationPreviewResponse
{
    public string NavigationUrl { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class ExternalAuthenticationPreviewResult
{
    public string ConnectionId { get; set; } = "";
    public string MaterialRevision { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string MaskedSubject { get; set; } = "";
    public Dictionary<string, ICollection<string>> ProjectedClaims { get; set; } = new(StringComparer.Ordinal);
    public string PolicyDecision { get; set; } = "";
    public ICollection<ExternalAuthenticationPermissionGrant> PermissionProjection { get; set; } = [];
    public ICollection<string> Warnings { get; set; } = [];
}

public sealed class ExternalAuthenticationPermissionGrant
{
    public string Permission { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string SourceReference { get; set; } = "";
}
