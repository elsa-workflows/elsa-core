using System.Security.Claims;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Services;

public sealed class DefaultClaimsIdentityResolver(IOptions<UserTasksOptions> options) : IUserTaskIdentityResolver
{
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
        var permissions = _options.PermissionClaimTypes
            .SelectMany(type => principal.FindAll(type))
            .Select(x => x.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return ValueTask.FromResult<UserTaskActor?>(new UserTaskActor(subject, groups, subject.DisplayName)
        {
            IsManager = principal.IsInRole("user-tasks:manager")
                        || permissions.Contains("manage:user-tasks"),
            Permissions = permissions
        });
    }
}
