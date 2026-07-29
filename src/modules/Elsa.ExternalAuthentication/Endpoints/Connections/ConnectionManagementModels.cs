using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

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
    // Accepted only to return a precise error for clients attempting to mutate
    // secret references through the general connection document.
    public Dictionary<string, SecretBinding>? SecretBindings { get; set; }
    public string? DisplayName { get; set; }
    public string? IconId { get; set; }
    public int Order { get; set; }
    public bool IsPreferred { get; set; }
    public bool OverridesConfigurationConnection { get; set; }
    public PolicySelection? UnlinkedPolicy { get; set; }
    public List<GrantSourceSelection>? PermissionGrantSources { get; set; }
    public ClaimProjectionRequest? ClaimProjection { get; set; }
    public string? UpstreamLogoutMode { get; set; }
    public bool ConfirmUnsafeSettings { get; set; }
    public bool ConfirmFinalLoginPathOverride { get; set; }

    public bool HasOnlyHostScope() => Scope is null ||
        (string.IsNullOrWhiteSpace(Scope.TenantId) && (string.IsNullOrWhiteSpace(Scope.Kind) || string.Equals(Scope.Kind, "host", StringComparison.OrdinalIgnoreCase)));

    public IdentityProviderConnection ToConnection() => new()
    {
        TenantId = ConnectionScope.HostTenantId,
        Key = Key ?? string.Empty,
        AdapterType = AdapterType ?? string.Empty,
        AdapterSettingsVersion = AdapterSettingsVersion,
        AdapterSettings = AdapterSettings.ValueKind == JsonValueKind.Undefined ? default : AdapterSettings.Clone(),
        SecretBindings = new Dictionary<string, SecretBinding>(StringComparer.Ordinal),
        DisplayName = DisplayName ?? string.Empty,
        IconId = IconId,
        DisplayOrder = Order,
        IsPreferred = IsPreferred,
        OverridesConfigurationConnection = OverridesConfigurationConnection,
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

internal sealed record ConnectionSecretBindingResponse(string Ownership, string? ResolverType, string? Reference, bool IsConfigured, bool IsResolvable);

internal sealed class ManagedSecretBindingRequest
{
    public string? ResolverType { get; set; }
    public string? Value { get; set; }
}

internal sealed record ConnectionScopeResponse(string Kind, string TenantId);

internal sealed class ConnectionResponse
{
    public string Id { get; init; } = null!;
    public string Key { get; init; } = null!;
    public string Source { get; init; } = null!;
    public ConnectionScopeResponse Scope { get; init; } = null!;
    public string AdapterType { get; init; } = null!;
    public Uri? CallbackUri { get; init; }
    public Uri? PreviewCallbackUri { get; init; }
    public int AdapterSettingsVersion { get; init; }
    public JsonElement AdapterSettings { get; init; }
    public IReadOnlyDictionary<string, ConnectionSecretBindingResponse> SecretBindings { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? IconId { get; init; }
    public int Order { get; init; }
    public bool IsPreferred { get; init; }
    public bool OverridesConfigurationConnection { get; init; }
    public bool CanCreateOverride { get; init; }
    public bool CanPromoteToConfigurationOverride { get; init; }
    public bool EnabledIntent { get; init; }
    public bool EffectivelyEnabled { get; init; }
    public string Validity { get; init; } = null!;
    public bool Shadowed { get; init; }
    public bool Archived { get; init; }
    public PolicySelection? UnlinkedPolicy { get; init; }
    public IReadOnlyCollection<GrantSourceSelection> PermissionGrantSources { get; init; } = [];
    public ClaimProjection ClaimProjection { get; init; } = ClaimProjection.Empty;
    public string UpstreamLogoutMode { get; init; } = null!;
    public long Revision { get; init; }
    public string MaterialRevision { get; init; } = null!;
    public ConnectionObservationResponse? LatestObservation { get; init; }

    public static async ValueTask<ConnectionResponse> FromAsync(EffectiveIdentityProviderConnection effective, Services.IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ConnectionObservation? observation, CancellationToken cancellationToken)
    {
        var states = await management.GetSecretBindingStatesAsync(effective.Connection, cancellationToken);
        var adapterSettings = effective.Connection.AdapterSettings.ValueKind == JsonValueKind.Undefined
            ? default
            : adapters.TryGet(effective.Connection.AdapterType, out var adapter)
                ? AdapterSettingsSecretFieldGuard.RedactDeclaredSecrets(effective.Connection.AdapterSettings, adapter.Describe())
                : JsonSerializer.SerializeToElement(new Dictionary<string, object?>());
        return new ConnectionResponse
        {
            Id = effective.Connection.Id,
            Key = effective.Connection.Key,
            Source = effective.Ownership == ConnectionSourceOwnership.Configuration ? "configuration" : "database",
            Scope = new ConnectionScopeResponse(effective.Scope.Kind switch { ConnectionScopeKind.Host => "host", ConnectionScopeKind.DefaultTenant => "defaultTenant", _ => "tenant" }, effective.Scope.TenantId),
            AdapterType = effective.Connection.AdapterType,
            CallbackUri = management.GetProviderCallbackUri(effective.Connection),
            PreviewCallbackUri = management.GetProviderPreviewCallbackUri(effective.Connection),
            AdapterSettingsVersion = effective.Connection.AdapterSettingsVersion,
            AdapterSettings = adapterSettings,
            SecretBindings = effective.Connection.SecretBindings.ToDictionary(x => x.Key, x =>
            {
                states.TryGetValue(x.Key, out var state);
                var presentation = management.PresentSecretBinding(x.Value, state);
                return new ConnectionSecretBindingResponse(
                    presentation.Ownership,
                    x.Value.Ownership == SecretBindingOwnership.External ? x.Value.ResolverType : null,
                    x.Value.Ownership == SecretBindingOwnership.External ? x.Value.Reference : null,
                    presentation.IsConfigured,
                    presentation.IsResolvable);
            }, StringComparer.Ordinal),
            DisplayName = effective.Connection.DisplayName,
            IconId = effective.Connection.IconId,
            Order = effective.Connection.DisplayOrder,
            IsPreferred = effective.Connection.IsPreferred,
            OverridesConfigurationConnection = effective.Connection.OverridesConfigurationConnection,
            CanCreateOverride = effective.Ownership == ConnectionSourceOwnership.Configuration && management.CanCreateConfigurationOverride(),
            CanPromoteToConfigurationOverride = management.CanPromoteToConfigurationOverride(effective),
            EnabledIntent = effective.Connection.IsEnabled,
            EffectivelyEnabled = effective.Connection.IsEnabled && !effective.Connection.ArchivedAt.HasValue && !effective.IsShadowed && effective.Validity != ConnectionValidity.Invalid,
            Validity = effective.Validity.ToString().ToLowerInvariant(),
            Shadowed = effective.IsShadowed,
            Archived = effective.Connection.ArchivedAt.HasValue,
            UnlinkedPolicy = effective.Connection.UnlinkedPolicy,
            PermissionGrantSources = effective.Connection.PermissionGrantSources.ToArray(),
            ClaimProjection = effective.Connection.ClaimProjection,
            UpstreamLogoutMode = FormatUpstreamLogoutMode(effective.Connection.UpstreamLogoutMode),
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

    private static string FormatUpstreamLogoutMode(UpstreamLogoutMode mode) => mode switch
    {
        Elsa.ExternalAuthentication.Models.UpstreamLogoutMode.Disabled => "disabled",
        Elsa.ExternalAuthentication.Models.UpstreamLogoutMode.UserChoice => "user-choice",
        Elsa.ExternalAuthentication.Models.UpstreamLogoutMode.Always => "always",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "The upstream logout mode is not supported.")
    };
}

internal sealed record ConnectionObservationResponse(string Status, DateTimeOffset ObservedAt, string TestedMaterialRevision, bool IsStale, string Category, string Summary);
internal sealed record ConnectionValidationResponse(bool Valid, IReadOnlyCollection<ConnectionValidationError> Errors, IReadOnlyCollection<string> Warnings);
internal sealed record ConnectionListResponse(IReadOnlyCollection<ConnectionResponse> Items, string? NextCursor);
internal sealed record ManagementErrorResponse(string Error, string Message, object? Details, string CorrelationId);
