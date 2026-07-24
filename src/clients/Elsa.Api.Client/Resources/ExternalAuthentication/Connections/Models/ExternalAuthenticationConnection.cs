using System.Text.Json;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;

/// <summary>
/// An external authentication connection exposed by the management API.
/// </summary>
public sealed class ExternalAuthenticationConnection
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public string Source { get; set; } = "";
    public ExternalAuthenticationConnectionScope Scope { get; set; } = new();
    public string AdapterType { get; set; } = "";
    public int AdapterSettingsVersion { get; set; }
    public JsonElement AdapterSettings { get; set; }
    public Dictionary<string, ExternalAuthenticationSecretBindingState> SecretBindings { get; set; } = new(StringComparer.Ordinal);
    public string DisplayName { get; set; } = "";
    public string? IconId { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
    public bool EnabledIntent { get; set; }
    public bool EffectivelyEnabled { get; set; }
    public string Validity { get; set; } = "";
    public bool Shadowed { get; set; }
    public bool Archived { get; set; }
    public ExternalAuthenticationPolicySelection? UnlinkedPolicy { get; set; }
    public ICollection<ExternalAuthenticationGrantSourceSelection> PermissionGrantSources { get; set; } = [];
    public ExternalAuthenticationClaimProjection ClaimProjection { get; set; } = new();
    public string UpstreamLogoutMode { get; set; } = "disabled";
    public long Revision { get; set; }
    public string MaterialRevision { get; set; } = "";
    public ExternalAuthenticationConnectionObservation? LatestObservation { get; set; }
}

public sealed class ExternalAuthenticationConnectionScope
{
    public string Kind { get; set; } = "tenant";
    public string? TenantId { get; set; }
}

public sealed class ExternalAuthenticationSecretBindingState
{
    public string ResolverType { get; set; } = "";
    public string Reference { get; set; } = "";
    public string? ExpectedType { get; set; }
    public string? ExpectedScope { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsResolvable { get; set; }
}

public sealed class ExternalAuthenticationPolicySelection
{
    public string Type { get; set; } = "";
    public int SettingsVersion { get; set; }
    public JsonElement Settings { get; set; }
}

public sealed class ExternalAuthenticationGrantSourceSelection
{
    public string Type { get; set; } = "";
    public int SettingsVersion { get; set; }
    public JsonElement Settings { get; set; }
    public int Order { get; set; }
}

public sealed class ExternalAuthenticationClaimProjection
{
    public ICollection<string> AllowedClaimTypes { get; set; } = [];
    public ICollection<string> RedactedClaimTypes { get; set; } = [];
    public int MaximumClaimCount { get; set; }
    public int MaximumValueLength { get; set; }
    public int MaximumTotalBytes { get; set; }
}

public sealed class ExternalAuthenticationConnectionObservation
{
    public string Status { get; set; } = "";
    public DateTimeOffset ObservedAt { get; set; }
    public string TestedMaterialRevision { get; set; } = "";
    public bool IsStale { get; set; }
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
}
