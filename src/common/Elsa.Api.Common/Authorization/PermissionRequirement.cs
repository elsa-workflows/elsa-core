using Microsoft.AspNetCore.Authorization;

namespace Elsa.Authorization;

/// <summary>Requires the calling principal to hold a permission satisfying <see cref="Permission"/>.</summary>
public sealed class PermissionRequirement(Permission permission) : IAuthorizationRequirement
{
    /// <summary>The permission the endpoint requires.</summary>
    public Permission Permission { get; } = permission;

    /// <summary>The policy name carrying <paramref name="permission"/>, used to register one policy per requirement.</summary>
    public static string PolicyName(Permission permission) => $"elsa:permission:{permission}";
}
