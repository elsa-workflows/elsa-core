using System.Security.Claims;

namespace Elsa.ServerLogs.Permissions;

public static class ServerLogPermissions
{
    public const string ReadAll = "read:*";
    public const string Read = "read:server-logs";

    public static bool CanRead(ClaimsPrincipal user)
    {
        return user.HasClaim("permissions", PermissionNames.All) ||
               user.HasClaim("permissions", ReadAll) ||
               user.HasClaim("permissions", Read);
    }
}
