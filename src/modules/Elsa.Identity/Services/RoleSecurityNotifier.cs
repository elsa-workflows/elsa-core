using System.Security.Claims;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Identity.Services;

/// <summary>Publishes role and assignment changes for audit subscribers.</summary>
/// <remarks>
/// Follows ADR 0007: typed, redacted notifications over the mediator, with no audit persistence owned
/// here. A future audit module subscribes, stores, and sets its own retention policy without coupling
/// identity administration to an audit database.
/// </remarks>
[UsedImplicitly]
public class RoleSecurityNotifier(INotificationSender notificationSender, ITenantAccessor tenantAccessor, ISystemClock clock)
{
    /// <summary>Publishes a role creation, update, or deletion.</summary>
    public Task RoleChangedAsync(ClaimsPrincipal? actor, string operation, string roleId, string roleName, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default) =>
        notificationSender.SendAsync(
            new RoleChanged(Context(actor, $"Role '{roleName}' {operation}."), operation, roleId, roleName, permissions),
            cancellationToken);

    /// <summary>Publishes a change to the roles a user holds.</summary>
    public Task UserRolesChangedAsync(ClaimsPrincipal? actor, string userId, IReadOnlyCollection<string> assigned, IReadOnlyCollection<string> removed, CancellationToken cancellationToken = default) =>
        notificationSender.SendAsync(
            new UserRoleAssignmentChanged(Context(actor, $"Roles changed for user '{userId}': {assigned.Count} assigned, {removed.Count} removed."), userId, assigned, removed),
            cancellationToken);

    private RoleSecurityEventContext Context(ClaimsPrincipal? actor, string summary) =>
        new(actor?.Identity?.Name, tenantAccessor.TenantId, clock.UtcNow, summary);
}
