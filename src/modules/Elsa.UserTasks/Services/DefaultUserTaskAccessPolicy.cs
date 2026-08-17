using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskAccessPolicy : IUserTaskAccessPolicy
{
    public Task<UserTaskQueryScope> CreateScopeAsync(UserTaskActor actor, UserTaskAccessOperation operation, CancellationToken cancellationToken = default)
    {
        var canRead = actor.HasPermission("read:user-tasks");
        var managerScope = actor.IsManager && actor.HasPermission("manage:user-tasks") && canRead;
        return Task.FromResult(new UserTaskQueryScope(
            actor.Subject.TenantId,
            actor.Subject,
            actor.Groups,
            managerScope,
            IncludeAssigned: canRead,
            IncludeCandidateUsers: canRead && operation is (UserTaskAccessOperation.ReadSummary or UserTaskAccessOperation.Claim),
            IncludeCandidateGroups: canRead && operation is (UserTaskAccessOperation.ReadSummary or UserTaskAccessOperation.Claim),
            IncludeSnapshotMembers: canRead && operation is (UserTaskAccessOperation.ReadSummary or UserTaskAccessOperation.Claim),
            IncludeHistory: canRead,
            ExcludeBlocking: !managerScope));
    }

    public Task<bool> AuthorizeAsync(UserTask task, UserTaskActor actor, UserTaskAccessOperation operation, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(task.TenantId, actor.Subject.TenantId, StringComparison.Ordinal))
            return Task.FromResult(false);

        var isManager = actor.IsManager;
        if (!actor.HasPermission(RequiredPermission(operation)))
            return Task.FromResult(false);
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
        task.ExcludedUsers.Any(x => x.Matches(actor.Subject)) && !(actor.IsManager && task.AllowManagerExclusionOverride);

    private static string RequiredPermission(UserTaskAccessOperation operation) => operation switch
    {
        UserTaskAccessOperation.ReadSummary or UserTaskAccessOperation.ReadProtected => "read:user-tasks",
        UserTaskAccessOperation.Claim or UserTaskAccessOperation.Release => "claim:user-tasks",
        UserTaskAccessOperation.Complete => "complete:user-tasks",
        UserTaskAccessOperation.Assign => "assign:user-tasks",
        UserTaskAccessOperation.UpdateScheduling => "update:user-tasks",
        UserTaskAccessOperation.Cancel => "cancel:user-tasks",
        UserTaskAccessOperation.Manage => "manage:user-tasks",
        UserTaskAccessOperation.IssueInvitation => "invite:user-tasks",
        UserTaskAccessOperation.RetryResolution => "manage:user-tasks",
        _ => "read:user-tasks"
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
