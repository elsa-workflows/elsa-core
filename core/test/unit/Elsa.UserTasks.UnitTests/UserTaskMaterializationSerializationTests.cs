using Elsa.Extensions;
using Elsa.UserTasks.Handlers;
using Elsa.UserTasks.Models;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// The finalize handler reads <c>Diff.Removed</c> from workflow state. On EF Core hosts that state
/// round-trips through <see cref="IWorkflowStateSerializer"/>, which writes <c>$id</c>/<c>$ref</c>.
/// A positional record cannot deserialize that shape; the parameterless constructor plus the
/// bookmark-id fallback are what let a completing task reach <see cref="UserTaskStatus.Completed"/>.
/// </summary>
public class UserTaskMaterializationSerializationTests
{
    [Fact]
    public void PayloadSerializer_RoundTripsUserTaskMaterializationTaskId()
    {
        using var serviceProvider = BuildUserTasksProvider();
        var serializer = serviceProvider.GetRequiredService<IPayloadSerializer>();
        var materialization = CreateMaterialization("task-keep");

        var payload = serializer.Deserialize(serializer.Serialize(materialization));

        var readBack = Assert.IsType<UserTaskMaterialization>(payload);
        Assert.Equal("task-keep", readBack.TaskId);
        Assert.Equal(materialization.BookmarkId, readBack.BookmarkId);
        Assert.Equal(materialization.TenantId, readBack.TenantId);
    }

    [Fact]
    public async Task HandleAsync_FinalizesCompletingTaskAfterWorkflowStateSerializerRoundTrip()
    {
        var fixture = new UserTaskTestFixture();
        var actor = fixture.Actor("user-1");
        var task = await fixture.ProjectAsync(actor.Subject);
        var claimed = await fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(task.Revision, "claim-1"), actor);
        Assert.True(claimed.Accepted);
        var completing = await fixture.Manager.CompleteAsync(
            UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(completing.Accepted);
        Assert.Equal(UserTaskStatus.Completing, completing.Task.Status);

        using var serviceProvider = BuildUserTasksProvider();
        var serializer = serviceProvider.GetRequiredService<IWorkflowStateSerializer>();
        var bookmark = new Bookmark(task.BookmarkId, "UserTask", "hash", fixture.Materialization(actor.Subject), "a1", "n1", null, DateTimeOffset.UtcNow);
        var state = serializer.Deserialize(serializer.Serialize(new WorkflowState { Id = "i", Bookmarks = [bookmark] }));
        var payload = state.Bookmarks.Single().Payload;
        Assert.IsType<UserTaskMaterialization>(payload);
        Assert.Equal("task-1", ((UserTaskMaterialization)payload!).TaskId);

        var handler = new UserTaskBookmarkPersistedHandler(fixture.Projection, fixture.Repository, fixture.Clock);
        await handler.HandleAsync(new WorkflowBookmarksPersisted(Diff.From<Bookmark>([], [state.Bookmarks.Single()], [])), CancellationToken.None);

        var completed = await fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        Assert.Equal(UserTaskStatus.Completed, completed!.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task HandleAsync_FinalizesCompletingTaskWhenPayloadIsUnreadable()
    {
        var fixture = new UserTaskTestFixture();
        var actor = fixture.Actor("user-1");
        var task = await fixture.ProjectAsync(actor.Subject);
        var claimed = await fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(task.Revision, "claim-1"), actor);
        var completing = await fixture.Manager.CompleteAsync(
            UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.Equal(UserTaskStatus.Completing, completing.Task.Status);

        // 3.9 state-serializer bag: $values on collections, so ReadMaterialization fails and TaskId is lost.
        var payload = new Dictionary<string, object?>
        {
            ["tenantId"] = UserTaskTestFixture.TenantId,
            ["definition"] = new Dictionary<string, object?>
            {
                ["tags"] = new Dictionary<string, object?> { ["$values"] = new[] { "urgent" } }
            }
        };
        var bookmark = new Bookmark(task.BookmarkId, "UserTask", "hash", payload, "a1", "n1", null, DateTimeOffset.UtcNow);
        var handler = new UserTaskBookmarkPersistedHandler(fixture.Projection, fixture.Repository, fixture.Clock);

        await handler.HandleAsync(new WorkflowBookmarksPersisted(Diff.From<Bookmark>([], [bookmark], [])), CancellationToken.None);

        var completed = await fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        Assert.Equal(UserTaskStatus.Completed, completed!.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    private static ServiceProvider BuildUserTasksProvider()
    {
        var services = new ServiceCollection();
        services.AddElsa(elsa => elsa.UseUserTasks());
        return services.BuildServiceProvider();
    }

    private static UserTaskMaterialization CreateMaterialization(string taskId) =>
        new(
            "tenant",
            "definition",
            "instance",
            "activity",
            "bookmark",
            new UserTaskDefinitionSnapshot { Title = "Approve" },
            [],
            [],
            DateTimeOffset.UtcNow,
            taskId);
}
