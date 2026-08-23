using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.ExternalAuthentication.Permissions;

/// <summary>
/// Stable resource names for External Authentication. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class ExternalAuthenticationResourcePermissions
{
    /// <summary>Manage connections to external identity providers. Archive is reversible and preserves links; there is no hard delete.</summary>
    public const string Connections = "external-authentication/connections";
    /// <summary>Browse installed adapters, policies, permission sources, user matchers, and secret resolvers.</summary>
    public const string Descriptors = "external-authentication/descriptors";
    /// <summary>Search users and link, relink, or unlink external identities.</summary>
    public const string IdentityLinks = "external-authentication/identity-links";
    /// <summary>Inspect and revoke external authentication sessions.</summary>
    public const string Sessions = "external-authentication/sessions";
    /// <summary>Configure how unknown external identities are admitted.</summary>
    public const string Policies = "external-authentication/policies";
    /// <summary>Choose the roles granted to a user created for an unknown external identity.</summary>
    public const string PolicyDefaultRoles = "external-authentication/policies/default-roles";
    /// <summary>Confirm an unsafe provider trust setting or a final-login-path recovery override.</summary>
    public const string ProviderTrust = "external-authentication/provider-trust";
    /// <summary>Configure which Elsa permissions an external claim mapping may confer. The unrestricted verb lifts the requirement that the actor already holds them.</summary>
    public const string PermissionGrants = "external-authentication/permission-grants";
}

/// <summary>Contributes the External Authentication resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class ExternalAuthenticationResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(ExternalAuthenticationResourcePermissions.Connections, [CoreVerbs.View, CoreVerbs.Create, CoreVerbs.Update, "archive", "test", "preview"], "Identity provider connections", "Manage connections to external identity providers. Archive is reversible and preserves links; there is no hard delete.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.Descriptors, [CoreVerbs.View], "External authentication descriptors", "Browse installed adapters, policies, permission sources, user matchers, and secret resolvers.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.IdentityLinks, [CoreVerbs.View, CoreVerbs.Write, CoreVerbs.Delete], "External identity links", "Search users and link, relink, or unlink external identities.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.Sessions, [CoreVerbs.View, "revoke"], "External authentication sessions", "Inspect and revoke external authentication sessions.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.Policies, [CoreVerbs.View, CoreVerbs.Update], "Unlinked identity policies", "Configure how unknown external identities are admitted.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.PolicyDefaultRoles, [CoreVerbs.Update], "Policy default roles", "Choose the roles granted to a user created for an unknown external identity.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.ProviderTrust, ["override"], "Provider trust overrides", "Confirm an unsafe provider trust setting or a final-login-path recovery override.", "External Authentication"),
        new(ExternalAuthenticationResourcePermissions.PermissionGrants, ["delegate", "delegate-unrestricted"], "External permission grants", "Configure which Elsa permissions an external claim mapping may confer. The unrestricted verb lifts the requirement that the actor already holds them.", "External Authentication"),
    ];
}
