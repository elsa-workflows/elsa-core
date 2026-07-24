using System.Text.Json;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Requests;

public sealed class ListExternalAuthenticationConnectionsRequest
{
    public string? Search { get; set; }
    public string? Source { get; set; }
    public string? Scope { get; set; }
    public string? TenantId { get; set; }
    public string? AdapterType { get; set; }
    public bool? Enabled { get; set; }
    public bool? Valid { get; set; }
    public bool? Shadowed { get; set; }
    public bool? Archived { get; set; }
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 100;
}

public sealed class SaveExternalAuthenticationConnectionRequest
{
    public string Key { get; set; } = "";
    public ExternalAuthenticationConnectionScope Scope { get; set; } = new();
    public string AdapterType { get; set; } = "";
    public int AdapterSettingsVersion { get; set; }
    public JsonElement AdapterSettings { get; set; }
    public Dictionary<string, SaveExternalAuthenticationSecretBindingRequest> SecretBindings { get; set; } = new(StringComparer.Ordinal);
    public string DisplayName { get; set; } = "";
    public string? IconId { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
    public ExternalAuthenticationPolicySelection? UnlinkedPolicy { get; set; }
    public ICollection<ExternalAuthenticationGrantSourceSelection> PermissionGrantSources { get; set; } = [];
    public ExternalAuthenticationClaimProjection ClaimProjection { get; set; } = new();
    public string UpstreamLogoutMode { get; set; } = "disabled";
    public bool ConfirmUnsafeSettings { get; set; }
}

public sealed class SaveExternalAuthenticationSecretBindingRequest
{
    public string ResolverType { get; set; } = "";
    public string Reference { get; set; } = "";
    public string? ExpectedType { get; set; }
    public string? ExpectedScope { get; set; }
}
