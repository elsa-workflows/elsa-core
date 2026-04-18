using CShells.Features;
using Elsa.Extensions;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Identity.ShellFeatures;

/// <summary>
/// Feature that initializes an admin user from configuration if provided.
/// </summary>
[ShellFeature(
    DisplayName = "Default Admin User Initialization",
    Description = "Initializes a default admin user from configuration if provided",
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class DefaultAdminUserFeature : IShellFeature
{
    /// <summary>
    /// Gets or sets the admin user name. Must be explicitly configured to enable user bootstrap.
    /// </summary>
    public string AdminUserName { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the admin user password. Must be explicitly configured to enable user bootstrap.
    /// </summary>
    public string AdminPassword { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the admin role name.
    /// </summary>
    public string AdminRoleName { get; set; } = "admin";
    
    /// <summary>
    /// Gets or sets the admin role permissions.
    /// </summary>
    public ICollection<string> AdminRolePermissions { get; set; } = ["*"];
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<DefaultAdminUserOptions>(options =>
        {
            options.AdminUserName = AdminUserName;
            options.AdminPassword = AdminPassword;
            options.AdminRoleName = AdminRoleName;
            options.AdminRolePermissions = AdminRolePermissions;
        });
        services.AddBackgroundTask<AdminUserInitializer>();
    }
}
