using Elsa.Identity.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IdentityFeature"/>.
/// </summary>
public static class IdentityFeatureExtensions
{
    /// <summary>
    /// Configures the default admin user.
    /// </summary>
    public static IdentityFeature UseDefaultAdmin(this IdentityFeature feature, Action<DefaultAdminUserFeature>? configure = null)
    {
        feature.Module.Use(configure);
        return feature;
    }
    
    /// <summary>
    /// Configures the default admin user.
    /// </summary>
    public static IdentityFeature UseDefaultAdmin(this IdentityFeature feature, string username, string password, string roleName, params ICollection<string>[] permissions)
    {
        return feature.UseDefaultAdmin(admin => admin
            .WithAdminUserName(username)
            .WithAdminPassword(password)
            .WithAdminRoleName(roleName)
            .WithAdminRolePermissions(permissions.SelectMany(x => x).ToList()));
    }
}