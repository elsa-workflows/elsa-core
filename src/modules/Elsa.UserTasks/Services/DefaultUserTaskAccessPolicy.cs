using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Permissions;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskAccessPolicy : IUserTaskAccessPolicy
{
    /// <summary>
    /// Guests authenticate through a task-scoped invitation session. They may read and complete exactly one
    /// task and can never claim, release, reassign, schedule, invite, cancel, or manage.
    /// </summary>
    private static readonly UserTaskAccessOperation[] GuestOperations =
    [
        UserTaskAccessOperation.ReadSummary,
        UserTaskAccessOperation.ReadProtected,
        UserTaskAccessOperation.Complete
    ];

    public Task<UserTaskQueryScope?> CreateScopeAsync(UserTaskActor actor, UserTaskQueryScopeKind kind, CancellationToken cancellationToken = default)
    {
        // A guest session is issued for one task and carries no list capability at all.
        if (actor.IsGuest || !actor.HasPermission(UserTasksPermissions.Read))
            return Task.FromResult<UserTaskQueryScope?>(null);

        var isManager = IsManager(actor);
        if (!isManager && kind is UserTaskQueryScopeKind.All or UserTaskQueryScopeKind.NeedsAttention)
            return Task.FromResult<UserTaskQueryScope?>(null);

        return Task.FromResult<UserTaskQueryScope?>(new UserTaskQueryScope(
            actor.Subject.TenantId,
            actor.Subject,
            actor.Groups,
            isManager,
            kind,
            ExcludeBlocking: !isManager));
    }

    public Task<bool> AuthorizeAsync(UserTask task, UserTaskActor actor, UserTaskAccessOperation operation, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(task.TenantId, actor.Subject.TenantId, StringComparison.Ordinal))
            return Task.FromResult(false);
        if (!actor.HasPermission(RequiredPermission(operation)))
            return Task.FromResult(false);

        if (actor.IsGuest)
            return Task.FromResult(AuthorizeGuest(task, actor, operation));

        var isManager = IsManager(actor);
        if (task.HealthSeverity == UserTaskHealthSeverity.Blocking && !isManager)
            return Task.FromResult(false);

        var isAssignee = task.Assignee?.Matches(actor.Subject) == true;
        var isCandidate = IsCandidate(task, actor);
        var isCompleter = task.CompletedBy?.Matches(actor.Subject) == true;
        var acted = task.Events.Any(x => x.Actor?.Matches(actor.Subject) == true);

        var result = operation switch
        {
            UserTaskAccessOperation.ReadSummary => isManager || isAssignee || isCandidate || isCompleter || acted,
            UserTaskAccessOperation.ReadProtected => isManager || (task.IsTerminal ? isCompleter : isAssignee),
            UserTaskAccessOperation.Claim => !task.IsTerminal && task.Status is (UserTaskStatus.Available or UserTaskStatus.Unassigned) && isCandidate && !IsExcluded(task, actor),
            UserTaskAccessOperation.Release => !task.IsTerminal && isAssignee,
            UserTaskAccessOperation.Assign => isManager,
            UserTaskAccessOperation.UpdateScheduling => isManager,
            // The completer may replay the same terminal operation for idempotency; new terminal
            // commands are rejected by the manager after operation lookup.
            UserTaskAccessOperation.Complete => task.IsTerminal ? isCompleter : isAssignee,
            UserTaskAccessOperation.Cancel => isManager,
            UserTaskAccessOperation.Manage => isManager,
            UserTaskAccessOperation.IssueInvitation => isManager,
            UserTaskAccessOperation.RetryResolution => isManager,
            _ => false
        };

        return Task.FromResult(result);
    }

    private static bool AuthorizeGuest(UserTask task, UserTaskActor actor, UserTaskAccessOperation operation)
    {
        if (!string.Equals(actor.GuestTaskId, task.Id, StringComparison.Ordinal))
            return false;
        if (!GuestOperations.Contains(operation))
            return false;
        // The invitation claimed the task for the guest participant; losing that assignment (through a
        // manager reassignment or reissue) revokes the guest's access on the next request.
        if (task.Assignee?.Matches(actor.Subject) != true)
            return false;
        return operation != UserTaskAccessOperation.Complete || !task.IsTerminal;
    }

    /// <summary>
    /// A tenant-scoped manager must hold <c>manage:user-tasks</c> (or the wildcard grant). The actor flag on
    /// its own is host-supplied metadata and is never sufficient.
    /// </summary>
    private static bool IsManager(UserTaskActor actor) =>
        !actor.IsGuest && actor.IsManager && actor.HasPermission(UserTasksPermissions.Manage);

    private static bool IsCandidate(UserTask task, UserTaskActor actor)
    {
        if (task.ExcludedUsers.Any(x => x.Matches(actor.Subject)))
            return false;

        if (task.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot)
            return task.SnapshotMembers.Any(x => x.Matches(actor.Subject));

        return task.CandidateUsers.Any(x => x.Matches(actor.Subject))
               || task.CandidateGroups.Any(candidate => actor.Groups.Any(candidate.Matches));
    }

    private static bool IsExcluded(UserTask task, UserTaskActor actor) =>
        task.ExcludedUsers.Any(x => x.Matches(actor.Subject)) && !(IsManager(actor) && task.AllowManagerExclusionOverride);

    private static string RequiredPermission(UserTaskAccessOperation operation) => operation switch
    {
        UserTaskAccessOperation.ReadSummary or UserTaskAccessOperation.ReadProtected => UserTasksPermissions.Read,
        UserTaskAccessOperation.Claim or UserTaskAccessOperation.Release => UserTasksPermissions.Claim,
        UserTaskAccessOperation.Complete => UserTasksPermissions.Complete,
        UserTaskAccessOperation.Assign => UserTasksPermissions.Assign,
        UserTaskAccessOperation.UpdateScheduling => UserTasksPermissions.Update,
        UserTaskAccessOperation.Cancel => UserTasksPermissions.Cancel,
        UserTaskAccessOperation.Manage => UserTasksPermissions.Manage,
        UserTaskAccessOperation.IssueInvitation => UserTasksPermissions.Invite,
        UserTaskAccessOperation.RetryResolution => UserTasksPermissions.Manage,
        _ => UserTasksPermissions.Read
    };
}

/// <summary>
/// A safe default for hosts that do not expose a participant directory. It keeps lookup optional and
/// avoids coupling User Tasks to Elsa.Identity.
/// </summary>
public sealed class EmptyUserTaskParticipantDirectory : IUserTaskParticipantDirectory
{
    public Task<ParticipantSearchResult> SearchAsync(UserTaskParticipantQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ParticipantSearchResult([], null, 0));

    public Task<IReadOnlyCollection<ParticipantReference>> ResolveDisplayNamesAsync(IReadOnlyCollection<ParticipantReference> participants, CancellationToken cancellationToken = default) =>
        Task.FromResult(participants);

    public Task<IReadOnlyCollection<ParticipantReference>> EnumerateGroupMembersAsync(ParticipantReference group, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ParticipantReference>>([]);
}
