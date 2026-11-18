using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionDeletionTests
{
    [Fact]
    public void DeletingAnOwnedVersionDoesNotPromoteASharedVersionOfTheSameDefinition()
    {
        var owned = Definition();
        var shared = owned.ShallowClone();
        shared.Id = "shared-version";
        shared.TenantId = "*";
        shared.IsLatest = false;

        var plan = new WorkflowDefinitionDeletionPlan([owned], [owned, shared]);

        Assert.True(plan.Matches([owned, shared]));
        Assert.Equal(owned.Id, Assert.Single(plan.Deleted).Id);
        Assert.Empty(plan.Promoted);
    }

    [Fact]
    public async Task ChangedVersionSetStopsDeletionBeforeDependentCleanup()
    {
        var definition = Definition();
        var plan = new WorkflowDefinitionDeletionPlan([definition], [definition]);
        var current = definition.ShallowClone();
        current.IsReadonly = true;
        var store = Substitute.For<IWorkflowDefinitionStore>();
        store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<WorkflowDefinition>>([current]));
        var notifications = Substitute.For<INotificationSender>();
        var manager = new WorkflowDefinitionManager(store, notifications, Substitute.For<IWorkflowDefinitionPublisher>());

        await Assert.ThrowsAsync<WorkflowDefinitionEditConflictException>(() => manager.DeleteAuthorizedAsync(plan));

        await notifications.DidNotReceive().SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        await notifications.DidNotReceive().SendAsync(Arg.Any<INotification>(), Arg.Any<IEventPublishingStrategy>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().TryDeleteAsync(plan, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupFailureLeavesDefinitionsAvailableForAnOrdinaryRetry()
    {
        var definition = Definition();
        var plan = new WorkflowDefinitionDeletionPlan([definition], [definition]);
        var store = Substitute.For<IWorkflowDefinitionStore>();
        store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<WorkflowDefinition>>([definition]));
        store.SupportsDeletion(plan).Returns(true);
        store.TryDeleteAsync(plan, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<WorkflowDefinitionVersionsDeleting>(), NotificationStrategy.Sequential, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Cleanup failed.")), Task.CompletedTask);
        var manager = new WorkflowDefinitionManager(store, notifications, Substitute.For<IWorkflowDefinitionPublisher>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.DeleteAuthorizedAsync(plan));
        await store.DidNotReceive().TryDeleteAsync(plan, Arg.Any<CancellationToken>());

        Assert.Equal(1L, await manager.DeleteAuthorizedAsync(plan));
        await store.Received(1).TryDeleteAsync(plan, Arg.Any<CancellationToken>());
        await notifications.Received(1).SendAsync(Arg.Is<WorkflowDefinitionVersionsDeleted>(message => message.Ids.SequenceEqual(new[] { "version" })), Arg.Any<CancellationToken>());
    }

    private static WorkflowDefinition Definition() => new() { Id = "version", DefinitionId = "workflow", Version = 1, IsLatest = true };
}
