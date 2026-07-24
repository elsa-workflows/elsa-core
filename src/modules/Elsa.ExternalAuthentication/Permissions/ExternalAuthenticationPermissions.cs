using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Permissions;

/// <summary>Stable permission names and optional Studio descriptor metadata for External Authentication administration.</summary>
public static class ExternalAuthenticationPermissions
{
    public const string ConnectionsRead = "external-authentication:connections:read";
    public const string ConnectionsCreate = "external-authentication:connections:create";
    public const string ConnectionsUpdate = "external-authentication:connections:update";
    public const string ConnectionsArchive = "external-authentication:connections:archive";
    public const string ConnectionsTest = "external-authentication:connections:test";
    public const string ConnectionsPreview = "external-authentication:connections:preview";
    public const string PoliciesManage = "external-authentication:policies:manage";
    public const string ProviderTrustUnsafe = "external-authentication:provider-trust:unsafe";
    public const string PermissionsDelegate = "external-authentication:permissions:delegate";
    public const string PermissionsDelegateUnrestricted = "external-authentication:permissions:delegate-unrestricted";
    public const string LinksManage = "external-authentication:links:manage";
    public const string SessionsRead = "external-authentication:sessions:read";
    public const string SessionsRevoke = "external-authentication:sessions:revoke";

    public static IReadOnlyCollection<PermissionDescriptor> Descriptors { get; } =
    [
        new(ConnectionsRead, "View identity provider connections", "View connection configuration and safe operational status.", "External Authentication"),
        new(ConnectionsCreate, "Create identity provider connections", "Create database-owned identity provider connection drafts.", "External Authentication"),
        new(ConnectionsUpdate, "Update identity provider connections", "Update settings and Secret Bindings, and enable or disable database-owned connections.", "External Authentication"),
        new(ConnectionsArchive, "Archive identity provider connections", "Archive and restore database-owned identity provider connections.", "External Authentication"),
        new(ConnectionsTest, "Test identity provider connections", "Run on-demand provider connection tests.", "External Authentication"),
        new(ConnectionsPreview, "Preview identity provider sign-in", "Run a redacted, non-mutating sign-in preview.", "External Authentication"),
        new(PoliciesManage, "Manage external authentication policies", "Configure unlinked identity policies and permission grant-source selections.", "External Authentication"),
        new(ProviderTrustUnsafe, "Use privileged authentication overrides", "Confirm unsafe provider trust settings or a final-login-path recovery override.", "External Authentication"),
        new(PermissionsDelegate, "Delegate external permissions", "Configure mappings for permissions the actor may delegate.", "External Authentication"),
        new(PermissionsDelegateUnrestricted, "Delegate unrestricted external permissions", "Configure permission mappings without possessing every delegated permission.", "External Authentication"),
        new(LinksManage, "Manage external identity links", "Search tenant users and prelink or unlink external identities.", "External Authentication"),
        new(SessionsRead, "View external authentication sessions", "View safe external authentication session metadata.", "External Authentication"),
        new(SessionsRevoke, "Revoke external authentication sessions", "Revoke an external authentication session and its refresh credentials.", "External Authentication")
    ];
}
