using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Endpoints.Connections;

internal sealed class ConnectionScopeRequest
{
    public string? Kind { get; set; }
    public string? TenantId { get; set; }
}

internal sealed class ConnectionRequest
{
    public string? Key { get; set; }
    public ConnectionScopeRequest? Scope { get; set; }
    public string? AdapterType { get; set; }
    public int AdapterSettingsVersion { get; set; }
    public JsonElement AdapterSettings { get; set; }
    public Dictionary<string, SecretBinding>? SecretBindings { get; set; }
    public string? DisplayName { get; set; }
    public string? IconId { get; set; }
    public int Order { get; set; }
    public bool IsDefault { get; set; }
    public PolicySelection? UnlinkedPolicy { get; set; }
    public List<GrantSourceSelection>? PermissionGrantSources { get; set; }
    public ClaimProjectionRequest? ClaimProjection { get; set; }
    public string? UpstreamLogoutMode { get; set; }
    public bool ConfirmUnsafeSettings { get; set; }
    public bool ConfirmFinalLoginPathOverride { get; set; }

    public IdentityProviderConnection ToConnection(string fallbackTenantId) => new()
    {
        TenantId = Scope?.Kind?.ToLowerInvariant() switch
        {
            "host" => ConnectionScope.HostTenantId,
            "defaulttenant" or "default-tenant" or "default" => ConnectionScope.DefaultTenantId,
            "tenant" => Scope.TenantId ?? fallbackTenantId,
            _ => Scope?.TenantId ?? fallbackTenantId
        },
        Key = Key ?? string.Empty,
        AdapterType = AdapterType ?? string.Empty,
        AdapterSettingsVersion = AdapterSettingsVersion,
        AdapterSettings = AdapterSettings.ValueKind == JsonValueKind.Undefined ? default : AdapterSettings.Clone(),
        SecretBindings = SecretBindings?.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal) ?? new Dictionary<string, SecretBinding>(StringComparer.Ordinal),
        DisplayName = DisplayName ?? string.Empty,
        IconId = IconId,
        DisplayOrder = Order,
        IsDefault = IsDefault,
        UnlinkedPolicy = UnlinkedPolicy,
        PermissionGrantSources = PermissionGrantSources?.Select(x => new GrantSourceSelection(x.Type, x.SettingsVersion, x.Settings.ValueKind == JsonValueKind.Undefined ? default : x.Settings.Clone(), x.Order)).ToArray() ?? [],
        ClaimProjection = ClaimProjection?.ToProjection() ?? Elsa.ExternalAuthentication.Models.ClaimProjection.Empty,
        UpstreamLogoutMode = ParseUpstreamLogoutMode(UpstreamLogoutMode)
    };

    private static UpstreamLogoutMode ParseUpstreamLogoutMode(string? value) => value?.ToLowerInvariant() switch
    {
        "disabled" or null => Elsa.ExternalAuthentication.Models.UpstreamLogoutMode.Disabled,
        "userchoice" or "user-choice" or "user_choice" => Elsa.ExternalAuthentication.Models.UpstreamLogoutMode.UserChoice,
        "always" => Elsa.ExternalAuthentication.Models.UpstreamLogoutMode.Always,
        _ => (Elsa.ExternalAuthentication.Models.UpstreamLogoutMode)(-1)
    };
}

internal sealed class ClaimProjectionRequest
{
    public ICollection<string>? AllowedClaimTypes { get; set; }
    public ICollection<string>? RedactedClaimTypes { get; set; }
    public int MaximumClaimCount { get; set; }
    public int MaximumValueLength { get; set; }
    public int MaximumTotalBytes { get; set; }

    public ClaimProjection ToProjection() => new(
        new HashSet<string>(AllowedClaimTypes ?? [], StringComparer.Ordinal),
        new HashSet<string>(RedactedClaimTypes ?? [], StringComparer.Ordinal),
        MaximumClaimCount,
        MaximumValueLength,
        MaximumTotalBytes);
}

internal sealed record ConnectionSecretBindingResponse(string ResolverType, string Reference, string? ExpectedType, string? ExpectedScope, bool IsConfigured, bool IsResolvable);

internal sealed class SecretBindingRequest
{
    public string? ResolverType { get; set; }
    public string? Reference { get; set; }
    public string? ExpectedType { get; set; }
    public string? ExpectedScope { get; set; }

    public SecretBinding ToBinding() => new(ResolverType ?? string.Empty, Reference ?? string.Empty, ExpectedType, ExpectedScope);
}

internal sealed record ConnectionScopeResponse(string Kind, string TenantId);

internal sealed class ConnectionResponse
{
    public string Id { get; init; } = null!;
    public string Key { get; init; } = null!;
    public string Source { get; init; } = null!;
    public ConnectionScopeResponse Scope { get; init; } = null!;
    public string AdapterType { get; init; } = null!;
    public int AdapterSettingsVersion { get; init; }
    public JsonElement AdapterSettings { get; init; }
    public IReadOnlyDictionary<string, ConnectionSecretBindingResponse> SecretBindings { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? IconId { get; init; }
    public int Order { get; init; }
    public bool IsDefault { get; init; }
    public bool EnabledIntent { get; init; }
    public bool EffectivelyEnabled { get; init; }
    public string Validity { get; init; } = null!;
    public bool Shadowed { get; init; }
    public bool Archived { get; init; }
    public PolicySelection? UnlinkedPolicy { get; init; }
    public IReadOnlyCollection<GrantSourceSelection> PermissionGrantSources { get; init; } = [];
    public ClaimProjection ClaimProjection { get; init; } = ClaimProjection.Empty;
    public UpstreamLogoutMode UpstreamLogoutMode { get; init; }
    public long Revision { get; init; }
    public string MaterialRevision { get; init; } = null!;
    public ConnectionObservationResponse? LatestObservation { get; init; }

    public static async ValueTask<ConnectionResponse> FromAsync(EffectiveIdentityProviderConnection effective, Services.IdentityProviderConnectionManagementService management, ConnectionObservation? observation, CancellationToken cancellationToken)
    {
        var states = await management.GetSecretBindingStatesAsync(effective.Connection, cancellationToken);
        return new ConnectionResponse
        {
            Id = effective.Connection.Id,
            Key = effective.Connection.Key,
            Source = effective.Ownership == ConnectionSourceOwnership.Configuration ? "configuration" : "database",
            Scope = new ConnectionScopeResponse(effective.Scope.Kind switch { ConnectionScopeKind.Host => "host", ConnectionScopeKind.DefaultTenant => "defaultTenant", _ => "tenant" }, effective.Scope.TenantId),
            AdapterType = effective.Connection.AdapterType,
            AdapterSettingsVersion = effective.Connection.AdapterSettingsVersion,
            AdapterSettings = effective.Connection.AdapterSettings.ValueKind == JsonValueKind.Undefined ? default : effective.Connection.AdapterSettings.Clone(),
            SecretBindings = effective.Connection.SecretBindings.ToDictionary(x => x.Key, x =>
            {
                states.TryGetValue(x.Key, out var state);
                return new ConnectionSecretBindingResponse(x.Value.ResolverType, x.Value.Reference, x.Value.ExpectedType, x.Value.ExpectedScope, state?.IsConfigured ?? false, state?.IsResolvable ?? false);
            }, StringComparer.Ordinal),
            DisplayName = effective.Connection.DisplayName,
            IconId = effective.Connection.IconId,
            Order = effective.Connection.DisplayOrder,
            IsDefault = effective.Connection.IsDefault,
            EnabledIntent = effective.Connection.IsEnabled,
            EffectivelyEnabled = effective.Connection.IsEnabled && !effective.Connection.ArchivedAt.HasValue && !effective.IsShadowed && effective.Validity != ConnectionValidity.Invalid,
            Validity = effective.Validity.ToString().ToLowerInvariant(),
            Shadowed = effective.IsShadowed,
            Archived = effective.Connection.ArchivedAt.HasValue,
            UnlinkedPolicy = effective.Connection.UnlinkedPolicy,
            PermissionGrantSources = effective.Connection.PermissionGrantSources.ToArray(),
            ClaimProjection = effective.Connection.ClaimProjection,
            UpstreamLogoutMode = effective.Connection.UpstreamLogoutMode,
            Revision = effective.Connection.Revision,
            MaterialRevision = effective.Connection.MaterialRevision,
            LatestObservation = observation is null
                ? null
                : new ConnectionObservationResponse(
                    observation.Status.ToString().ToLowerInvariant(),
                    observation.ObservedAt,
                    observation.TestedMaterialRevision,
                    !string.Equals(observation.TestedMaterialRevision, effective.Connection.MaterialRevision, StringComparison.Ordinal),
                    observation.Category,
                    observation.Summary)
        };
    }
}

internal sealed record ConnectionObservationResponse(string Status, DateTimeOffset ObservedAt, string TestedMaterialRevision, bool IsStale, string Category, string Summary);
internal sealed record ConnectionValidationResponse(bool Valid, IReadOnlyCollection<ConnectionValidationError> Errors, IReadOnlyCollection<string> Warnings);
internal sealed record ConnectionListResponse(IReadOnlyCollection<ConnectionResponse> Items, string? NextCursor);
internal sealed record ManagementErrorResponse(string Error, string Message, object? Details = null);
