using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Services;
using Elsa.Workflows;
using Microsoft.Extensions.Options;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.EFCore;
using Elsa.UserTasks.Persistence.EFCore.Repositories;
using Elsa.UserTasks.Persistence.EFCore.Sqlite.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.UserTasks.Persistence.EFCore.UnitTests;

public sealed class EFCoreUserTaskRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Join(Path.GetTempPath(), $"elsa-user-tasks-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider serviceProvider;

    public EFCoreUserTaskRepositoryTests()
    {
        var services = new ServiceCollection();
        services.AddSqliteEntityModelCreatingHandlers();
        services.AddDbContextFactory<UserTasksElsaDbContext>(builder =>
            builder.UseElsaSqlite(typeof(SqliteUserTasksPersistenceFeatureExtensions).Assembly, $"Data Source={databasePath}"));
        services.AddScoped<Store<UserTasksElsaDbContext, UserTaskRecord>>();
        services.AddScoped<EFCoreUserTaskRepository>();
        serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserTasksElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        if (File.Exists(databasePath))
            File.Delete(databasePath);
    }

    [Fact]
    public async Task RoundTrip_PreservesProtectedAggregateAndAppendOnlyHistory()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreUserTaskRepository>();
        var createdAt = DateTimeOffset.UtcNow;
        var actor = new ParticipantReference("tenant-1", "directory", UserTaskParticipantType.User, "u1", "Alice");
        var task = CreateTask(createdAt, actor);
        task.Events.Add(new UserTaskEvent("event-1", task.TenantId, task.Id, 1, "Created", createdAt, actor));
        task.Operations.Add(new UserTaskOperation("operation-1", task.TenantId, task.Id, "op-1", UserTaskOperationKind.Claim, 1, "hash", UserTaskOperationStatus.Accepted, createdAt, createdAt, "Complete"));

        await repository.AddProjectionAsync(task);
        var reloaded = await repository.GetAsync(task.TenantId, task.Id);
        Assert.NotNull(reloaded);
        var loaded = reloaded!;
        Assert.Single(loaded.CandidateUsers);
        Assert.Equal("alice", loaded.CandidateUsers[0].DisplayName?.ToLowerInvariant());
        Assert.Single(loaded.InvitationDefinitions);
        Assert.Equal("Complete", loaded.Operations.Single().ActionKey);

        loaded.Events.Add(new UserTaskEvent("event-2", task.TenantId, task.Id, 2, "Claimed", createdAt.AddMinutes(1), actor));
        loaded.Operations[0] = loaded.Operations[0] with { Status = UserTaskOperationStatus.Completed, ErrorCode = "none", UpdatedAt = createdAt.AddMinutes(1) };
        await repository.SaveAsync(loaded, 1);

        var saved = await repository.GetAsync(task.TenantId, task.Id);
        Assert.NotNull(saved);
        var persisted = saved!;
        Assert.Equal(2, persisted.Events.Count);
        Assert.Equal(2, persisted.Revision);
        Assert.Equal(UserTaskOperationStatus.Completed, persisted.Operations.Single().Status);
        Assert.Equal("Complete", persisted.Operations.Single().ActionKey);
        Assert.Equal("none", persisted.Operations.Single().ErrorCode);
    }

    [Fact]
    public async Task QueryScopeAndConcurrency_AreAppliedBeforePaging()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreUserTaskRepository>();
        var actor = new ParticipantReference("tenant-1", "directory", UserTaskParticipantType.User, "u1");
        var task = CreateTask(DateTimeOffset.UtcNow, actor);
        await repository.AddProjectionAsync(task);
        var pagingSecondTask = CreateTask(DateTimeOffset.UtcNow.AddSeconds(1), actor);
        await repository.AddProjectionAsync(pagingSecondTask);

        var query = await repository.QueryAsync(Query(task.TenantId, actor, limit: 1, includeTotalCount: true));
        Assert.Single(query.Items);
        Assert.Single(query.Items.Single().CandidateUsers);
        Assert.Equal(2, query.TotalCount);
        Assert.NotNull(query.NextCursor);

        var finalPage = await repository.QueryAsync(Query(task.TenantId, actor, limit: 1) with { Cursor = query.NextCursor });
        Assert.Single(finalPage.Items);
        Assert.Null(finalPage.NextCursor);

        // The subject holds no assignment yet, so the assigned scope must be empty even though the
        // available scope returns both rows.
        var assignedScope = await repository.QueryAsync(Query(task.TenantId, actor, UserTaskQueryScopeKind.Assigned, includeTotalCount: true));
        Assert.Empty(assignedScope.Items);
        Assert.Equal(0, assignedScope.TotalCount);

        var crossTenantScope = await repository.QueryAsync(new UserTaskQuery
        {
            TenantId = task.TenantId,
            IncludeTotalCount = true,
            Scope = new UserTaskQueryScope("other-tenant", actor with { TenantId = "other-tenant" }, [], Kind: UserTaskQueryScopeKind.Available)
        });
        Assert.Empty(crossTenantScope.Items);
        Assert.Equal(0, crossTenantScope.TotalCount);

        var first = await repository.GetAsync(task.TenantId, task.Id);
        var second = await repository.GetAsync(task.TenantId, task.Id);
        Assert.NotNull(first);
        Assert.NotNull(second);
        var firstTask = first!;
        var secondTask = second!;
        firstTask.Status = UserTaskStatus.Assigned;
        await repository.SaveAsync(firstTask, 1);
        secondTask.Status = UserTaskStatus.Completed;
        await Assert.ThrowsAsync<UserTaskRevisionConflictException>(() => repository.SaveAsync(secondTask, 1));

        var excludedTask = CreateTask(DateTimeOffset.UtcNow.AddSeconds(2), actor);
        excludedTask.ExcludedUsers = [actor];
        await repository.AddProjectionAsync(excludedTask);
        var excludedQuery = await repository.QueryAsync(Query(task.TenantId, actor, includeTotalCount: true));
        Assert.DoesNotContain(excludedQuery.Items, x => x.Id == excludedTask.Id);
        // Three tasks exist; the excluded one is filtered in the query path, so it is absent from the
        // total as well rather than merely being hidden on the page.
        Assert.Equal(2, excludedQuery.TotalCount);
    }

    [Fact]
    public async Task InvitationLookup_ResolvesTheOwningTaskFromATokenHashWithoutATenantHint()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreUserTaskRepository>();
        var actor = new ParticipantReference("tenant-1", "directory", UserTaskParticipantType.User, "u1");
        var task = CreateTask(DateTimeOffset.UtcNow, actor);
        task.Invitations.Add(new("invitation-1", task.TenantId, task.Id, "guest@example.com", "HASH-1",
            UserTaskInvitationStatus.Pending, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), "email-code")
        {
            AllowedActions = ["Complete"]
        });
        await repository.AddProjectionAsync(task);

        var match = await repository.FindByInvitationTokenHashAsync("HASH-1");

        Assert.NotNull(match);
        var resolved = match!.Value;
        Assert.Equal(task.Id, resolved.Task.Id);
        Assert.Equal("Complete", Assert.Single(resolved.Invitation.AllowedActions));
        Assert.Null(await repository.FindByInvitationTokenHashAsync("HASH-UNKNOWN"));
    }

    [Fact]
    public async Task Manager_ReturnsARevisionConflictWhenAConcurrentEditWinsAgainstTheRealStore()
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreUserTaskRepository>();
        var clock = new FixedClock();
        var manager = new DefaultUserTaskManager(repository, new DefaultUserTaskAccessPolicy(), [],
            new NoOpResumer(), new NoOpSink(), new SequentialIdentityGenerator(), clock,
            Microsoft.Extensions.Options.Options.Create(new UserTasksOptions()));

        var subject = new ParticipantReference("tenant-1", "directory", UserTaskParticipantType.User, "u1");
        var actor = new UserTaskActor(subject, [])
        {
            Permissions = new HashSet<string>(["read:user-tasks", "claim:user-tasks", "complete:user-tasks"], StringComparer.OrdinalIgnoreCase)
        };
        var task = CreateTask(DateTimeOffset.UtcNow, subject);
        await repository.AddProjectionAsync(task);

        // Someone else moves the task on, so the revision the caller holds is now stale.
        var concurrent = (await repository.GetAsync(task.TenantId, task.Id))!;
        concurrent.Priority = 90;
        await repository.SaveAsync(concurrent, 1);

        // The store raises its own concurrency exception here. It must surface as the documented
        // conflict result rather than escaping to the unhandled-error middleware.
        var result = await manager.ClaimAsync(task.TenantId, task.Id, new(1, "claim-1"), actor);

        Assert.False(result.Accepted);
        Assert.Equal("revision-conflict", result.ConflictCode);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.UtcNow;
    }

    private sealed class SequentialIdentityGenerator : IIdentityGenerator
    {
        private int counter;
        public string GenerateId() => $"id-{Interlocked.Increment(ref counter)}";
    }

    private sealed class NoOpResumer : IUserTaskWorkflowResumer
    {
        public Task ResumeAsync(UserTask task, UserTaskStimulus stimulus, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpSink : IUserTaskNotificationSink
    {
        public Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static UserTaskQuery Query(string tenantId, ParticipantReference subject,
        UserTaskQueryScopeKind kind = UserTaskQueryScopeKind.Available, int limit = 50, bool includeTotalCount = false) => new()
    {
        TenantId = tenantId,
        Limit = limit,
        IncludeTotalCount = includeTotalCount,
        Scope = new(tenantId, subject, [], Kind: kind)
    };

    private static UserTask CreateTask(DateTimeOffset createdAt, ParticipantReference actor) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        TenantId = actor.TenantId,
        WorkflowDefinitionId = "definition-1",
        WorkflowInstanceId = "instance-1",
        ActivityInstanceId = "activity-1",
        BookmarkId = Guid.NewGuid().ToString("N"),
        MaterializationKey = Guid.NewGuid().ToString("N"),
        Title = "Approve request",
        Summary = "Review the request",
        Tags = ["finance"],
        CandidateUsers = [actor],
        InvitationDefinitions = [new UserTaskInvitationDefinition("email-code", ["Complete"])],
        CreatedAt = createdAt,
        UpdatedAt = createdAt
    };
}
