using Elsa.Common;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.HostedServices;

/// <summary>
/// Hosted service that initializes the admin user and role from <see cref="DefaultAdminUserOptions"/> configuration if provided.
/// </summary>
/// <remarks>
/// Runs once per tenant activation. The admin role is resolved within the current tenant and, when missing, is
/// created with a generated ID that the admin user then references, so tenants that share one store each get
/// their own admin role and user.
/// </remarks>
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
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var adminUserName = options.Value.AdminUserName;
        var adminPassword = options.Value.AdminPassword;
        var adminRoleName = options.Value.AdminRoleName;
        var adminRolePermissions = options.Value.AdminRolePermissions;
        if (string.IsNullOrWhiteSpace(adminRoleName))
        {
            logger.LogWarning("AdminRoleName is not configured. Skipping admin role and user creation.");
            return;
        }

        var existingRole = await FindAdminRoleAsync(adminRoleName, cancellationToken);
        string roleToAssign;

        if (existingRole == null)
        {
            // No ID is passed, so the role gets a generated one. Role IDs are unique across the whole store,
            // and an ID taken from AdminRoleName could already belong to another tenant sharing this store.
            var roleResult = await roleManager.CreateRoleAsync(
                adminRoleName,
                adminRolePermissions.ToList(),
                cancellationToken: cancellationToken);

            roleToAssign = roleResult.Role.Id;
            logger.LogInformation("Admin role '{RoleName}' created successfully with {PermissionCount} permissions.",
                roleResult.Role.Name,
                roleResult.Role.Permissions.Count);
        }
        else
        {
            roleToAssign = existingRole.Id;
            var missingPermissions = adminRolePermissions.Except(existingRole.Permissions, StringComparer.Ordinal).ToArray();

            if (missingPermissions.Length == 0)
            {
                logger.LogInformation("Admin role '{RoleName}' already exists with all configured permissions.", adminRoleName);
            }
            else
            {
                existingRole.Permissions = existingRole.Permissions.Concat(missingPermissions).ToList();
                await roleStore.SaveAsync(existingRole, cancellationToken);
                logger.LogInformation(
                    "Admin role '{RoleName}' updated successfully with {PermissionCount} missing configured permissions.",
                    adminRoleName,
                    missingPermissions.Length);
            }
        }

        // Create user if configured
        if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("AdminUserName and/or AdminPassword not configured in DefaultAdminUserOptions. Skipping admin user creation.");
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

        logger.LogInformation("Admin user '{Name}' created successfully with role '{Role}' ({RoleId}).", result.User.Name, adminRoleName, roleToAssign);
    }

    /// <summary>
    /// Finds the current tenant's admin role. A role whose ID is <paramref name="adminRoleName"/> is what earlier
    /// versions created, and existing users reference that ID, so it is preferred even if it has since been renamed.
    /// Otherwise the role is looked up by name within the tenant. Both lookups only see roles visible to the
    /// current tenant, so a role another tenant owns is never reused, and tenant-agnostic roles are skipped: the
    /// initializer adds permissions to the role it finds, which must not widen a role that every tenant shares.
    /// </summary>
    private async Task<Role?> FindAdminRoleAsync(string adminRoleName, CancellationToken cancellationToken)
    {
        var legacyRole = await roleStore.FindAsync(new() { Id = adminRoleName }, cancellationToken);
        if (legacyRole != null && !RoleStoreExtensions.IsTenantAgnostic(legacyRole))
        {
            return legacyRole;
        }

        return await roleStore.FindByNameAsync(adminRoleName, includeTenantAgnostic: false, cancellationToken);
    }
}
