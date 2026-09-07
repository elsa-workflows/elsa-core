using System.Security.Claims;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Permissions;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Services;

public sealed class DefaultClaimsIdentityResolver(IOptions<UserTasksOptions> options) : IUserTaskIdentityResolver
{
    /// <summary>An ASP.NET role, not a permission, despite reading like one. Kept for hosts that grant it.</summary>
    private const string ManagerRole = "user-tasks:manager";

    private readonly UserTasksOptions _options = options.Value;

    public ValueTask<UserTaskActor?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var subjectId = principal.FindFirstValue(_options.SubjectClaimType)
                        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subjectId))
            return ValueTask.FromResult<UserTaskActor?>(null);

        var tenantId = principal.FindFirstValue(_options.TenantClaimType) ?? _options.DefaultTenantId;
        var provider = principal.FindFirstValue(_options.ProviderClaimType) ?? _options.DefaultProvider;
        var subject = new ParticipantReference(tenantId, provider, UserTaskParticipantType.User, subjectId,
            principal.FindFirstValue(_options.DisplayNameClaimType) ?? principal.Identity?.Name);
        var groups = _options.GroupClaimTypes
            .SelectMany(type => principal.FindAll(type))
            .Select(x => x.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Select(id => new ParticipantReference(tenantId, provider, UserTaskParticipantType.Group, id))
            .ToArray();
        // Ordinal, matching the permission model everywhere else: folding case here would collapse two
        // spellings into whichever arrived first, and the survivor might be the one that no longer matches.
        var permissions = _options.PermissionClaimTypes
            .SelectMany(type => principal.FindAll(type))
            .Select(x => x.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        var actor = new UserTaskActor(subject, groups, subject.DisplayName) { Permissions = permissions };

        return ValueTask.FromResult<UserTaskActor?>(actor with
        {
            // Asked of the actor rather than of the raw strings, so a subtree or verb wildcard confers
            // manager standing here exactly as it does at every other check.
            IsManager = principal.IsInRole(ManagerRole)
                        || actor.HasPermission(UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Supervise)
        });
    }
}
