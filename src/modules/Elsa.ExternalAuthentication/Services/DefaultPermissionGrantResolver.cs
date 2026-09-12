using System.Security.Claims;
using Elsa.Authorization;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.Permissions;
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
        var boundary = new PermissionGrantBoundary(options.Value.PermissionGrants);

        foreach (var selection in context.Connection.Connection.PermissionGrantSources.OrderBy(x => x.Order).ThenBy(x => x.Type, StringComparer.Ordinal))
        {
            if (!IsAllowedSource(selection.Type) || !_sources.TryGetValue(selection.Type, out var source))
            {
                AddWarning(warnings, warningKeys, new("permission_grant_source_unavailable", $"The permission grant source '{selection.Type}' is not available."));
                continue;
            }

            var result = await source.GetGrantsAsync(new(context.TargetTenantId, context.UserId, context.Connection, context.Identity, context.ProjectedClaims, selection), cancellationToken);
            foreach (var warning in result.Warnings)
                AddWarning(warnings, warningKeys, warning);
            foreach (var grant in result.Grants)
            {
                // A value the evaluator cannot parse authorizes nothing, so carrying it into a token only
                // hides the mistake. Say so rather than passing it through as an opaque string.
                if (!Permission.TryParse(grant.Permission, out _))
                {
                    AddWarning(warnings, warningKeys, new("malformed_permission", $"The permission '{grant.Permission}' is not well-formed and was dropped."));
                    continue;
                }

                if (!boundary.Allows(grant.Permission))
                {
                    AddWarning(warnings, warningKeys, new("permission_denied_by_deployment", $"The permission '{grant.Permission}' is outside the deployment grant boundary."));
                    continue;
                }

                // Checked against the core catalog, which is keyed by resource and lists the verbs each one
                // accepts. The module used to keep its own registry, fed only its legacy permission names, so
                // once the vocabulary changed every grant looked unknown and the warning became constant noise.
                if (!IsAdvertised(descriptors, grant.Permission))
                    AddWarning(warnings, warningKeys, new("unknown_permission_descriptor", $"No module advertises a descriptor for permission '{grant.Permission}'."));

                if (grants.All(x => !string.Equals(x.Permission, grant.Permission, StringComparison.Ordinal)))
                    grants.Add(grant);
            }
        }

        return new(grants, warnings);
    }

    /// <summary>
    /// Whether some module advertises <paramref name="permission"/>. A wildcard is advertised by definition:
    /// it names a pattern rather than one resource, so there is no single descriptor to look it up in.
    /// </summary>
    private static bool IsAdvertised(IPermissionDescriptorRegistry descriptors, string permission)
    {
        if (!Permission.TryParse(permission, out var parsed))
            return false;

        if (parsed.HasWildcard)
            return true;

        return descriptors.Find(parsed.Resource)?.Supports(parsed.Verb) == true;
    }

    private bool IsAllowedSource(string type) => options.Value.AllowedPermissionGrantSourceTypes.Count == 0 || options.Value.AllowedPermissionGrantSourceTypes.Contains(type, StringComparer.Ordinal);
    private static void AddWarning(ICollection<PermissionGrantWarning> warnings, ISet<(string Code, string Message)> keys, PermissionGrantWarning warning)
    {
        if (keys.Add((warning.Code, warning.Message)))
            warnings.Add(warning);
    }
}

public sealed class DefaultPermissionDelegationAuthorizer(IOptions<ExternalAuthenticationOptions> options, IPermissionEvaluator permissionEvaluator) : IPermissionDelegationAuthorizer
{
    public ValueTask<PermissionDelegationResult> AuthorizeAsync(ClaimsPrincipal actor, IReadOnlyCollection<GrantSourceSelection> selections, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var boundary = new PermissionGrantBoundary(options.Value.PermissionGrants);
        var configuredPermissions = selections.SelectMany(x => PermissionGrantMappingSettings.Read(x.Settings)).SelectMany(x => x.Permissions)
            .Concat(selections.Where(x => string.Equals(x.Type, ClaimPassThroughPermissionGrantSource.SourceType, StringComparison.Ordinal)).SelectMany(x => PermissionGrantMappingSettings.ReadPassThroughPermissions(x.Settings)))
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var unrestricted = permissionEvaluator.HasPermission(actor, ExternalAuthenticationResourcePermissions.PermissionGrants, ExternalAuthenticationVerbs.DelegateUnrestricted);
        var mayDelegate = unrestricted || permissionEvaluator.HasPermission(actor, ExternalAuthenticationResourcePermissions.PermissionGrants, ExternalAuthenticationVerbs.Delegate);
        var unauthorized = configuredPermissions.Where(permission => !boundary.Allows(permission) || !mayDelegate || (!unrestricted && !permissionEvaluator.HasPermission(actor, permission))).ToArray();
        return ValueTask.FromResult(new PermissionDelegationResult(unauthorized.Length == 0, unauthorized));
    }
}

/// <summary>
/// The deployment's allow and deny boundary on delegated permissions. Both lists are permission patterns,
/// so they read the same way a role does: <c>workflows/*:delete</c> denies every delete beneath
/// <c>workflows</c>, not just a permission spelled exactly that way.
/// </summary>
internal sealed class PermissionGrantBoundary
{
    private readonly IReadOnlyCollection<Permission> _allowed;
    private readonly IReadOnlyCollection<Permission> _denied;
    private readonly bool _isUnusable;

    public PermissionGrantBoundary(PermissionGrantOptions options)
    {
        _allowed = Parse(options.AllowedPermissions);
        _denied = Parse(options.DeniedPermissions);

        // A boundary the deployment configured but that does not parse is a configuration error, and the only
        // safe reading of one is "allow nothing". Dropping the unparseable entries and carrying on would turn
        // a typo in the allow list into no allow list at all -- an empty allow list means unrestricted -- and
        // a typo in the deny list into a silent un-denying of whatever it named. ExternalAuthenticationOptionsValidator
        // rejects this at startup, so reaching it here means that validation was bypassed rather than that an
        // operator is mid-edit.
        _isUnusable = _allowed.Count != options.AllowedPermissions.Count || _denied.Count != options.DeniedPermissions.Count;
    }

    public bool Allows(string permission)
    {
        if (_isUnusable || !Permission.TryParse(permission, out var candidate))
            return false;

        // Deny wins, and it is tested in both directions: a grant beneath a denied subtree is denied, and a
        // wildcard grant that would reach a denied permission is denied too. Testing only one direction
        // would let 'workflows/*:delete' hand out a delete the deployment denied by name.
        if (_denied.Any(x => PermissionMatcher.Satisfies(x, candidate) || PermissionMatcher.Satisfies(candidate, x)))
            return false;

        // Allow is one-directional on purpose. An allow entry must cover the whole grant, so a grant broader
        // than anything allowed is refused rather than admitted for the part that overlaps.
        return _allowed.Count == 0 || _allowed.Any(x => PermissionMatcher.Satisfies(x, candidate));
    }

    private static IReadOnlyCollection<Permission> Parse(IEnumerable<string> values) =>
        values.Select(x => Permission.TryParse(x, out var permission) ? permission : (Permission?)null).OfType<Permission>().ToArray();
}
