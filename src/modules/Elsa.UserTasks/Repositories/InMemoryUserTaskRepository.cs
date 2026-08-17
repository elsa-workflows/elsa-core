using System.Text;
using System.Text.Json;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Repositories;

/// <summary>
/// A deterministic repository for the Core module and development hosts. It uses a single lock to
/// provide the same compare-and-swap and projection idempotency guarantees expected from durable stores.
/// </summary>
public sealed class InMemoryUserTaskRepository : IUserTaskRepository
{
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
                .Where(x => scope == null || IsVisible(x, scope))
                .Where(x => query.Status == null || x.Status == query.Status)
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
            var materialized = Sort(items, query.Sort, query.Descending).ToList();
            if (!string.IsNullOrWhiteSpace(query.Cursor))
            {
                var cursor = DecodeCursor(query.Cursor!);
                if (cursor != null)
                    materialized = materialized.Where(x => IsAfterCursor(x, cursor.Value, query.Descending, query.Sort)).ToList();
            }

            int? total = query.IncludeTotalCount ? filteredCount : null;
            var limit = Math.Clamp(query.Limit, 1, 200);
            var page = materialized.Take(limit).Select(Clone).ToArray();
            var next = materialized.Count > limit ? EncodeCursor(page[^1], query.Sort) : null;
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

    public Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = Key(task.TenantId, task.Id);
            if (!_tasks.TryGetValue(key, out var current) || current.Revision != expectedRevision)
                throw new InvalidOperationException("User Task revision conflict.");

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
        if (scope.IsManager)
            return true;
        if (scope.IncludeAssigned && task.Assignee?.Matches(scope.Subject) == true)
            return true;
        if (scope.IncludeCandidateUsers && task.CandidateUsers.Any(x => x.Matches(scope.Subject)))
            return true;
        if (scope.IncludeCandidateGroups && task.CandidateGroups.Any(candidate => scope.Groups.Any(candidate.Matches)))
            return true;
        if (scope.IncludeSnapshotMembers && task.SnapshotMembers.Any(x => x.Matches(scope.Subject)))
            return true;
        return scope.IncludeHistory && (task.CompletedBy?.Matches(scope.Subject) == true || task.Events.Any(x => x.Actor?.Matches(scope.Subject) == true));
    }

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

    private static IEnumerable<UserTask> Sort(IEnumerable<UserTask> items, string sort, bool descending)
    {
        var normalized = NormalizeSort(sort);
        return normalized switch
        {
            "due" => descending
                ? items.OrderByDescending(x => x.DueAt.HasValue).ThenByDescending(x => x.DueAt).ThenByDescending(x => x.Id, StringComparer.Ordinal)
                : items.OrderBy(x => x.DueAt.HasValue ? 0 : 1).ThenBy(x => x.DueAt).ThenBy(x => x.Id, StringComparer.Ordinal),
            "priority" => descending
                ? items.OrderByDescending(x => x.Priority).ThenByDescending(x => x.Id, StringComparer.Ordinal)
                : items.OrderBy(x => x.Priority).ThenBy(x => x.Id, StringComparer.Ordinal),
            "updated" => descending
                ? items.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id, StringComparer.Ordinal)
                : items.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id, StringComparer.Ordinal),
            "completed" => descending
                ? items.OrderByDescending(x => x.CompletedAt.HasValue).ThenByDescending(x => x.CompletedAt).ThenByDescending(x => x.Id, StringComparer.Ordinal)
                : items.OrderBy(x => x.CompletedAt.HasValue ? 0 : 1).ThenBy(x => x.CompletedAt).ThenBy(x => x.Id, StringComparer.Ordinal),
            "title" => descending
                ? items.OrderByDescending(x => x.Title, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.Id, StringComparer.Ordinal)
                : items.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.Ordinal),
            _ => descending
                ? items.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id, StringComparer.Ordinal)
                : items.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id, StringComparer.Ordinal)
        };
    }

    private static bool IsAfterCursor(UserTask task, (string Kind, string Value, string Id) cursor, bool descending, string sort)
    {
        var normalized = NormalizeSort(sort);
        // Due/completed ordering keeps null values at the end in both directions. A generic numeric
        // comparison would incorrectly drop nulls after a descending non-null page (or reintroduce
        // non-null values after a descending null page).
        if (descending && (normalized is "due" or "completed"))
        {
            var valueIsNull = SortValue(task, normalized) == null;
            var cursorIsNull = cursor.Value == "~";
            if (valueIsNull != cursorIsNull)
                return valueIsNull;
        }

        var comparison = CompareSortValue(SortKind(sort), SortValue(task, sort), cursor.Kind, cursor.Value);
        if (comparison == 0)
            comparison = StringComparer.Ordinal.Compare(task.Id, cursor.Id);
        return descending ? comparison < 0 : comparison > 0;
    }

    private static string EncodeCursor(UserTask task, string sort)
    {
        var kind = SortKind(sort);
        var value = EncodeSortValue(SortValue(task, sort), kind);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(kind + "|" + value + "|" + task.Id));
    }

    private static (string Kind, string Value, string Id)? DecodeCursor(string cursor)
    {
        try
        {
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var first = value.IndexOf('|');
            var last = value.LastIndexOf('|');
            return first <= 0 || last <= first ? null : (value[..first], value[(first + 1)..last], value[(last + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string NormalizeSort(string sort) => sort.ToLowerInvariant() switch
    {
        "dueat" => "due",
        "completedat" => "completed",
        _ => sort.ToLowerInvariant() is "due" or "priority" or "updated" or "completed" or "title" ? sort.ToLowerInvariant() : "created"
    };

    private static string SortKind(string sort) => NormalizeSort(sort) switch
    {
        "priority" => "i",
        "title" => "s",
        "due" or "completed" => "n",
        _ => "n"
    };

    private static object? SortValue(UserTask task, string sort) => NormalizeSort(sort) switch
    {
        "due" => task.DueAt,
        "priority" => task.Priority,
        "updated" => task.UpdatedAt,
        "completed" => task.CompletedAt,
        "title" => task.Title,
        _ => task.CreatedAt
    };

    private static string EncodeSortValue(object? value, string kind) => value switch
    {
        null => "~",
        DateTimeOffset date => date.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
        int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString() ?? ""))
    };

    private static int CompareSortValue(string kind, object? value, string cursorKind, string cursorValue)
    {
        if (!string.Equals(kind, cursorKind, StringComparison.Ordinal))
            return 0;
        if (value == null || cursorValue == "~")
            return value == null && cursorValue == "~" ? 0 : value == null ? 1 : -1;
        if (kind == "i")
            return int.Parse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture).CompareTo(int.Parse(cursorValue, System.Globalization.CultureInfo.InvariantCulture));
        if (kind == "n")
            return long.Parse(value switch { DateTimeOffset d => d.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture), _ => value.ToString()! }, System.Globalization.CultureInfo.InvariantCulture)
                .CompareTo(long.Parse(cursorValue, System.Globalization.CultureInfo.InvariantCulture));
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursorValue));
        return StringComparer.OrdinalIgnoreCase.Compare(value.ToString(), decoded);
    }

    private static string Key(string tenantId, string taskId) => tenantId + "\0" + taskId;

    private static UserTask Clone(UserTask task) => new()
    {
        Id = task.Id,
        TenantId = task.TenantId,
        WorkflowDefinitionId = task.WorkflowDefinitionId,
        WorkflowInstanceId = task.WorkflowInstanceId,
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
