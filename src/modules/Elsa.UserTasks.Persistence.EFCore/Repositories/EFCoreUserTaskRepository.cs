using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.EFCore.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore.Repositories;

/// <summary>
/// EF Core implementation of the public User Tasks repository and the low-level projection adapter.
/// Participant identities are flattened into provider/type/id columns; no identity or foreign-key
/// dependency is introduced by this package.
/// </summary>
public sealed class EFCoreUserTaskRepository(Store<UserTasksElsaDbContext, UserTaskRecord> store)
    : IUserTaskRepository, IUserTaskPersistenceAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<UserTask?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var record = await dbContext.UserTasks.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == taskId, cancellationToken);
        return record is null ? null : await LoadAggregateAsync(dbContext, record, cancellationToken);
    }

    public async Task<UserTaskQueryResult> QueryAsync(UserTaskQuery query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var records = BuildQuery(dbContext, query);
        int? totalCount = query.IncludeTotalCount ? await records.CountAsync(cancellationToken) : null;
        var limit = Math.Clamp(query.Limit <= 0 ? 50 : query.Limit, 1, 200);
        var rows = await ApplyCursor(ApplyOrdering(records, query), query).Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        // The query predicate is intentionally SQL-side, but the policy layer still needs the
        // normalized candidate/snapshot/exclusion/history relationships to calculate allowed actions.
        // Hydrate those relationships in bounded batches after paging rather than issuing one query per
        // task or allowing a summary projection with empty candidate collections.
        var items = await LoadAggregatesAsync(dbContext, page, cancellationToken);
        return new UserTaskQueryResult(items, hasMore ? CreateCursor(page[^1], query.Sort) : null, totalCount);
    }

    public async Task<UserTask?> FindByMaterializationKeyAsync(string tenantId, string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var record = await dbContext.UserTasks.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MaterializationKey == key, cancellationToken);
        return record is null ? null : await LoadAggregateAsync(dbContext, record, cancellationToken);
    }

    public async Task<UserTask?> FindByBookmarkIdAsync(string tenantId, string bookmarkId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var record = await dbContext.UserTasks.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.BookmarkId == bookmarkId, cancellationToken);
        return record is null ? null : await LoadAggregateAsync(dbContext, record, cancellationToken);
    }

    public async Task<(UserTask Task, UserTaskInvitation Invitation)?> FindByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        // The unique index on TokenHash makes this a single seek. Deliberately not tenant-filtered: an
        // anonymous holder presents only a secret and must not be trusted to name its own tenant.
        var row = await dbContext.UserTaskInvitations.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (row is null)
            return null;

        var record = await dbContext.UserTasks.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == row.TenantId && x.Id == row.TaskId, cancellationToken);
        if (record is null)
            return null;

        var task = await LoadAggregateAsync(dbContext, record, cancellationToken);
        return (task, ToInvitation(row));
    }

    public async Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await dbContext.UserTasks.FirstOrDefaultAsync(x => x.TenantId == task.TenantId && x.Id == task.Id, cancellationToken);
        EnsureExpectedRevision(existing, task.Id, expectedRevision);

        Copy(task, existing!);
        existing!.Revision = expectedRevision + 1;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await ReplaceChildrenAsync(dbContext, task, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            task.Revision = existing.Revision;
            task.UpdatedAt = existing.UpdatedAt;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            // Translated to the repository contract's exception so callers can distinguish a lost
            // optimistic-concurrency race from a fault without depending on EF Core.
            throw new UserTaskRevisionConflictException(task.Id, expectedRevision, exception);
        }
    }

    public async Task AddProjectionAsync(UserTask task, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        if (await ExistsByMaterializationKeyAsync(dbContext, task.TenantId, task.MaterializationKey, cancellationToken))
            return;

        dbContext.UserTasks.Add(ToRecord(task));
        dbContext.UserTaskCandidates.AddRange(task.CandidateUsers.Select(x => ToCandidate(task, x, UserTaskPersistenceCandidateSource.DirectUser)));
        dbContext.UserTaskCandidates.AddRange(task.CandidateGroups.Select(x => ToCandidate(task, x, UserTaskPersistenceCandidateSource.DirectGroup)));
        dbContext.UserTaskSnapshotMembers.AddRange(task.SnapshotMembers.Select(x => ToSnapshot(task, x)));
        dbContext.UserTaskSnapshotMembers.AddRange(task.SnapshotGroups.Select(x => ToSnapshot(task, x)));
        dbContext.UserTaskExclusions.AddRange(task.ExcludedUsers.Select(x => ToExclusion(task, x)));
        dbContext.UserTaskEvents.AddRange(task.Events.Select(ToEventRecord));
        dbContext.UserTaskOperations.AddRange(task.Operations.Select(ToOperationRecord));
        dbContext.UserTaskInvitations.AddRange(task.Invitations.Select(ToInvitationRecord));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await ExistsByMaterializationKeyAsync(dbContext, task.TenantId, task.MaterializationKey, cancellationToken))
                throw;
            // A concurrent projection won the unique materialization-key race. Projection is idempotent.
        }
    }

    public async Task AppendEventAsync(string tenantId, string taskId, UserTaskEvent @event, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        if (!await dbContext.UserTasks.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == taskId, cancellationToken))
            return;

        // A plain insert: the aggregate row, and therefore its revision, is deliberately left untouched.
        dbContext.UserTaskEvents.Add(ToEventRecord(@event));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryMutateAsync(string tenantId, string taskId, int expectedRevision, Func<UserTask, bool> mutation, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await dbContext.UserTasks.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == taskId, cancellationToken);
        if (record is null || record.Revision != expectedRevision)
            return false;

        var task = await LoadAggregateAsync(dbContext, record, cancellationToken);
        if (!mutation(task))
            return false;

        Copy(task, record);
        record.Revision = expectedRevision + 1;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        await ReplaceChildrenAsync(dbContext, task, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    async Task<UserTaskRecord?> IUserTaskPersistenceAdapter.GetAsync(string tenantId, string taskId, CancellationToken cancellationToken)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        return await dbContext.UserTasks.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == taskId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserTaskRecord>> QueryAsync(UserTaskPersistenceQuery query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var records = dbContext.UserTasks.AsNoTracking().Where(x => x.TenantId == query.TenantId);
        if (query.Statuses.Count > 0)
            records = records.Where(x => query.Statuses.Contains(x.Status));
        if (!string.IsNullOrWhiteSpace(query.AssigneeProvider))
            records = records.Where(x => x.AssigneeProvider == query.AssigneeProvider);
        if (!string.IsNullOrWhiteSpace(query.AssigneeType))
            records = records.Where(x => x.AssigneeType == query.AssigneeType);
        if (!string.IsNullOrWhiteSpace(query.AssigneeId))
            records = records.Where(x => x.AssigneeId == query.AssigneeId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            records = records.Where(x => x.Title.Contains(search) || (x.Summary != null && x.Summary.Contains(search)) || (x.Reference != null && x.Reference.Contains(search)) || (x.TaskType != null && x.TaskType.Contains(search)));
        }

        var limit = query.Limit is > 0 ? Math.Min(query.Limit.Value, 200) : 100;
        return await records.OrderBy(x => x.DueAt == null).ThenBy(x => x.DueAt).ThenByDescending(x => x.Priority).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAddProjectionAsync(UserTaskRecord task, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        if (await ExistsByMaterializationKeyAsync(dbContext, task.TenantId, task.MaterializationKey, cancellationToken))
            return false;
        dbContext.UserTasks.Add(task);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            if (!await ExistsByMaterializationKeyAsync(dbContext, task.TenantId, task.MaterializationKey, cancellationToken))
                throw;
            return false;
        }
    }

    public async Task<bool> TrySaveAsync(UserTaskRecord task, int expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.UserTasks.FirstOrDefaultAsync(x => x.TenantId == task.TenantId && x.Id == task.Id, cancellationToken);
        if (existing is null || existing.Revision != expectedRevision)
            return false;
        Copy(task, existing);
        existing.Revision = expectedRevision + 1;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static IQueryable<UserTaskRecord> BuildQuery(UserTasksElsaDbContext dbContext, UserTaskQuery query)
    {
        var records = dbContext.UserTasks.AsNoTracking();
        records = records.Where(x => x.TenantId == query.TenantId);
        if (query.Statuses.Count > 0)
        {
            var statuses = query.Statuses.ToArray();
            records = records.Where(x => statuses.Contains(x.Status));
        }
        if (query.OnlyOverdue)
            records = records.Where(x => x.IsOverdue);
        if (query.OnlyWithoutDueDate)
            records = records.Where(x => x.DueAt == null);
        if (!string.IsNullOrWhiteSpace(query.TaskType))
            records = records.Where(x => x.TaskType == query.TaskType);
        if (query.PriorityFrom is not null)
            records = records.Where(x => x.Priority >= query.PriorityFrom);
        if (query.PriorityTo is not null)
            records = records.Where(x => x.Priority <= query.PriorityTo);
        if (query.DueFrom is not null)
            records = records.Where(x => x.DueAt >= query.DueFrom);
        if (query.DueTo is not null)
            records = records.Where(x => x.DueAt <= query.DueTo);
        if (!string.IsNullOrWhiteSpace(query.WorkflowDefinitionId))
            records = records.Where(x => x.WorkflowDefinitionId == query.WorkflowDefinitionId);
        if (!string.IsNullOrWhiteSpace(query.WorkflowInstanceId))
            records = records.Where(x => x.WorkflowInstanceId == query.WorkflowInstanceId);
        if (!string.IsNullOrWhiteSpace(query.Reference))
            records = records.Where(x => x.Reference == query.Reference);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            records = records.Where(x => x.Title.Contains(search) || (x.Summary != null && x.Summary.Contains(search)) || (x.Reference != null && x.Reference.Contains(search)) || (x.TaskType != null && x.TaskType.Contains(search)));
        }
        if (query.Scope is { } requestedScope &&
            (!string.Equals(requestedScope.TenantId, query.TenantId, StringComparison.Ordinal) ||
             !string.Equals(requestedScope.Subject.TenantId, query.TenantId, StringComparison.Ordinal) ||
             requestedScope.Groups.Any(group => !string.Equals(group.TenantId, query.TenantId, StringComparison.Ordinal))))
            return records.Where(_ => false);

        if (query.Scope is not { } scope)
            return records;

        var subject = scope.Subject;
        var subjectType = subject.Type.ToString();
        var groupKeys = scope.Groups.Select(GetParticipantKey).Distinct(StringComparer.Ordinal).ToArray();
        var tenantId = query.TenantId;

        if (scope.ExcludeBlocking)
            records = records.Where(task => task.HealthSeverity != UserTaskHealthSeverity.Blocking);

        // Manager-only scopes were already rejected by the policy for non-managers. Reaching them here means
        // the caller manages the tenant, so the tenant predicate above is the whole authorization.
        if (scope.RequiresManager)
        {
            if (!scope.IsManager)
                return records.Where(_ => false);
            if (scope.Kind == UserTaskQueryScopeKind.NeedsAttention)
            {
                records = records.Where(task =>
                    task.HealthSeverity == UserTaskHealthSeverity.Blocking
                    || task.IsOverdue
                    || (task.AssigneeId == null && task.Status != UserTaskStatus.Completed && task.Status != UserTaskStatus.TimedOut && task.Status != UserTaskStatus.Cancelled)
                    || task.Status == UserTaskStatus.Completing
                    || task.Status == UserTaskStatus.TimingOut
                    || task.Status == UserTaskStatus.Cancelling);
            }
            return records;
        }

        // Correlated predicates are applied in the query path so tenant, eligibility, and exclusion are
        // evaluated before totals, cursors, and page limits — an unauthorized row can never reach the page.
        return scope.Kind switch
        {
            UserTaskQueryScopeKind.Assigned => records.Where(task =>
                task.AssigneeProvider == subject.Provider && task.AssigneeType == subjectType && task.AssigneeId == subject.Id),

            UserTaskQueryScopeKind.Available => records.Where(task =>
                task.AssigneeId == null
                && task.Status != UserTaskStatus.Completed && task.Status != UserTaskStatus.TimedOut && task.Status != UserTaskStatus.Cancelled
                && !dbContext.UserTaskExclusions.Any(exclusion =>
                    exclusion.TenantId == tenantId && exclusion.TaskId == task.Id &&
                    exclusion.ParticipantType == UserTaskParticipantType.User && exclusion.Provider == subject.Provider && exclusion.ParticipantId == subject.Id)
                && (dbContext.UserTaskCandidates.Any(candidate =>
                        candidate.TenantId == tenantId && candidate.TaskId == task.Id &&
                        candidate.ParticipantType == UserTaskParticipantType.User && candidate.Provider == subject.Provider && candidate.ParticipantId == subject.Id)
                    || (groupKeys.Length > 0 && dbContext.UserTaskCandidates.Any(candidate =>
                        candidate.TenantId == tenantId && candidate.TaskId == task.Id &&
                        candidate.ParticipantType == UserTaskParticipantType.Group && groupKeys.Contains(candidate.ParticipantKey)))
                    || dbContext.UserTaskSnapshotMembers.Any(member =>
                        member.TenantId == tenantId && member.TaskId == task.Id &&
                        ((member.ParticipantType == UserTaskParticipantType.User && member.Provider == subject.Provider && member.ParticipantId == subject.Id)
                         || (member.ParticipantType == UserTaskParticipantType.Group && groupKeys.Contains(member.ParticipantKey)))))),

            UserTaskQueryScopeKind.History => records.Where(task =>
                (task.Status == UserTaskStatus.Completed || task.Status == UserTaskStatus.TimedOut || task.Status == UserTaskStatus.Cancelled)
                && dbContext.UserTaskEvents.Any(@event =>
                    @event.TenantId == tenantId && @event.TaskId == task.Id &&
                    @event.ActorProvider == subject.Provider && @event.ActorType == subjectType && @event.ActorId == subject.Id)),

            _ => records.Where(_ => false)
        };
    }

    private static IQueryable<UserTaskRecord> ApplyOrdering(IQueryable<UserTaskRecord> records, UserTaskQuery query)
    {
        return query.Sort.ToLowerInvariant() switch
        {
            "due" when query.Descending => records.OrderBy(x => x.DueAt == null).ThenByDescending(x => x.DueAt).ThenBy(x => x.Id),
            "due" => records.OrderBy(x => x.DueAt == null).ThenBy(x => x.DueAt).ThenBy(x => x.Id),
            "priority" when query.Descending => records.OrderByDescending(x => x.Priority).ThenBy(x => x.Id),
            "priority" => records.OrderBy(x => x.Priority).ThenBy(x => x.Id),
            "title" when query.Descending => records.OrderByDescending(x => x.Title).ThenBy(x => x.Id),
            "title" => records.OrderBy(x => x.Title).ThenBy(x => x.Id),
            _ when query.Descending => records.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => records.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
        };
    }

    private static IQueryable<UserTaskRecord> ApplyCursor(IQueryable<UserTaskRecord> records, UserTaskQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Cursor) || !TryReadCursor(query.Cursor, out var cursorValue, out var cursorId))
            return records;

        return query.Sort.ToLowerInvariant() switch
        {
            "priority" when int.TryParse(cursorValue, out var priority) => query.Descending
                ? records.Where(x => x.Priority < priority || (x.Priority == priority && string.Compare(x.Id, cursorId) > 0))
                : records.Where(x => x.Priority > priority || (x.Priority == priority && string.Compare(x.Id, cursorId) > 0)),
            "title" => query.Descending
                ? records.Where(x => string.Compare(x.Title, cursorValue) < 0 || (x.Title == cursorValue && string.Compare(x.Id, cursorId) > 0))
                : records.Where(x => string.Compare(x.Title, cursorValue) > 0 || (x.Title == cursorValue && string.Compare(x.Id, cursorId) > 0)),
            "due" when cursorValue == "~null" => records.Where(x => x.DueAt == null && string.Compare(x.Id, cursorId) > 0),
            "due" when DateTimeOffset.TryParse(cursorValue, out var dueAt) => query.Descending
                ? records.Where(x => x.DueAt == null || x.DueAt < dueAt || (x.DueAt == dueAt && string.Compare(x.Id, cursorId) > 0))
                : records.Where(x => x.DueAt == null || x.DueAt > dueAt || (x.DueAt == dueAt && string.Compare(x.Id, cursorId) > 0)),
            _ when DateTimeOffset.TryParse(cursorValue, out var createdAt) => query.Descending
                ? records.Where(x => x.CreatedAt < createdAt || (x.CreatedAt == createdAt && string.Compare(x.Id, cursorId) > 0))
                : records.Where(x => x.CreatedAt > createdAt || (x.CreatedAt == createdAt && string.Compare(x.Id, cursorId) > 0)),
            _ => records
        };
    }

    private static string CreateCursor(UserTaskRecord record, string sort)
    {
        var value = sort.ToLowerInvariant() switch
        {
            "priority" => record.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "title" => record.Title,
            "due" => record.DueAt?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "~null",
            _ => record.CreatedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        };
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new[] { value, record.Id }, JsonOptions))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryReadCursor(string cursor, out string value, out string id)
    {
        value = id = "";
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/') + new string('=', (4 - cursor.Length % 4) % 4);
            var parts = JsonSerializer.Deserialize<string[]>(Convert.FromBase64String(padded), JsonOptions);
            if (parts is not [var cursorValue, var cursorId] || string.IsNullOrWhiteSpace(cursorId))
                return false;
            value = cursorValue;
            id = cursorId;
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

    private static async Task<UserTask> LoadAggregateAsync(UserTasksElsaDbContext dbContext, UserTaskRecord record, CancellationToken cancellationToken)
    {
        return (await LoadAggregatesAsync(dbContext, [record], cancellationToken))[0];
    }

    private static async Task<List<UserTask>> LoadAggregatesAsync(UserTasksElsaDbContext dbContext, IReadOnlyCollection<UserTaskRecord> records, CancellationToken cancellationToken)
    {
        var tasks = records.Select(ToModel).ToList();
        if (tasks.Count == 0)
            return tasks;

        var tenantId = records.First().TenantId;
        var taskIds = tasks.Select(x => x.Id).ToArray();
        var taskById = tasks.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var candidates = await dbContext.UserTaskCandidates.AsNoTracking().Where(x => x.TenantId == tenantId && taskIds.Contains(x.TaskId)).ToListAsync(cancellationToken);
        foreach (var (task, group) in GroupByLoadedTask(candidates, x => x.TaskId, taskById))
        {
            task.CandidateUsers = group.Where(x => x.ParticipantType == UserTaskParticipantType.User).Select(ToParticipant).ToList();
            task.CandidateGroups = group.Where(x => x.ParticipantType == UserTaskParticipantType.Group).Select(ToParticipant).ToList();
        }

        var snapshots = await dbContext.UserTaskSnapshotMembers.AsNoTracking().Where(x => x.TenantId == tenantId && taskIds.Contains(x.TaskId)).ToListAsync(cancellationToken);
        foreach (var (task, group) in GroupByLoadedTask(snapshots, x => x.TaskId, taskById))
        {
            task.SnapshotMembers = group.Where(x => x.ParticipantType == UserTaskParticipantType.User).Select(ToParticipant).ToList();
            task.SnapshotGroups = group.Where(x => x.ParticipantType == UserTaskParticipantType.Group).Select(ToParticipant).ToList();
        }

        var exclusions = await dbContext.UserTaskExclusions.AsNoTracking().Where(x => x.TenantId == tenantId && taskIds.Contains(x.TaskId)).ToListAsync(cancellationToken);
        foreach (var (task, group) in GroupByLoadedTask(exclusions, x => x.TaskId, taskById))
            task.ExcludedUsers = group.Select(ToParticipant).ToList();

        var events = await dbContext.UserTaskEvents.AsNoTracking().Where(x => x.TenantId == tenantId && taskIds.Contains(x.TaskId)).OrderBy(x => x.Revision).ToListAsync(cancellationToken);
        foreach (var (task, group) in GroupByLoadedTask(events, x => x.TaskId, taskById))
            task.Events = group.Select(ToEvent).ToList();

        var operations = await dbContext.UserTaskOperations.AsNoTracking().Where(x => x.TenantId == tenantId && taskIds.Contains(x.TaskId)).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        foreach (var (task, group) in GroupByLoadedTask(operations, x => x.TaskId, taskById))
            task.Operations = group.Select(ToOperation).ToList();

        var invitations = await dbContext.UserTaskInvitations.AsNoTracking().Where(x => x.TenantId == tenantId && taskIds.Contains(x.TaskId)).OrderBy(x => x.IssuedAt).ToListAsync(cancellationToken);
        foreach (var (task, group) in GroupByLoadedTask(invitations, x => x.TaskId, taskById))
            task.Invitations = group.Select(ToInvitation).ToList();

        return tasks;
    }

    /// <summary>
    /// Pairs each group of child rows with the task it belongs to, dropping groups whose task is not on the
    /// current page. The filter is explicit and each group still costs a single dictionary probe.
    /// </summary>
    private static IEnumerable<(UserTask Task, IGrouping<string, TRow> Rows)> GroupByLoadedTask<TRow>(
        IEnumerable<TRow> rows, Func<TRow, string> taskIdSelector, Dictionary<string, UserTask> taskById) =>
        rows.GroupBy(taskIdSelector)
            .Select(group => (Task: taskById.GetValueOrDefault(group.Key), Rows: group))
            .Where(pair => pair.Task is not null)
            .Select(pair => (pair.Task!, pair.Rows));

    private static UserTask ToModel(UserTaskRecord record) => new()
    {
        Id = record.Id,
        TenantId = record.TenantId,
        WorkflowDefinitionId = record.WorkflowDefinitionId,
        WorkflowDefinitionName = record.WorkflowDefinitionName,
        WorkflowDefinitionVersion = record.WorkflowDefinitionVersion,
        WorkflowInstanceId = record.WorkflowInstanceId,
        WorkflowInstanceReference = record.WorkflowInstanceReference,
        ActivityInstanceId = record.ActivityInstanceId,
        BookmarkId = record.BookmarkId,
        MaterializationKey = record.MaterializationKey,
        Title = record.Title,
        Summary = record.Summary,
        Reference = record.Reference,
        Tags = Deserialize<HashSet<string>>(record.TagsJson) ?? new(StringComparer.OrdinalIgnoreCase),
        TaskType = record.TaskType,
        Requester = ToParticipant(record.RequesterProvider, record.RequesterType, record.RequesterId, record.RequesterDisplayName, record.TenantId),
        Assignee = ToParticipant(record.AssigneeProvider, record.AssigneeType, record.AssigneeId, record.AssigneeDisplayName, record.TenantId),
        MembershipResolutionMode = record.MembershipResolutionMode ?? UserTaskMembershipResolutionMode.Live,
        AllowManagerExclusionOverride = record.AllowManagerExclusionOverride,
        Priority = record.Priority,
        DueAt = record.DueAt,
        IsOverdue = record.IsOverdue,
        Instructions = Deserialize<string>(record.InstructionsJson),
        TaskData = DeserializeJson(record.TaskDataJson),
        RequestedForm = Deserialize<UserTaskFormReference>(record.FormReferenceJson),
        PinnedForm = Deserialize<ResolvedUserTaskForm>(record.PinnedFormJson),
        Actions = Deserialize<List<UserTaskAction>>(record.ActionsJson) ?? [new UserTaskAction("Complete", "Complete")],
        InvitationDefinitions = Deserialize<List<UserTaskInvitationDefinition>>(record.InvitationDefinitionsJson) ?? [],
        EnableTimeoutOutcome = record.TimeoutEnabled,
        EnableCancellationOutcome = record.CancellationEnabled,
        Status = record.Status,
        HealthSeverity = record.HealthSeverity,
        HealthCode = record.HealthCode,
        HealthMessage = record.HealthMessage,
        CompletionActionKey = record.CompletionActionKey,
        CompletionData = DeserializeJson(record.CompletionDataJson),
        CompletedBy = Deserialize<ParticipantReference>(record.CompletionActorJson),
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        AssignedAt = record.AssignedAt,
        CompletedAt = record.CompletedAt,
        Revision = record.Revision
    };

    private static UserTaskRecord ToRecord(UserTask task)
    {
        var record = new UserTaskRecord { Id = string.IsNullOrWhiteSpace(task.Id) ? Guid.NewGuid().ToString("N") : task.Id };
        Copy(task, record);
        return record;
    }

    private static void Copy(UserTask source, UserTaskRecord target)
    {
        target.Id = source.Id;
        target.TenantId = source.TenantId;
        target.WorkflowDefinitionId = source.WorkflowDefinitionId;
        target.WorkflowDefinitionName = source.WorkflowDefinitionName;
        target.WorkflowDefinitionVersion = source.WorkflowDefinitionVersion;
        target.WorkflowInstanceId = source.WorkflowInstanceId;
        target.WorkflowInstanceReference = source.WorkflowInstanceReference;
        target.ActivityInstanceId = source.ActivityInstanceId;
        target.BookmarkId = source.BookmarkId;
        target.MaterializationKey = source.MaterializationKey;
        target.Title = source.Title;
        target.Summary = source.Summary;
        target.Reference = source.Reference;
        target.TaskType = source.TaskType;
        target.TagsJson = Serialize(source.Tags);
        target.RequesterProvider = source.Requester?.Provider;
        target.RequesterType = source.Requester?.Type.ToString();
        target.RequesterId = source.Requester?.Id;
        target.RequesterDisplayName = source.Requester?.DisplayName;
        target.Priority = source.Priority;
        target.DueAt = source.DueAt;
        target.IsOverdue = source.IsOverdue;
        target.Status = source.Status;
        target.TimeoutEnabled = source.EnableTimeoutOutcome;
        target.CancellationEnabled = source.EnableCancellationOutcome;
        target.AllowManagerExclusionOverride = source.AllowManagerExclusionOverride;
        target.MembershipResolutionMode = source.MembershipResolutionMode;
        target.AssigneeProvider = source.Assignee?.Provider;
        target.AssigneeType = source.Assignee?.Type.ToString();
        target.AssigneeId = source.Assignee?.Id;
        target.AssigneeDisplayName = source.Assignee?.DisplayName;
        target.InstructionsJson = Serialize(source.Instructions);
        target.TaskDataJson = Serialize(source.TaskData);
        target.FormReferenceJson = Serialize(source.RequestedForm);
        target.PinnedFormJson = Serialize(source.PinnedForm);
        target.ActionsJson = Serialize(source.Actions);
        target.InvitationDefinitionsJson = Serialize(source.InvitationDefinitions);
        target.HealthIssuesJson = Serialize(new { source.HealthSeverity, source.HealthCode, source.HealthMessage });
        target.HealthSeverity = source.HealthSeverity;
        target.HealthCode = source.HealthCode;
        target.HealthMessage = source.HealthMessage;
        target.CompletionActionKey = source.CompletionActionKey;
        target.CompletionDataJson = Serialize(source.CompletionData);
        target.CompletionActorJson = Serialize(source.CompletedBy);
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
        target.AssignedAt = source.AssignedAt;
        target.CompletedAt = source.CompletedAt;
        target.Revision = source.Revision;
    }

    private static void Copy(UserTaskRecord source, UserTaskRecord target)
    {
        target.TenantId = source.TenantId;
        target.WorkflowDefinitionId = source.WorkflowDefinitionId;
        target.WorkflowDefinitionName = source.WorkflowDefinitionName;
        target.WorkflowDefinitionVersion = source.WorkflowDefinitionVersion;
        target.WorkflowInstanceId = source.WorkflowInstanceId;
        target.WorkflowInstanceReference = source.WorkflowInstanceReference;
        target.ActivityInstanceId = source.ActivityInstanceId;
        target.BookmarkId = source.BookmarkId;
        target.MaterializationKey = source.MaterializationKey;
        target.Title = source.Title;
        target.Summary = source.Summary;
        target.Reference = source.Reference;
        target.TaskType = source.TaskType;
        target.TagsJson = source.TagsJson;
        target.RequesterProvider = source.RequesterProvider;
        target.RequesterType = source.RequesterType;
        target.RequesterId = source.RequesterId;
        target.RequesterDisplayName = source.RequesterDisplayName;
        target.Priority = source.Priority;
        target.DueAt = source.DueAt;
        target.IsOverdue = source.IsOverdue;
        target.Status = source.Status;
        target.TimeoutEnabled = source.TimeoutEnabled;
        target.CancellationEnabled = source.CancellationEnabled;
        target.AllowManagerExclusionOverride = source.AllowManagerExclusionOverride;
        target.MembershipResolutionMode = source.MembershipResolutionMode;
        target.AssigneeProvider = source.AssigneeProvider;
        target.AssigneeType = source.AssigneeType;
        target.AssigneeId = source.AssigneeId;
        target.AssigneeDisplayName = source.AssigneeDisplayName;
        target.InstructionsJson = source.InstructionsJson;
        target.TaskDataJson = source.TaskDataJson;
        target.FormReferenceJson = source.FormReferenceJson;
        target.PinnedFormJson = source.PinnedFormJson;
        target.ActionsJson = source.ActionsJson;
        target.InvitationDefinitionsJson = source.InvitationDefinitionsJson;
        target.HealthIssuesJson = source.HealthIssuesJson;
        target.HealthSeverity = source.HealthSeverity;
        target.HealthCode = source.HealthCode;
        target.HealthMessage = source.HealthMessage;
        target.CompletionActionKey = source.CompletionActionKey;
        target.CompletionDataJson = source.CompletionDataJson;
        target.CompletionActorJson = source.CompletionActorJson;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
        target.AssignedAt = source.AssignedAt;
        target.CompletedAt = source.CompletedAt;
        target.CreatedFromBookmarkRevision = source.CreatedFromBookmarkRevision;
    }

    private static async Task ReplaceChildrenAsync(UserTasksElsaDbContext dbContext, UserTask task, CancellationToken cancellationToken)
    {
        dbContext.UserTaskCandidates.RemoveRange(await dbContext.UserTaskCandidates.Where(x => x.TenantId == task.TenantId && x.TaskId == task.Id).ToListAsync(cancellationToken));
        dbContext.UserTaskSnapshotMembers.RemoveRange(await dbContext.UserTaskSnapshotMembers.Where(x => x.TenantId == task.TenantId && x.TaskId == task.Id).ToListAsync(cancellationToken));
        dbContext.UserTaskExclusions.RemoveRange(await dbContext.UserTaskExclusions.Where(x => x.TenantId == task.TenantId && x.TaskId == task.Id).ToListAsync(cancellationToken));
        var existingEvents = await dbContext.UserTaskEvents.Where(x => x.TenantId == task.TenantId && x.TaskId == task.Id).ToListAsync(cancellationToken);
        var existingEventIds = existingEvents.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var existingEventRevisions = existingEvents.Select(x => x.Revision).ToHashSet();
        dbContext.UserTaskEvents.AddRange(task.Events.Where(x => !existingEventIds.Contains(x.Id) && !existingEventRevisions.Contains(x.Revision)).Select(ToEventRecord));

        var existingOperations = await dbContext.UserTaskOperations.Where(x => x.TenantId == task.TenantId && x.TaskId == task.Id).ToListAsync(cancellationToken);
        var existingOperationIds = existingOperations.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var operation in task.Operations)
        {
            var entity = existingOperations.FirstOrDefault(x => x.Id == operation.Id || x.OperationId == operation.OperationId);
            if (entity is null)
            {
                if (!existingOperationIds.Contains(operation.Id))
                    dbContext.UserTaskOperations.Add(ToOperationRecord(operation));
                continue;
            }

            entity.Kind = operation.Kind.ToString();
            entity.ExpectedRevision = operation.ExpectedRevision;
            entity.RequestHash = operation.RequestHash;
            entity.Status = operation.Status switch
            {
                UserTaskOperationStatus.Completed => UserTaskPersistenceOperationStatus.Completed,
                UserTaskOperationStatus.Failed => UserTaskPersistenceOperationStatus.Failed,
                _ => UserTaskPersistenceOperationStatus.Enqueued
            };
            entity.UpdatedAt = operation.UpdatedAt;
            entity.ActionKey = operation.ActionKey;
            entity.ProtectedPayloadJson = Serialize(operation.Data);
            entity.ErrorCode = operation.ErrorCode;
        }

        var existingInvitations = await dbContext.UserTaskInvitations.Where(x => x.TenantId == task.TenantId && x.TaskId == task.Id).ToListAsync(cancellationToken);
        var existingInvitationIds = existingInvitations.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var invitation in task.Invitations)
        {
            var entity = existingInvitations.FirstOrDefault(x => x.Id == invitation.Id);
            if (entity is null)
            {
                if (!existingInvitationIds.Contains(invitation.Id))
                    dbContext.UserTaskInvitations.Add(ToInvitationRecord(invitation));
                continue;
            }

            entity.Status = invitation.Status;
            entity.VerifiedAt = invitation.VerifiedAt;
            entity.ConsumedAt = invitation.ConsumedAt;
            entity.RevokedAt = invitation.RevokedAt;
        }
        dbContext.UserTaskCandidates.AddRange(task.CandidateUsers.Select(x => ToCandidate(task, x, UserTaskPersistenceCandidateSource.DirectUser)));
        dbContext.UserTaskCandidates.AddRange(task.CandidateGroups.Select(x => ToCandidate(task, x, UserTaskPersistenceCandidateSource.DirectGroup)));
        dbContext.UserTaskSnapshotMembers.AddRange(task.SnapshotMembers.Select(x => ToSnapshot(task, x)));
        dbContext.UserTaskSnapshotMembers.AddRange(task.SnapshotGroups.Select(x => ToSnapshot(task, x)));
        dbContext.UserTaskExclusions.AddRange(task.ExcludedUsers.Select(x => ToExclusion(task, x)));
    }

    private static UserTaskCandidateRecord ToCandidate(UserTask task, ParticipantReference participant, UserTaskPersistenceCandidateSource source) => new()
    {
        TenantId = task.TenantId, TaskId = task.Id, Provider = participant.Provider, ParticipantKey = GetParticipantKey(participant), ParticipantType = participant.Type,
        ParticipantId = participant.Id, DisplayName = participant.DisplayName, Source = source
    };

    private static UserTaskSnapshotMemberRecord ToSnapshot(UserTask task, ParticipantReference participant) => new()
    {
        TenantId = task.TenantId, TaskId = task.Id, Provider = participant.Provider, ParticipantKey = GetParticipantKey(participant), ParticipantType = participant.Type,
        ParticipantId = participant.Id, CreatedAt = task.CreatedAt
    };

    private static UserTaskExclusionRecord ToExclusion(UserTask task, ParticipantReference participant) => new()
    {
        TenantId = task.TenantId, TaskId = task.Id, Provider = participant.Provider, ParticipantKey = GetParticipantKey(participant), ParticipantType = participant.Type,
        ParticipantId = participant.Id, CreatedAt = task.CreatedAt
    };

    private static ParticipantReference ToParticipant(UserTaskCandidateRecord row) => new(row.TenantId, row.Provider, row.ParticipantType, row.ParticipantId, row.DisplayName);
    private static ParticipantReference ToParticipant(UserTaskSnapshotMemberRecord row) => new(row.TenantId, row.Provider, row.ParticipantType, row.ParticipantId);
    private static ParticipantReference ToParticipant(UserTaskExclusionRecord row) => new(row.TenantId, row.Provider, row.ParticipantType, row.ParticipantId);

    private static ParticipantReference? ToParticipant(string? provider, string? type, string? id, string? displayName, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(id) || !Enum.TryParse<UserTaskParticipantType>(type, true, out var participantType))
            return null;
        return new ParticipantReference(tenantId, provider, participantType, id, displayName);
    }

    private static string GetParticipantKey(ParticipantReference participant) => $"{participant.Provider}|{participant.Type}|{participant.Id}";

    private static UserTaskEvent ToEvent(UserTaskEventRecord row) => new(row.Id, row.TenantId, row.TaskId, row.Revision, row.EventType, row.OccurredAt, Deserialize<ParticipantReference>(row.ActorJson) ?? ToParticipant(row.ActorProvider, row.ActorType, row.ActorId, null, row.TenantId), row.OperationId, row.Reason, Deserialize<IReadOnlyDictionary<string, object?>>(row.MetadataJson));

    private static UserTaskEventRecord ToEventRecord(UserTaskEvent value) => new()
    {
        Id = value.Id, TenantId = value.TenantId, TaskId = value.TaskId, Revision = value.Revision, EventType = value.EventType,
        OccurredAt = value.OccurredAt, ActorProvider = value.Actor?.Provider, ActorType = value.Actor?.Type.ToString(), ActorId = value.Actor?.Id,
        ActorJson = Serialize(value.Actor), OperationId = value.OperationId, Reason = value.Reason,
        MetadataJson = Serialize(value.Metadata ?? new Dictionary<string, object?>())
    };

    private static UserTaskOperation ToOperation(UserTaskOperationRecord row) => new(
        row.Id, row.TenantId, row.TaskId, row.OperationId,
        Enum.TryParse<UserTaskOperationKind>(row.Kind, true, out var kind) ? kind : UserTaskOperationKind.Claim,
        row.ExpectedRevision, row.RequestHash,
        row.Status switch
        {
            UserTaskPersistenceOperationStatus.Completed => UserTaskOperationStatus.Completed,
            UserTaskPersistenceOperationStatus.Failed => UserTaskOperationStatus.Failed,
            _ => UserTaskOperationStatus.Accepted
        },
        row.CreatedAt, row.UpdatedAt, row.ActionKey, DeserializeJson(row.ProtectedPayloadJson), row.ErrorCode);

    private static UserTaskOperationRecord ToOperationRecord(UserTaskOperation value) => new()
    {
        Id = value.Id, TenantId = value.TenantId, TaskId = value.TaskId, OperationId = value.OperationId, Kind = value.Kind.ToString(),
        ExpectedRevision = value.ExpectedRevision, RequestHash = value.RequestHash,
        Status = value.Status switch
        {
            UserTaskOperationStatus.Completed => UserTaskPersistenceOperationStatus.Completed,
            UserTaskOperationStatus.Failed => UserTaskPersistenceOperationStatus.Failed,
            _ => UserTaskPersistenceOperationStatus.Enqueued
        },
        CreatedAt = value.CreatedAt, UpdatedAt = value.UpdatedAt, ActionKey = value.ActionKey, ProtectedPayloadJson = Serialize(value.Data), ErrorCode = value.ErrorCode
    };

    private static UserTaskInvitation ToInvitation(UserTaskInvitationRecord row) => new(
        row.Id, row.TenantId, row.TaskId, Deserialize<string>(row.RecipientJson), row.TokenHash, row.Status,
        row.IssuedAt, row.ExpiresAt, row.VerifierProvider, row.VerifiedAt, row.ConsumedAt, row.RevokedAt, row.SiblingGroupId)
    {
        AllowedActions = Deserialize<List<string>>(row.AllowedActionsJson) ?? []
    };

    private static UserTaskInvitationRecord ToInvitationRecord(UserTaskInvitation value) => new()
    {
        Id = value.Id, TenantId = value.TenantId, TaskId = value.TaskId, RecipientJson = Serialize(value.Recipient), TokenHash = value.TokenHash,
        VerifierProvider = value.VerifierName ?? "default", Status = value.Status, IssuedAt = value.IssuedAt, ExpiresAt = value.ExpiresAt,
        VerifiedAt = value.VerifiedAt, ConsumedAt = value.ConsumedAt, RevokedAt = value.RevokedAt, SiblingGroupId = value.SiblingGroupId,
        AllowedActionsJson = Serialize(value.AllowedActions)
    };

    private static async Task<bool> ExistsByMaterializationKeyAsync(UserTasksElsaDbContext dbContext, string tenantId, string key, CancellationToken cancellationToken) => await dbContext.UserTasks.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.MaterializationKey == key, cancellationToken);

    private static void EnsureExpectedRevision(UserTaskRecord? existing, string taskId, int expectedRevision)
    {
        if (existing is null)
            throw new KeyNotFoundException($"User task '{taskId}' was not found.");
        if (existing.Revision != expectedRevision)
            throw new UserTaskRevisionConflictException(taskId, expectedRevision);
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    private static string Serialize(JsonElement? value) => !value.HasValue || value.Value.ValueKind == JsonValueKind.Undefined ? "null" : value.Value.GetRawText();
    private static T? Deserialize<T>(string? value) => string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ? default : JsonSerializer.Deserialize<T>(value, JsonOptions);
    private static JsonElement? DeserializeJson(string? value) => Deserialize<JsonElement>(value);
}
