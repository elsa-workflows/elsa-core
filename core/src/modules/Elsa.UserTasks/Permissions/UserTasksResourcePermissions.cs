using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.UserTasks.Permissions;

/// <summary>
/// Stable resource names for User Tasks. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class UserTasksResourcePermissions
{
    /// <summary>Work on human tasks: read them, claim them, complete them, and supervise the whole tenant's queue.</summary>
    public const string UserTasks = "user-tasks";

    /// <summary>Search the users and groups a task may be assigned to.</summary>
    public const string Participants = "user-tasks/participants";
}

/// <summary>
/// The non-core verbs User Tasks declares. They live beside the resources they apply to so a call site and
/// the catalog cannot drift apart, and so a policy check cannot spell one differently from the endpoint it
/// guards.
/// </summary>
public static class UserTaskVerbs
{
    /// <summary>Take a task from the candidate pool, or give it back. One verb, because releasing is undoing a claim.</summary>
    public const string Claim = "claim";

    /// <summary>Submit a completion action against a task, resuming the workflow that raised it.</summary>
    public const string Complete = "complete";

    /// <summary>Hand a task to a participant.</summary>
    public const string Assign = "assign";

    /// <summary>End a task without completing it.</summary>
    public const string Cancel = "cancel";

    /// <summary>Issue, list, and revoke guest invitations to a task.</summary>
    public const string Invite = "invite";

    /// <summary>
    /// Act across the tenant's whole queue rather than only on tasks you take part in: read every task,
    /// assign, reschedule, cancel, see blocked tasks, and retry a failed resolution.
    /// </summary>
    /// <remarks>
    /// Named <c>supervise</c> rather than <c>manage</c> for the reason <c>workflows/runtime:control</c> is
    /// not called <c>manage</c> either: it is an elevated tier, not an aggregate of the verbs beside it, and
    /// a name that reads like an aggregate invites exactly that misreading. Holding it confers none of
    /// <c>claim</c>, <c>complete</c>, <c>assign</c>, <c>cancel</c> or <c>invite</c> — no verb implies another.
    /// </remarks>
    public const string Supervise = "supervise";
}

/// <summary>Contributes the User Tasks resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class UserTasksResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(UserTasksResourcePermissions.UserTasks,
            [CoreVerbs.View, CoreVerbs.Update, UserTaskVerbs.Claim, UserTaskVerbs.Complete, UserTaskVerbs.Assign, UserTaskVerbs.Cancel, UserTaskVerbs.Invite, UserTaskVerbs.Supervise],
            "User tasks",
            "Work on human tasks: read them, claim them, complete them, and supervise the whole tenant's queue.",
            "User Tasks"),
        new(UserTasksResourcePermissions.Participants,
            [CoreVerbs.View],
            "User task participants",
            "Search the users and groups a task may be assigned to.",
            "User Tasks"),
    ];
}
