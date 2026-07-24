using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

public sealed class DefaultPermissionGrantResolver(
    IEnumerable<IPermissionGrantSource> sources,
    IPermissionDescriptorRegistry descriptors,
    IOptions<ExternalAuthenticationOptions> options) : IPermissionGrantResolver
{
    private readonly IReadOnlyDictionary<string, IPermissionGrantSource> _sources = sources.ToDictionary(x => x.Type, StringComparer.Ordinal);

    public async ValueTask<PermissionGrantResult> ResolveAsync(PermissionGrantResolutionContext context, CancellationToken cancellationToken = default)
    {
        var grants = new List<PermissionGrant>();
        var warnings = new List<PermissionGrantWarning>();
        var warningKeys = new HashSet<(string Code, string Message)>();
        var knownPermissions = descriptors.List().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        var boundary = new PermissionGrantBoundary(options.Value.PermissionGrants);

        foreach (var selection in context.Connection.Connection.PermissionGrantSources.OrderBy(x => x.Order).ThenBy(x => x.Type, StringComparer.Ordinal))
        {
            if (!IsAllowedSource(selection.Type) || !_sources.TryGetValue(selection.Type, out var source))
            {
                AddWarning(warnings, warningKeys, new PermissionGrantWarning("permission_grant_source_unavailable", $"The permission grant source '{selection.Type}' is not available."));
                continue;
            }

            var result = await source.GetGrantsAsync(new PermissionGrantContext(context.TargetTenantId, context.UserId, context.Connection, context.Identity, context.ProjectedClaims, selection), cancellationToken);
            foreach (var warning in result.Warnings)
                AddWarning(warnings, warningKeys, warning);
            foreach (var grant in result.Grants)
            {
                if (!boundary.Allows(grant.Permission))
                {
                    AddWarning(warnings, warningKeys, new PermissionGrantWarning("permission_denied_by_deployment", $"The permission '{grant.Permission}' is outside the deployment grant boundary."));
                    continue;
                }

                if (!knownPermissions.Contains(grant.Permission))
                    AddWarning(warnings, warningKeys, new PermissionGrantWarning("unknown_permission_descriptor", $"No module advertises a descriptor for permission '{grant.Permission}'."));

                if (grants.All(x => !string.Equals(x.Permission, grant.Permission, StringComparison.Ordinal)))
                    grants.Add(grant);
            }
        }

        return new PermissionGrantResult(grants, warnings);
    }

    private bool IsAllowedSource(string type) => options.Value.AllowedPermissionGrantSourceTypes.Count == 0 || options.Value.AllowedPermissionGrantSourceTypes.Contains(type, StringComparer.Ordinal);
    private static void AddWarning(ICollection<PermissionGrantWarning> warnings, ISet<(string Code, string Message)> keys, PermissionGrantWarning warning)
    {
        if (keys.Add((warning.Code, warning.Message)))
            warnings.Add(warning);
    }
}

public sealed class DefaultPermissionDelegationAuthorizer(IOptions<ExternalAuthenticationOptions> options) : IPermissionDelegationAuthorizer
{
    public ValueTask<PermissionDelegationResult> AuthorizeAsync(System.Security.Claims.ClaimsPrincipal actor, IReadOnlyCollection<GrantSourceSelection> selections, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var actorPermissions = actor.FindAll(PermissionNames.ClaimType).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
        var boundary = new PermissionGrantBoundary(options.Value.PermissionGrants);
        var configuredPermissions = selections.SelectMany(x => PermissionGrantMappingSettings.Read(x.Settings)).SelectMany(x => x.Permissions)
            .Concat(selections.Where(x => string.Equals(x.Type, ClaimPassThroughPermissionGrantSource.SourceType, StringComparison.Ordinal)).SelectMany(x => PermissionGrantMappingSettings.ReadPassThroughPermissions(x.Settings)))
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var unrestricted = actorPermissions.Contains(PermissionNames.All) || actorPermissions.Contains(Permissions.ExternalAuthenticationPermissions.PermissionsDelegateUnrestricted);
        var mayDelegate = unrestricted || actorPermissions.Contains(Permissions.ExternalAuthenticationPermissions.PermissionsDelegate);
        var unauthorized = configuredPermissions.Where(permission => !boundary.Allows(permission) || !mayDelegate || (!unrestricted && !actorPermissions.Contains(permission))).ToArray();
        return ValueTask.FromResult(new PermissionDelegationResult(unauthorized.Length == 0, unauthorized));
    }
}

internal sealed class PermissionGrantBoundary(PermissionGrantOptions options)
{
    private readonly IReadOnlySet<string> _allowed = options.AllowedPermissions.ToHashSet(StringComparer.Ordinal);
    private readonly IReadOnlySet<string> _denied = options.DeniedPermissions.ToHashSet(StringComparer.Ordinal);
    public bool Allows(string permission) => !string.IsNullOrWhiteSpace(permission) && !_denied.Contains(permission) && (_allowed.Count == 0 || _allowed.Contains(permission));
}
