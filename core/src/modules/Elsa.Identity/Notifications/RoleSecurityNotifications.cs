using Elsa.Mediator.Contracts;

namespace Elsa.Identity.Notifications;

/// <summary>
/// Safe context shared by role and assignment security notifications. It deliberately carries no secret,
/// credential, or token: these are published for audit subscribers, and this module owns no audit store.
/// </summary>
/// <param name="ActorId">Who performed the change, where the caller is identifiable.</param>
/// <param name="TenantId">The tenant the change applies to.</param>
public sealed record RoleSecurityEventContext(
    string? ActorId,
    string? TenantId,
    DateTimeOffset OccurredAt,
    string Summary);

/// <summary>Published when a role is created, updated, or deleted.</summary>
/// <param name="Operation">One of <c>created</c>, <c>updated</c>, or <c>deleted</c>.</param>
/// <param name="Permissions">
/// The resulting grants, so a reviewer can reconstruct what a role conferred at a point in time without
/// replaying every prior event.
/// </param>
public sealed record RoleChanged(
    RoleSecurityEventContext Context,
    string Operation,
    string RoleId,
    string RoleName,
    IReadOnlyCollection<string> Permissions) : INotification;

/// <summary>Published when roles are assigned to or removed from a user.</summary>
/// <param name="Assigned">Role identifiers added by this change.</param>
/// <param name="Removed">Role identifiers withdrawn by this change.</param>
public sealed record UserRoleAssignmentChanged(
    RoleSecurityEventContext Context,
    string UserId,
    IReadOnlyCollection<string> Assigned,
    IReadOnlyCollection<string> Removed) : INotification;
