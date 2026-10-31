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
                    "Role '{RoleName}' ({RoleId}) holds {Count} permission(s) that no longer resolve and will not authorize: {Permissions}. See doc/migrations/authorization-model.md.",
                    role.Name, role.Id, unresolvable.Count, string.Join(", ", unresolvable));
            }

            if (affected > 0)
                logger.LogWarning("{Count} role(s) hold unresolvable permissions. Re-author them against the permission catalog at GET /identity/permissions.", affected);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Reporting must never prevent the host from starting: an unreachable or half-migrated store
            // is exactly when an operator most needs the host up to fix it.
            logger.LogWarning(ex, "Could not validate stored role permissions.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool Resolves(IPermissionDescriptorRegistry registry, string value)
    {
        if (!Permission.TryParse(value, out var permission) || !permission.IsValidPattern)
            return false;

        if (permission.IsResourceWildcard)
            return true;

        // A subtree grant reaching nothing is far more likely a typo ('workflow/*') than a grant for a
        // module yet to be installed, so it is reported rather than assumed forward-reaching.
        if (permission.IsSubtree)
        {
            var reached = registry.Reach(permission.Resource);

            // A concrete verb is only resolved when something under the subtree actually supports it:
            // 'workflows/*:frobnicate' reaches plenty and authorizes nothing, which is the same inert
            // grant an unreachable subtree is, and deserves the same warning.
            return permission.IsVerbWildcard
                ? reached.Count > 0
                : reached.Any(x => registry.Find(x)?.Supports(permission.Verb) == true);
        }

        var descriptor = registry.Find(permission.Resource);

        return descriptor is not null && (permission.IsVerbWildcard || descriptor.Supports(permission.Verb));
    }
}
