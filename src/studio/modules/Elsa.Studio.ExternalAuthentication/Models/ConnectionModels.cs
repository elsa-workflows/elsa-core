using System.Text.Json;

namespace Elsa.Studio.ExternalAuthentication.Models;

public static class ExternalAuthenticationPermissions
{
    public const string Read = "external-authentication:connections:read";
    public const string Create = "external-authentication:connections:create";
    public const string Update = "external-authentication:connections:update";
    public const string Archive = "external-authentication:connections:archive";
    public const string Test = "external-authentication:connections:test";
    public const string Preview = "external-authentication:connections:preview";
    public const string ManagePolicies = "external-authentication:policies:manage";
    public const string DelegatePermissions = "external-authentication:permissions:delegate";
    public const string DelegatePermissionsUnrestricted = "external-authentication:permissions:delegate-unrestricted";
    public const string ManageLinks = "external-authentication:links:manage";
    public const string SessionsRead = "external-authentication:sessions:read";
    public const string SessionsRevoke = "external-authentication:sessions:revoke";
    public const string UnsafeProviderTrust = "external-authentication:provider-trust:unsafe";
    public const string RolesRead = "read:role";
}

public sealed class ConnectionScope
{
    public string Kind { get; set; } = "host";
    public string? TenantId { get; set; }

    public string DisplayName => Kind switch
    {
        "host" => "Host-wide",
        "default" => "Default tenant",
        "tenant" when !string.IsNullOrWhiteSpace(TenantId) => $"Tenant: {TenantId}",
        _ => Kind
    };
}

public sealed class ConnectionObservation
{
    public string Status { get; set; } = "unknown";
    public DateTimeOffset ObservedAt { get; set; }
    public string TestedMaterialRevision { get; set; } = string.Empty;
    public bool IsStale { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class ConnectionReference
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class ConnectionSummary
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Source { get; set; } = "database";
    public ConnectionScope Scope { get; set; } = new();
    public string AdapterType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? IconId { get; set; }
    public int Order { get; set; }
    public bool IsPreferred { get; set; }
    public bool OverridesConfigurationConnection { get; set; }
    public bool EnabledIntent { get; set; }
    public bool EffectivelyEnabled { get; set; }
    public string Validity { get; set; } = "unknown";
    public bool Shadowed { get; set; }
    public ConnectionReference? ShadowedBy { get; set; }
    public ICollection<ConnectionReference> Shadows { get; set; } = [];
    public bool Archived { get; set; }
    public bool CanCreateOverride { get; set; }
    public bool CanPromoteToConfigurationOverride { get; set; }
    public long Revision { get; set; }
    public string MaterialRevision { get; set; } = string.Empty;
    public ConnectionObservation? LatestObservation { get; set; }

    public bool IsConfigurationOwned => string.Equals(Source, "configuration", StringComparison.OrdinalIgnoreCase);
}

public sealed class ListConnectionsResponse
{
    public ICollection<ConnectionSummary> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class ExternalAuthenticationRuntimeDescriptor
{
    public int ManagementContractVersion { get; set; }
    public string ProductVersion { get; set; } = string.Empty;
    public string InformationalVersion { get; set; } = string.Empty;
}

public sealed class SecretBindingState
{
    public string Ownership { get; set; } = "external";
    public bool IsConfigured { get; set; }
    public bool IsResolvable { get; set; }
}

public sealed class ConnectionDetail : ConnectionSummary
{
    public string? CallbackUri { get; set; }
    public string? PreviewCallbackUri { get; set; }
    public int AdapterSettingsVersion { get; set; } = 1;
    public Dictionary<string, JsonElement> AdapterSettings { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, SecretBindingState> SecretBindings { get; set; } = new(StringComparer.Ordinal);
    public PolicySelection? UnlinkedPolicy { get; set; }
    public ICollection<PermissionGrantSourceSelection> PermissionGrantSources { get; set; } = [];
    public ClaimProjection ClaimProjection { get; set; } = new();
    public string UpstreamLogoutMode { get; set; } = "disabled";
    public ICollection<ConnectionValidationMessage> ValidationErrors { get; set; } = [];
    public ICollection<string> ValidationWarnings { get; set; } = [];
}

public sealed class ConnectionValidationMessage
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ConnectionValidationResult
{
    public bool Valid { get; set; }
    public ICollection<ConnectionValidationMessage> Errors { get; set; } = [];
    public ICollection<string> Warnings { get; set; } = [];
}

public sealed class ConnectionFieldDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = "text";
    public string UiHint { get; set; } = "text";
    public bool IsRequired { get; set; }
    public bool IsSecretBinding { get; set; }
    public bool IsUnsafe { get; set; }
    public JsonElement? DefaultValue { get; set; }
    public ICollection<string> AllowedValues { get; set; } = [];
    public ConnectionFieldValidation Validation { get; set; } = new();
    public ConnectionFieldVisibilityCondition? VisibleWhen { get; set; }
    public string? HelpText { get; set; }
    public bool IsRedacted { get; set; }
    public bool IsReadOnly { get; set; }
}

public sealed class ConnectionFieldValidation
{
    public int? MinimumLength { get; set; }
    public int? MaximumLength { get; set; }
    public string? Pattern { get; set; }
}

public sealed class ConnectionFieldVisibilityCondition
{
    public string Field { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
}

public sealed class CustomEditorContract
{
    public string Key { get; set; } = string.Empty;
    public int ContractVersion { get; set; }
}

public sealed class AdapterCapabilities
{
    public bool SupportsTest { get; set; }
    public bool SupportsPreview { get; set; }
    public bool SupportsUpstreamLogout { get; set; }
}

public sealed class AdapterDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SettingsVersion { get; set; }
    public ICollection<ConnectionFieldDescriptor> Fields { get; set; } = [];
    public AdapterCapabilities Capabilities { get; set; } = new();
    public CustomEditorContract? CustomEditor { get; set; }
}

public sealed class ConnectionMutation
{
    public string Key { get; set; } = string.Empty;
    public ConnectionScope Scope { get; set; } = new();
    public bool OverridesConfigurationConnection { get; set; }
    public string AdapterType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? IconId { get; set; }
    public int Order { get; set; }
    public bool IsPreferred { get; set; }
    public int AdapterSettingsVersion { get; set; } = 1;
    public Dictionary<string, JsonElement> AdapterSettings { get; set; } = new(StringComparer.Ordinal);
    public PolicySelection? UnlinkedPolicy { get; set; }
    public ICollection<PermissionGrantSourceSelection> PermissionGrantSources { get; set; } = [];
    public ClaimProjection ClaimProjection { get; set; } = new();
    public string UpstreamLogoutMode { get; set; } = "disabled";
    public bool ConfirmUnsafeSettings { get; set; }
}

public sealed class PolicySelection
{
    public string Type { get; set; } = string.Empty;
    public int SettingsVersion { get; set; }
    public JsonElement Settings { get; set; }
}

public sealed class PermissionGrantSourceSelection
{
    public string Type { get; set; } = string.Empty;
    public int SettingsVersion { get; set; }
    public Dictionary<string, JsonElement> Settings { get; set; } = new(StringComparer.Ordinal);
    public int Order { get; set; }
}

public sealed class ClaimProjection
{
    public ICollection<string> AllowedClaimTypes { get; set; } = [];
    public ICollection<string> RedactedClaimTypes { get; set; } = [];
    public int MaximumClaimCount { get; set; }
    public int MaximumValueLength { get; set; }
    public int MaximumTotalBytes { get; set; }
}

public sealed class PermissionGrantSourceDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SettingsVersion { get; set; }
    public ICollection<ConnectionFieldDescriptor> Fields { get; set; } = [];
    public CustomEditorContract? CustomEditor { get; set; }
}

public sealed class UnlinkedIdentityPolicyDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SettingsVersion { get; set; }
    public ICollection<ConnectionFieldDescriptor> Fields { get; set; } = [];
    public CustomEditorContract? CustomEditor { get; set; }
}

public sealed class ExternalUserMatcherDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SettingsVersion { get; set; }
    public ICollection<ConnectionFieldDescriptor> Fields { get; set; } = [];
    public CustomEditorContract? CustomEditor { get; set; }
    public ICollection<string> RequiredClaimTypes { get; set; } = [];
}

public sealed class IdentityRoleOptionsResponse
{
    public ICollection<IdentityRoleOption> Roles { get; set; } = [];
}

public sealed class IdentityRoleOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PermissionDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public sealed class PermissionProjection
{
    public string Permission { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
}

public sealed class PreviewSignInResult
{
    public string Issuer { get; set; } = string.Empty;
    public string MaskedSubject { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> ProjectedClaims { get; set; } = new(StringComparer.Ordinal);
    public JsonElement PolicyDecision { get; set; }
    public string ProposedAction { get; set; } = string.Empty;
    public ICollection<PermissionProjection> PermissionProjection { get; set; } = [];
    public ICollection<string> Warnings { get; set; } = [];
    public string MaterialRevision { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class ManagedSecretMutation
{
    public string ResolverType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class ManagedSecretResolverCatalog
{
    public ICollection<ManagedSecretResolverDescriptor> Items { get; set; } = [];
}

public sealed class ManagedSecretResolverDescriptor
{
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class ManagementApiException(string message, int statusCode, ConnectionDetail? current = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public ConnectionDetail? Current { get; } = current;
    public bool IsConcurrencyConflict => StatusCode == 412;
}
