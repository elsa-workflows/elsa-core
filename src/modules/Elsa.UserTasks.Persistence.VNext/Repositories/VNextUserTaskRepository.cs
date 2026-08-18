using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.VNext.Document;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Persistence.VNext.Repositories;

/// <summary>
/// Provider-neutral document implementation. The aggregate is stored as one document while the schema
/// provider advertises the same logical units and indexes as the relational providers.
/// </summary>
public sealed class VNextUserTaskRepository(IDocumentStore documentStore) : IUserTaskRepository
{
    public const string StorageUnitName = "UserTasks";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<UserTask?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default)
    {
        var document = await documentStore.LoadAsync(StorageUnitName, DocumentId(tenantId, taskId), cancellationToken);
        return document is null ? null : Deserialize(document);
    }

    public async Task<UserTaskQueryResult> QueryAsync(UserTaskQuery query, CancellationToken cancellationToken = default)
    {
        var tasks = new List<UserTask>();
        foreach (var status in Enum.GetValues<UserTaskStatus>())
        {
            var documents = await documentStore.QueryAsync(
                new DocumentQuery(StorageUnitName, new Dictionary<string, string?>
                {
                    ["TenantId"] = query.TenantId,
                    ["Status"] = status.ToString()
                }), cancellationToken);
            tasks.AddRange(documents.Select(Deserialize));
        }

        var filtered = tasks.Where(x => Matches(x, query)).Where(x => query.Scope is null || IsVisible(x, query.Scope)).ToList();
        int? totalCount = query.IncludeTotalCount ? filtered.Count : null;
        filtered = ApplyOrdering(filtered, query).ToList();
        filtered = ApplyCursor(filtered, query).ToList();
        var limit = Math.Clamp(query.Limit <= 0 ? 50 : query.Limit, 1, 200);
        var hasMore = filtered.Count > limit;
        var page = filtered.Take(limit).ToList();
        return new UserTaskQueryResult(page, hasMore ? CreateCursor(page[^1], query.Sort) : null, totalCount);
    }

    public Task<UserTask?> FindByMaterializationKeyAsync(string tenantId, string key, CancellationToken cancellationToken = default) => FindByIndexAsync(tenantId, x => x.MaterializationKey == key, cancellationToken);

    public Task<UserTask?> FindByBookmarkIdAsync(string tenantId, string bookmarkId, CancellationToken cancellationToken = default) => FindByIndexAsync(tenantId, x => x.BookmarkId == bookmarkId, cancellationToken);

    public async Task<(UserTask Task, UserTaskInvitation Invitation)?> FindByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        // The document store has no cross-tenant secondary index for invitation hashes, so this scans the
        // storage unit. Verification is rate limited and rare; correctness matters more than the scan here.
        foreach (var task in await LoadAllAsync(cancellationToken))
        {
            var invitation = task.Invitations.FirstOrDefault(x => string.Equals(x.TokenHash, tokenHash, StringComparison.Ordinal));
            if (invitation != null)
                return (task, invitation);
        }
        return null;
    }

    public async Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDocumentAsync(task.TenantId, task.Id, cancellationToken);
        if (existing is null)
            throw new KeyNotFoundException($"User task '{task.Id}' was not found.");
        var loaded = existing.Value;
        if (loaded.Task.Revision != expectedRevision)
            throw new DocumentStoreConcurrencyException(StorageUnitName, loaded.Document.Id, expectedRevision, loaded.Document.Version);

        task.Revision = expectedRevision + 1;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await documentStore.SaveAsync(CreateRequest(task, loaded.Document.Version), cancellationToken);
    }

    public async Task AddProjectionAsync(UserTask task, CancellationToken cancellationToken = default)
    {
        if (await FindByMaterializationKeyAsync(task.TenantId, task.MaterializationKey, cancellationToken) is not null)
            return;
        try
        {
            await documentStore.SaveAsync(CreateRequest(task, expectedVersion: 0), cancellationToken);
        }
        catch (DocumentStoreConcurrencyException)
        {
            // Projection is idempotent when the same aggregate ID was committed concurrently.
        }
    }

    public async Task AppendEventAsync(string tenantId, string taskId, UserTaskEvent @event, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDocumentAsync(tenantId, taskId, cancellationToken);
        if (existing is null)
            return;

        // The document store has no separate audit stream, so the entry is written back with the aggregate.
        // The revision is deliberately left as-is: an audited read must not consume the concurrency token.
        var loaded = existing.Value;
        loaded.Task.Events.Add(@event);
        try
        {
            await documentStore.SaveAsync(CreateRequest(loaded.Task, loaded.Document.Version), cancellationToken);
        }
        catch (DocumentStoreConcurrencyException)
        {
            // A concurrent writer won. The audit entry is advisory, so losing this race is not an error.
        }
    }

    public async Task<bool> TryMutateAsync(string tenantId, string taskId, int expectedRevision, Func<UserTask, bool> mutation, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDocumentAsync(tenantId, taskId, cancellationToken);
        if (existing is null)
            return false;
        var loaded = existing.Value;
        if (loaded.Task.Revision != expectedRevision || !mutation(loaded.Task))
            return false;
        loaded.Task.Revision = expectedRevision + 1;
        loaded.Task.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await documentStore.SaveAsync(CreateRequest(loaded.Task, loaded.Document.Version), cancellationToken);
            return true;
        }
        catch (DocumentStoreConcurrencyException)
        {
            return false;
        }
    }

    /// <summary>
    /// Loads every stored task across tenants. Only the invitation-hash lookup uses this: an anonymous
    /// holder presents a secret and no tenant, so the scan cannot be narrowed by an index.
    /// </summary>
    private async Task<IReadOnlyCollection<UserTask>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<UserTask>();
        foreach (var status in Enum.GetValues<UserTaskStatus>())
        {
            var documents = await documentStore.QueryAsync(new DocumentQuery(StorageUnitName, new Dictionary<string, string?>
            {
                ["Status"] = status.ToString()
            }), cancellationToken);
            tasks.AddRange(documents.Select(Deserialize));
        }
        return tasks;
    }

    private async Task<UserTask?> FindByIndexAsync(string tenantId, Func<UserTask, bool> predicate, CancellationToken cancellationToken)
    {
        foreach (var status in Enum.GetValues<UserTaskStatus>())
        {
            var documents = await documentStore.QueryAsync(new DocumentQuery(StorageUnitName, new Dictionary<string, string?>
            {
                ["TenantId"] = tenantId,
                ["Status"] = status.ToString()
            }), cancellationToken);
            var task = documents.Select(Deserialize).FirstOrDefault(predicate);
            if (task is not null)
                return task;
        }
        return null;
    }

    private async Task<(StoredDocument Document, UserTask Task)?> LoadDocumentAsync(string tenantId, string taskId, CancellationToken cancellationToken)
    {
        var document = await documentStore.LoadAsync(StorageUnitName, DocumentId(tenantId, taskId), cancellationToken);
        return document is null ? null : (document, Deserialize(document));
    }

    private static SaveDocumentRequest CreateRequest(UserTask task, long expectedVersion) => new(
        StorageUnitName,
        DocumentId(task.TenantId, task.Id),
        JsonSerializer.Serialize(task, JsonOptions),
        new Dictionary<string, string?>
        {
            ["TenantId"] = task.TenantId,
            ["Status"] = task.Status.ToString(),
            ["MaterializationKey"] = task.MaterializationKey,
            ["BookmarkId"] = task.BookmarkId,
            ["TaskType"] = task.TaskType,
            ["AssigneeProvider"] = task.Assignee?.Provider,
            ["AssigneeType"] = task.Assignee?.Type.ToString(),
            ["AssigneeId"] = task.Assignee?.Id,
            ["HealthSeverity"] = task.HealthSeverity?.ToString(),
            ["Priority"] = task.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["DueAt"] = task.DueAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        }, expectedVersion);

    private static UserTask Deserialize(StoredDocument document) => JsonSerializer.Deserialize<UserTask>(document.Content, JsonOptions)
        ?? throw new DocumentStoreValidationException($"Stored User Task document '{document.Id}' could not be deserialized.");

    private static string DocumentId(string tenantId, string taskId) => $"{tenantId}:{taskId}";

    private static bool Matches(UserTask task, UserTaskQuery query)
    {
        var search = query.Search?.Trim();
        return task.TenantId == query.TenantId && (query.Statuses.Count == 0 || query.Statuses.Contains(task.Status)) &&
               (!query.OnlyOverdue || task.IsOverdue) && (!query.OnlyWithoutDueDate || task.DueAt is null) &&
               (string.IsNullOrWhiteSpace(query.TaskType) || task.TaskType == query.TaskType) &&
               (!query.PriorityFrom.HasValue || task.Priority >= query.PriorityFrom) && (!query.PriorityTo.HasValue || task.Priority <= query.PriorityTo) &&
               (!query.DueFrom.HasValue || task.DueAt >= query.DueFrom) && (!query.DueTo.HasValue || task.DueAt <= query.DueTo) &&
               (string.IsNullOrWhiteSpace(query.WorkflowDefinitionId) || task.WorkflowDefinitionId == query.WorkflowDefinitionId) &&
               (string.IsNullOrWhiteSpace(query.WorkflowInstanceId) || task.WorkflowInstanceId == query.WorkflowInstanceId) &&
               (string.IsNullOrWhiteSpace(query.Reference) || task.Reference == query.Reference) &&
               (string.IsNullOrWhiteSpace(search) || task.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || task.Summary?.Contains(search, StringComparison.OrdinalIgnoreCase) == true || task.Reference?.Contains(search, StringComparison.OrdinalIgnoreCase) == true || task.TaskType?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static bool IsVisible(UserTask task, UserTaskQueryScope scope)
    {
        if (!string.Equals(scope.TenantId, task.TenantId, StringComparison.Ordinal) ||
            !string.Equals(scope.Subject.TenantId, task.TenantId, StringComparison.Ordinal) ||
            scope.Groups.Any(group => !string.Equals(group.TenantId, task.TenantId, StringComparison.Ordinal)))
            return false;
        if (scope.ExcludeBlocking && task.HealthSeverity == UserTaskHealthSeverity.Blocking)
            return false;
        // Manager-only scopes were already rejected by the policy for non-managers.
        if (scope.RequiresManager)
            return scope.IsManager && (scope.Kind != UserTaskQueryScopeKind.NeedsAttention || NeedsAttention(task));

        var subject = scope.Subject;
        var groups = scope.Groups;
        return scope.Kind switch
        {
            UserTaskQueryScopeKind.Assigned => task.Assignee?.Matches(subject) == true,
            UserTaskQueryScopeKind.Available => task.IsOpen && task.Assignee is null && IsEligible(task, subject, groups),
            UserTaskQueryScopeKind.History => task.IsTerminal &&
                                              (task.CompletedBy?.Matches(subject) == true || task.Events.Any(x => x.Actor?.Matches(subject) == true)),
            _ => false
        };
    }

    private static bool IsEligible(UserTask task, ParticipantReference subject, IReadOnlyCollection<ParticipantReference> groups)
    {
        if (task.ExcludedUsers.Any(x => x.Matches(subject)))
            return false;
        if (task.MembershipResolutionMode == UserTaskMembershipResolutionMode.Snapshot)
            return task.SnapshotMembers.Any(x => x.Matches(subject)) || task.SnapshotGroups.Any(x => groups.Any(x.Matches));
        return task.CandidateUsers.Any(x => x.Matches(subject)) || task.CandidateGroups.Any(x => groups.Any(x.Matches));
    }

    private static bool NeedsAttention(UserTask task) =>
        task.HealthSeverity == UserTaskHealthSeverity.Blocking
        || task.IsOverdue
        || (task.IsOpen && task.Assignee is null)
        || task.Status is UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling;

    private static IEnumerable<UserTask> ApplyOrdering(IEnumerable<UserTask> tasks, UserTaskQuery query) => query.Sort.ToLowerInvariant() switch
    {
        "priority" => query.Descending ? tasks.OrderByDescending(x => x.Priority).ThenBy(x => x.Id) : tasks.OrderBy(x => x.Priority).ThenBy(x => x.Id),
        "title" => query.Descending ? tasks.OrderByDescending(x => x.Title).ThenBy(x => x.Id) : tasks.OrderBy(x => x.Title).ThenBy(x => x.Id),
        "due" => query.Descending ? tasks.OrderBy(x => x.DueAt == null).ThenByDescending(x => x.DueAt).ThenBy(x => x.Id) : tasks.OrderBy(x => x.DueAt == null).ThenBy(x => x.DueAt).ThenBy(x => x.Id),
        _ => query.Descending ? tasks.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id) : tasks.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
    };

    private static IEnumerable<UserTask> ApplyCursor(IEnumerable<UserTask> tasks, UserTaskQuery query)
    {
        if (!TryReadCursor(query.Cursor, out var value, out var id))
            return tasks;
        return query.Sort.ToLowerInvariant() switch
        {
            "priority" when int.TryParse(value, out var priority) => tasks.Where(x => query.Descending ? x.Priority < priority || x.Priority == priority && string.Compare(x.Id, id) > 0 : x.Priority > priority || x.Priority == priority && string.Compare(x.Id, id) > 0),
            "title" => tasks.Where(x => query.Descending ? string.Compare(x.Title, value) < 0 || x.Title == value && string.Compare(x.Id, id) > 0 : string.Compare(x.Title, value) > 0 || x.Title == value && string.Compare(x.Id, id) > 0),
            "due" when value == "~null" => tasks.Where(x => x.DueAt == null && string.Compare(x.Id, id) > 0),
            "due" when DateTimeOffset.TryParse(value, out var due) => tasks.Where(x => x.DueAt == null || query.Descending && x.DueAt < due || !query.Descending && x.DueAt > due || x.DueAt == due && string.Compare(x.Id, id) > 0),
            _ when DateTimeOffset.TryParse(value, out var created) => tasks.Where(x => query.Descending ? x.CreatedAt < created || x.CreatedAt == created && string.Compare(x.Id, id) > 0 : x.CreatedAt > created || x.CreatedAt == created && string.Compare(x.Id, id) > 0),
            _ => tasks
        };
    }

    private static string CreateCursor(UserTask task, string sort)
    {
        var value = sort.ToLowerInvariant() switch
        {
            "priority" => task.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "title" => task.Title,
            "due" => task.DueAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "~null",
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
            if (values is not [var parsedValue, var parsedId])
                return false;
            value = parsedValue;
            id = parsedId;
            return true;
        }
        catch (FormatException) { return false; }
        catch (JsonException) { return false; }
    }
}
