using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Repositories;

/// <summary>
/// A deterministic repository for the Core module and development hosts. It uses a single lock to
/// provide the same compare-and-swap and projection idempotency guarantees expected from durable stores.
/// </summary>
public sealed class InMemoryUserTaskRepository : IUserTaskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Title OrderBy, ties, and cursors share this comparer. Title cursors are not portable to EF
    /// (column collation) or across databases; recreate the list after a provider change.
    /// </summary>
    private static readonly StringComparer TitleComparer = StringComparer.Ordinal;

    private readonly object _sync = new();
    private readonly Dictionary<string, UserTask> _tasks = new(StringComparer.Ordinal);

    public Task<UserTask?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
            return Task.FromResult(_tasks.TryGetValue(Key(tenantId, taskId), out var task) ? Clone(task) : null);
    }

    public Task<UserTaskQueryResult> QueryAsync(UserTaskQuery query, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var scope = query.Scope;
            var items = _tasks.Values
                .Where(x => string.Equals(x.TenantId, query.TenantId, StringComparison.Ordinal))
                // Visibility is evaluated before counting, cursoring, and paging so an unauthorized row can
                // never influence a total or push an authorized row off the page.
                .Where(x => scope == null || IsVisible(x, scope))
                .Where(x => query.Statuses.Count == 0 || query.Statuses.Contains(x.Status))
                .Where(x => !query.OnlyOverdue || x.IsOverdue)
                .Where(x => !query.OnlyWithoutDueDate || x.DueAt == null)
                .Where(x => query.PriorityFrom == null || x.Priority >= query.PriorityFrom)
                .Where(x => query.PriorityTo == null || x.Priority <= query.PriorityTo)
                .Where(x => query.DueFrom == null || x.DueAt >= query.DueFrom)
                .Where(x => query.DueTo == null || x.DueAt <= query.DueTo)
                .Where(x => query.WorkflowDefinitionId == null || x.WorkflowDefinitionId == query.WorkflowDefinitionId)
                .Where(x => query.WorkflowInstanceId == null || x.WorkflowInstanceId == query.WorkflowInstanceId)
                .Where(x => query.Reference == null || string.Equals(x.Reference, query.Reference, StringComparison.OrdinalIgnoreCase))
                .Where(x => query.TaskType == null || string.Equals(x.TaskType, query.TaskType, StringComparison.OrdinalIgnoreCase))
                .Where(x => MatchesSearch(x, query.Search));

            var filteredCount = query.IncludeTotalCount ? items.Count() : 0;
            var materialized = ApplyOrdering(items, query).ToList();
            materialized = ApplyCursor(materialized, query).ToList();

            int? total = query.IncludeTotalCount ? filteredCount : null;
            var limit = Math.Clamp(query.Limit, 1, 200);
            var page = materialized.Take(limit).Select(Clone).ToArray();
            var next = materialized.Count > limit ? CreateCursor(page[^1], query.Sort) : null;
            return Task.FromResult(new UserTaskQueryResult(page, next, total));
        }
    }

    public Task<UserTask?> FindByMaterializationKeyAsync(string tenantId, string key, CancellationToken cancellationToken = default)
    {
        lock (_sync)
            return Task.FromResult(_tasks.Values.FirstOrDefault(x => x.TenantId == tenantId && x.MaterializationKey == key) is { } task ? Clone(task) : null);
    }

    public Task<UserTask?> FindByBookmarkIdAsync(string tenantId, string bookmarkId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
            return Task.FromResult(_tasks.Values.FirstOrDefault(x => x.TenantId == tenantId && x.BookmarkId == bookmarkId) is { } task ? Clone(task) : null);
    }

    public Task<(UserTask Task, UserTaskInvitation Invitation)?> FindByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            foreach (var task in _tasks.Values)
            {
                // Ordinal comparison over a fixed-length hex hash; the secret itself is never stored.
                var invitation = task.Invitations.FirstOrDefault(x => string.Equals(x.TokenHash, tokenHash, StringComparison.Ordinal));
                if (invitation != null)
                    return Task.FromResult<(UserTask, UserTaskInvitation)?>((Clone(task), invitation));
            }
            return Task.FromResult<(UserTask, UserTaskInvitation)?>(null);
        }
    }

    public Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = Key(task.TenantId, task.Id);
            if (!_tasks.TryGetValue(key, out var current) || current.Revision != expectedRevision)
                throw new UserTaskRevisionConflictException(task.Id, expectedRevision);

            var copy = Clone(task);
            copy.Revision = expectedRevision + 1;
            copy.UpdatedAt = DateTimeOffset.UtcNow;
            _tasks[key] = copy;
            return Task.CompletedTask;
        }
    }

    public Task AddProjectionAsync(UserTask task, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_tasks.Values.Any(x => x.TenantId == task.TenantId &&
                ((!string.IsNullOrEmpty(task.MaterializationKey) && x.MaterializationKey == task.MaterializationKey) ||
                 (!string.IsNullOrEmpty(task.BookmarkId) && x.BookmarkId == task.BookmarkId))))
                return Task.CompletedTask;

            _tasks[Key(task.TenantId, task.Id)] = Clone(task);
            return Task.CompletedTask;
        }
    }

    public Task AppendEventAsync(string tenantId, string taskId, UserTaskEvent @event, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_tasks.TryGetValue(Key(tenantId, taskId), out var current))
                current.Events.Add(@event);
            return Task.CompletedTask;
        }
    }

    public Task<bool> TryMutateAsync(string tenantId, string taskId, int expectedRevision, Func<UserTask, bool> mutation, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = Key(tenantId, taskId);
            if (!_tasks.TryGetValue(key, out var current) || current.Revision != expectedRevision)
                return Task.FromResult(false);

            var copy = Clone(current);
            if (!mutation(copy))
                return Task.FromResult(false);

            copy.Revision = expectedRevision + 1;
            copy.UpdatedAt = DateTimeOffset.UtcNow;
            _tasks[key] = copy;
            return Task.FromResult(true);
        }
    }

    private static bool IsVisible(UserTask task, UserTaskQueryScope scope)
    {
        if (!string.Equals(task.TenantId, scope.TenantId, StringComparison.Ordinal))
            return false;
        if (scope.ExcludeBlocking && task.HealthSeverity == UserTaskHealthSeverity.Blocking)
            return false;
        // Manager-only scopes were already rejected by the policy for non-managers, so reaching them here
        // means the caller manages the tenant.
        if (scope.RequiresManager)
            return scope.IsManager && (scope.Kind != UserTaskQueryScopeKind.NeedsAttention || NeedsAttention(task));

        return scope.Kind switch
        {
            UserTaskQueryScopeKind.Assigned => task.Assignee?.Matches(scope.Subject) == true,
            UserTaskQueryScopeKind.Available => task.IsOpen && task.Assignee == null && IsEligible(task, scope),
            UserTaskQueryScopeKind.History => task.IsTerminal &&
                                              (task.CompletedBy?.Matches(scope.Subject) == true || task.Events.Any(x => x.Actor?.Matches(scope.Subject) == true)),
            _ => false
        };
    }

    private static bool IsEligible(UserTask task, UserTaskQueryScope scope)
    {
        if (task.ExcludedUsers.Any(x => x.Matches(scope.Subject)))
            return false;
        if (task.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot)
            return task.SnapshotMembers.Any(x => x.Matches(scope.Subject));
        return task.CandidateUsers.Any(x => x.Matches(scope.Subject))
               || task.CandidateGroups.Any(candidate => scope.Groups.Any(candidate.Matches));
    }

    private static bool NeedsAttention(UserTask task) =>
        task.HealthSeverity == UserTaskHealthSeverity.Blocking
        || task.IsOverdue
        || (task.IsOpen && task.Assignee == null)
        || task.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling;

    private static bool MatchesSearch(UserTask task, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        var value = search.Trim();
        return (task.Title?.Contains(value, StringComparison.OrdinalIgnoreCase) == true)
               || (task.Summary?.Contains(value, StringComparison.OrdinalIgnoreCase) == true)
               || (task.Reference?.Contains(value, StringComparison.OrdinalIgnoreCase) == true)
               || (task.TaskType?.Contains(value, StringComparison.OrdinalIgnoreCase) == true)
               || task.Tags.Any(x => x.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    // Same contract as EF/VNext: REST sorts only, Id always ThenBy ascending, JSON base64url cursors.
    private static IEnumerable<UserTask> ApplyOrdering(IEnumerable<UserTask> tasks, UserTaskQuery query) => query.Sort.ToLowerInvariant() switch
    {
        "priority" => query.Descending ? tasks.OrderByDescending(x => x.Priority).ThenBy(x => x.Id) : tasks.OrderBy(x => x.Priority).ThenBy(x => x.Id),
        "title" => query.Descending ? tasks.OrderByDescending(x => x.Title, TitleComparer).ThenBy(x => x.Id) : tasks.OrderBy(x => x.Title, TitleComparer).ThenBy(x => x.Id),
        "due" => query.Descending ? tasks.OrderBy(x => x.DueAt == null).ThenByDescending(x => x.DueAt).ThenBy(x => x.Id) : tasks.OrderBy(x => x.DueAt == null).ThenBy(x => x.DueAt).ThenBy(x => x.Id),
        "updated" => query.Descending ? tasks.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id) : tasks.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id),
        _ => query.Descending ? tasks.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id) : tasks.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
    };

    private static IEnumerable<UserTask> ApplyCursor(IEnumerable<UserTask> tasks, UserTaskQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Cursor) || !TryReadCursor(query.Cursor, out var value, out var id))
            return tasks;
        return query.Sort.ToLowerInvariant() switch
        {
            "priority" when int.TryParse(value, out var priority) => tasks.Where(x => query.Descending ? x.Priority < priority || x.Priority == priority && string.Compare(x.Id, id) > 0 : x.Priority > priority || x.Priority == priority && string.Compare(x.Id, id) > 0),
            "title" => tasks.Where(x => TitleIsAfterCursor(x.Title, value, x.Id, id, query.Descending)),
            "due" when value == "~null" => tasks.Where(x => x.DueAt == null && string.Compare(x.Id, id) > 0),
            "due" when DateTimeOffset.TryParse(value, out var due) => tasks.Where(x => x.DueAt == null || query.Descending && x.DueAt < due || !query.Descending && x.DueAt > due || x.DueAt == due && string.Compare(x.Id, id) > 0),
            "updated" when DateTimeOffset.TryParse(value, out var updated) => tasks.Where(x => query.Descending ? x.UpdatedAt < updated || x.UpdatedAt == updated && string.Compare(x.Id, id) > 0 : x.UpdatedAt > updated || x.UpdatedAt == updated && string.Compare(x.Id, id) > 0),
            _ when DateTimeOffset.TryParse(value, out var created) => tasks.Where(x => query.Descending ? x.CreatedAt < created || x.CreatedAt == created && string.Compare(x.Id, id) > 0 : x.CreatedAt > created || x.CreatedAt == created && string.Compare(x.Id, id) > 0),
            _ => tasks
        };
    }

    private static bool TitleIsAfterCursor(string title, string cursorTitle, string id, string cursorId, bool descending)
    {
        var comparison = TitleComparer.Compare(title, cursorTitle);
        return descending
            ? comparison < 0 || comparison == 0 && string.Compare(id, cursorId) > 0
            : comparison > 0 || comparison == 0 && string.Compare(id, cursorId) > 0;
    }

    private static string CreateCursor(UserTask task, string sort)
    {
        var value = sort.ToLowerInvariant() switch
        {
            "priority" => task.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "title" => task.Title,
            "due" => task.DueAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "~null",
            "updated" => task.UpdatedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            _ => task.CreatedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        };
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new[] { value, task.Id }, JsonOptions)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryReadCursor(string? cursor, out string value, out string id)
    {
        value = id = "";
        if (string.IsNullOrWhiteSpace(cursor))
            return false;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/') + new string('=', (4 - cursor.Length % 4) % 4);
            var values = JsonSerializer.Deserialize<string[]>(Convert.FromBase64String(padded), JsonOptions);
            if (values is not [var parsedValue, var parsedId] || string.IsNullOrWhiteSpace(parsedId))
                return false;
            value = parsedValue;
            id = parsedId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Key(string tenantId, string taskId) => tenantId + "\0" + taskId;

    private static UserTask Clone(UserTask task) => new()
    {
        Id = task.Id,
        TenantId = task.TenantId,
        WorkflowDefinitionId = task.WorkflowDefinitionId,
        WorkflowDefinitionName = task.WorkflowDefinitionName,
        WorkflowDefinitionVersion = task.WorkflowDefinitionVersion,
        WorkflowInstanceId = task.WorkflowInstanceId,
        WorkflowInstanceReference = task.WorkflowInstanceReference,
        ActivityInstanceId = task.ActivityInstanceId,
        BookmarkId = task.BookmarkId,
        MaterializationKey = task.MaterializationKey,
        Title = task.Title,
        Summary = task.Summary,
        Reference = task.Reference,
        Tags = new HashSet<string>(task.Tags, StringComparer.OrdinalIgnoreCase),
        TaskType = task.TaskType,
        Requester = task.Requester,
        Assignee = task.Assignee,
        CandidateUsers = [..task.CandidateUsers],
        CandidateGroups = [..task.CandidateGroups],
        SnapshotMembers = [..task.SnapshotMembers],
        SnapshotGroups = [..task.SnapshotGroups],
        ExcludedUsers = [..task.ExcludedUsers],
        MembershipResolutionMode = task.MembershipResolutionMode,
        AllowManagerExclusionOverride = task.AllowManagerExclusionOverride,
        Priority = task.Priority,
        DueAt = task.DueAt,
        IsOverdue = task.IsOverdue,
        Instructions = task.Instructions,
        TaskData = Clone(task.TaskData),
        RequestedForm = task.RequestedForm,
        PinnedForm = task.PinnedForm,
        Actions = [..task.Actions],
        InvitationDefinitions = [..task.InvitationDefinitions],
        EnableTimeoutOutcome = task.EnableTimeoutOutcome,
        EnableCancellationOutcome = task.EnableCancellationOutcome,
        Status = task.Status,
        HealthSeverity = task.HealthSeverity,
        HealthCode = task.HealthCode,
        HealthMessage = task.HealthMessage,
        CompletionActionKey = task.CompletionActionKey,
        CompletionData = Clone(task.CompletionData),
        CompletedBy = task.CompletedBy,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        AssignedAt = task.AssignedAt,
        CompletedAt = task.CompletedAt,
        Revision = task.Revision,
        Events = [..task.Events],
        Operations = [..task.Operations],
        Invitations = [..task.Invitations]
    };

    private static JsonElement? Clone(JsonElement? value) => value is { } element ? element.Clone() : null;
}
