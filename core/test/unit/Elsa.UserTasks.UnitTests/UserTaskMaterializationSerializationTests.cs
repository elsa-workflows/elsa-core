using Elsa.Extensions;
using Elsa.UserTasks.Handlers;
using Elsa.UserTasks.Models;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// Bookmark storage uses <see cref="IPayloadSerializer"/>. Without a registered alias the payload
/// comes back as an untyped bag and <c>TaskId</c> is lost, so the finalize handler skips the task.
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
    public async Task HandleAsync_FinalizesCompletingTaskAfterPayloadSerializerRoundTrip()
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
        var serializer = serviceProvider.GetRequiredService<IPayloadSerializer>();
        var payload = serializer.Deserialize(serializer.Serialize(fixture.Materialization(actor.Subject)));
        var bookmark = new Bookmark(task.BookmarkId, "UserTask", "hash", payload, "a1", "n1", null, DateTimeOffset.UtcNow);
        var handler = new UserTaskBookmarkPersistedHandler(fixture.Projection, fixture.Clock);

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
