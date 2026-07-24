using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Permissions;

/// <summary>
/// Grants only provider claim values explicitly bounded by configured literal permission names.
/// </summary>
public sealed class ClaimPassThroughPermissionGrantSource : IPermissionGrantSource
{
    public const string SourceType = "claim-pass-through";
    public string Type => SourceType;

    public PermissionGrantSourceDescriptor Describe() => new(Type, "Bounded claim pass-through", "Grants only projected claim values included in an explicit literal permission boundary.", 1, BuiltInPermissionGrantSourceDescriptors.PassThrough(), null);

    public ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default)
    {
        if (context.Selection.Settings.ValueKind != System.Text.Json.JsonValueKind.Object || !context.Selection.Settings.TryGetProperty("claimType", out var claimTypeProperty) || claimTypeProperty.ValueKind != System.Text.Json.JsonValueKind.String)
            return ValueTask.FromResult(new PermissionGrantResult([], []));

        var claimType = claimTypeProperty.GetString();
        var boundary = PermissionGrantMappingSettings.ReadPassThroughPermissions(context.Selection.Settings);
        if (string.IsNullOrWhiteSpace(claimType) || boundary.Count == 0 || !context.ProjectedClaims.TryGetValue(claimType, out var values))
            return ValueTask.FromResult(new PermissionGrantResult([], []));

        var grants = values.Where(boundary.Contains).OrderBy(x => x, StringComparer.Ordinal).Select(value => new PermissionGrant(value, Type, claimType)).ToArray();
        return ValueTask.FromResult(new PermissionGrantResult(grants, []));
    }
}
