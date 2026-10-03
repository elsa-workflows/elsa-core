using Elsa.Studio.Authorization;

namespace Elsa.Studio.Testing;

/// <summary>Returns fixed permissions and counts how often they were requested.</summary>
internal sealed class StubPermissionService(UserPermissions permissions) : IPermissionService
{
    public UserPermissions Permissions { get; set; } = permissions;

    public StubPermissionService(params string[] grants) : this(Grants(grants))
    {
    }

    public int Calls { get; private set; }

    public ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return new(Permissions);
    }

    public static UserPermissions Grants(params string[] grants) => UserPermissions.FromGrants(grants.Select(Parse));

    private static Permission Parse(string value) => Permission.TryParse(value, out var permission) ? permission : throw new FormatException(value);
}
