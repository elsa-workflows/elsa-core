using Elsa.Common;
using Elsa.Identity.Contracts;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.HostedServices;

/// <summary>
/// Hosted service that initializes the admin user and role from environment variables if configured.
/// </summary>
[UsedImplicitly]
public class AdminUserInitializer(
    IUserStore userStore,
    IRoleStore roleStore,
    IUserManager userManager,
    IRoleManager roleManager,
    IOptions<DefaultAdminUserOptions> options,
    ILogger<AdminUserInitializer> logger)
    : BackgroundTask
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var adminUserName = options.Value.AdminUserName;
        var adminPassword = options.Value.AdminPassword;
        var adminRoleName = options.Value.AdminRoleName;
        var adminRolePermissions = options.Value.AdminRolePermissions;
        var existingRole = await roleStore.FindAsync(new() { Id = adminRoleName }, cancellationToken);

        if (existingRole == null)
        {
            var roleResult = await roleManager.CreateRoleAsync(
                adminRoleName,
                adminRolePermissions.ToList(),
                adminRoleName,
                cancellationToken);

            logger.LogInformation("Admin role '{RoleName}' created successfully with {PermissionCount} permissions.",
                roleResult.Role.Name,
                roleResult.Role.Permissions.Count);
        }
        else
        {
            logger.LogInformation("Admin role '{RoleName}' already exists. Skipping creation.", adminRoleName);
        }
        
        var roleToAssign = adminRoleName;

        // Create user if configured
        if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("ELSA_ADMIN_USER and/or ELSA_ADMIN_PASSWORD not configured. Skipping admin user creation.");
            return;
        }

        // Check if user already exists
        var existingUser = await userStore.FindAsync(new() { Name = adminUserName }, cancellationToken);

        if (existingUser != null)
        {
            logger.LogInformation("Admin user '{User}' already exists. Skipping creation.", adminUserName);
            return;
        }

        // Create the admin user
        var result = await userManager.CreateUserAsync(
            adminUserName,
            adminPassword,
            new List<string> { roleToAssign },
            cancellationToken);

        logger.LogInformation("Admin user '{Name}' created successfully with role '{Role}'.", result.User.Name, roleToAssign);
    }
}
