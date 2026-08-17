using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.UserTasks.Contracts;
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

        var query = await repository.QueryAsync(new UserTaskQuery
        {
            TenantId = task.TenantId,
            Limit = 1,
            IncludeTotalCount = true,
            Scope = new UserTaskQueryScope(task.TenantId, actor, [])
        });
        Assert.Single(query.Items);
        Assert.Single(query.Items.Single().CandidateUsers);
        Assert.Equal(2, query.TotalCount);
        Assert.NotNull(query.NextCursor);

        var finalPage = await repository.QueryAsync(new UserTaskQuery
        {
            TenantId = task.TenantId,
            Limit = 1,
            Cursor = query.NextCursor,
            Scope = new UserTaskQueryScope(task.TenantId, actor, [])
        });
        Assert.Single(finalPage.Items);
        Assert.Null(finalPage.NextCursor);

        var crossTenantScope = await repository.QueryAsync(new UserTaskQuery
        {
            TenantId = task.TenantId,
            IncludeTotalCount = true,
            Scope = new UserTaskQueryScope("other-tenant", actor with { TenantId = "other-tenant" }, [])
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
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repository.SaveAsync(secondTask, 1));

        var excludedTask = CreateTask(DateTimeOffset.UtcNow.AddSeconds(2), actor);
        excludedTask.Assignee = actor;
        excludedTask.Status = UserTaskStatus.Assigned;
        excludedTask.ExcludedUsers = [actor];
        await repository.AddProjectionAsync(excludedTask);
        var excludedQuery = await repository.QueryAsync(new UserTaskQuery
        {
            TenantId = task.TenantId,
            IncludeTotalCount = true,
            Scope = new UserTaskQueryScope(task.TenantId, actor, [])
        });
        Assert.DoesNotContain(excludedQuery.Items, x => x.Id == excludedTask.Id);
        Assert.Equal(2, excludedQuery.TotalCount);
    }

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
