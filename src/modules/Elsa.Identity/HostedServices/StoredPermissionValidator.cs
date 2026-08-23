using Elsa.Authorization;
using Elsa.Identity.Contracts;
using Elsa.Permissions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Identity.HostedServices;

/// <summary>
/// Reports stored role permissions that no longer resolve, so an upgrade fails loudly rather than
/// silently narrowing roles.
/// </summary>
/// <remarks>
/// The authorization model deliberately breaks legacy permission strings rather than carrying a permanent
/// alias layer, which would keep two vocabularies valid forever. This makes the consequence visible: every
/// unresolvable permission is logged against the role that holds it. The whole-vocabulary grant survives
/// unchanged, so an administrator cannot be locked out while the rest is re-authored.
/// </remarks>
[UsedImplicitly]
public class StoredPermissionValidator(IServiceScopeFactory scopeFactory, ILogger<StoredPermissionValidator> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var roleProvider = scope.ServiceProvider.GetRequiredService<IRoleProvider>();
        var registry = scope.ServiceProvider.GetRequiredService<IPermissionDescriptorRegistry>();

        IReadOnlyCollection<string> Unresolvable(IEnumerable<string> permissions) =>
            permissions.Where(x => !Resolves(registry, x)).ToArray();

        try
        {
            var roles = await roleProvider.FindManyAsync(new(), cancellationToken);
            var affected = 0;

            foreach (var role in roles)
            {
                var unresolvable = Unresolvable(role.Permissions);

                if (unresolvable.Count == 0)
                    continue;

                affected++;
                logger.LogWarning(
                    "Role '{RoleName}' ({RoleId}) holds {Count} permission(s) that no longer resolve and will not authorize: {Permissions}. See docs/migrations/authorization-model.md.",
                    role.Name, role.Id, unresolvable.Count, string.Join(", ", unresolvable));
            }

            if (affected > 0)
                logger.LogWarning("{Count} role(s) hold unresolvable permissions. Re-author them against the permission catalog at GET /identity/permissions.", affected);
        }
        catch (Exception ex)
        {
            // Reporting must never prevent the host from starting.
            logger.LogWarning(ex, "Could not validate stored role permissions.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool Resolves(IPermissionDescriptorRegistry registry, string value)
    {
        if (!Permission.TryParse(value, out var permission))
            return false;

        if (permission.IsResourceWildcard || permission.IsSubtree)
            return true;

        var descriptor = registry.Find(permission.Resource);

        return descriptor is not null && (permission.IsVerbWildcard || descriptor.Supports(permission.Verb));
    }
}
