using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

/// <summary>
/// Shared arrangement for every conformance class.
///
/// The stores are shared for the whole provider collection so a container-backed provider is migrated once,
/// and each test isolates itself with its own tenant instead. Every contract except the deliberately
/// tenant-agnostic invitation-hash lookup is tenant-scoped, so this is isolation, not a shortcut.
/// </summary>
public abstract class UserTaskConformanceTestBase(UserTaskStoreFixture fixture)
{
    private int _sequence;

    protected UserTaskStoreFixture Fixture { get; } = fixture;
    protected IUserTaskRepository Repository => Fixture.Repository;
    protected UserTaskStoreFixture.TestClock Clock => Fixture.Clock;

    /// <summary>This test's private tenant. Never reused, so a shared store still gives per-test isolation.</summary>
    protected string TenantId { get; } = $"tenant-{Guid.NewGuid():N}";

    protected Task ActivateAsync() => Fixture.ActivateAsync();

    protected ParticipantReference Subject(string id = "user-1") => new(TenantId, "oidc", UserTaskParticipantType.User, id);

    protected ParticipantReference Group(string id) => new(TenantId, "oidc", UserTaskParticipantType.Group, id);

    /// <summary>Builds a task in this test's tenant with store-unique keys, ready for <c>AddProjectionAsync</c>.</summary>
    protected UserTask CreateTask(
        ParticipantReference? candidate = null,
        string title = "Approve request",
        int priority = 50,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? createdAt = null)
    {
        var ordinal = ++_sequence;
        var created = createdAt ?? Clock.UtcNow.AddMinutes(ordinal);
        return new()
        {
            // Ordinal-prefixed so the identity tiebreaker is predictable and a failure is readable.
            Id = $"task-{ordinal:D4}-{Guid.NewGuid():N}",
            TenantId = TenantId,
            WorkflowDefinitionId = "definition-1",
            WorkflowInstanceId = "instance-1",
            ActivityInstanceId = "activity-1",
            BookmarkId = $"bookmark-{Guid.NewGuid():N}",
            MaterializationKey = $"materialization-{Guid.NewGuid():N}",
            Title = title,
            Summary = "Review the request",
            Tags = ["finance"],
            Priority = priority,
            DueAt = dueAt,
            CandidateUsers = candidate is null ? [] : [candidate],
            InvitationDefinitions = [new UserTaskInvitationDefinition("bearer", ["Complete"], BearerOnly: true)],
            CreatedAt = created,
            UpdatedAt = created
        };
    }

    /// <summary>Projects a task and returns the stored copy, so a test starts from committed state in one line.</summary>
    protected async Task<UserTask> ProjectAsync(UserTask task)
    {
        await Repository.AddProjectionAsync(task);
        return await Repository.GetAsync(task.TenantId, task.Id)
               ?? throw new InvalidOperationException($"The projection of '{task.Id}' was not readable afterwards.");
    }

    protected async Task<UserTask> GetAsync(string taskId) =>
        await Repository.GetAsync(TenantId, taskId) ?? throw new InvalidOperationException($"Task '{taskId}' was not found.");

    protected UserTaskQuery Query(
        UserTaskQueryScopeKind kind = UserTaskQueryScopeKind.Available,
        ParticipantReference? subject = null,
        int limit = 50,
        string sort = "created",
        bool descending = false,
        bool includeTotalCount = false,
        string? cursor = null) => new()
    {
        TenantId = TenantId,
        Limit = limit,
        Sort = sort,
        Descending = descending,
        IncludeTotalCount = includeTotalCount,
        Cursor = cursor,
        Scope = new(TenantId, subject ?? Subject(), [], Kind: kind)
    };

    /// <summary>Pages a query to exhaustion through its cursors and returns the ids in the order seen.</summary>
    protected async Task<IReadOnlyList<string>> PageThroughAsync(UserTaskQuery query, int pageSize)
    {
        var seen = new List<string>();
        string? cursor = null;
        for (var page = 0; page < 100; page++)
        {
            var result = await Repository.QueryAsync(query with { Limit = pageSize, Cursor = cursor });
            seen.AddRange(result.Items.Select(x => x.Id));
            if (result.NextCursor is null)
                return seen;
            cursor = result.NextCursor;
        }

        throw new InvalidOperationException("The cursor never terminated; paging looped past 100 pages.");
    }
}
