using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowCommitNotificationBufferTests
{
    [Fact]
    public async Task SendAsync_WhenBuffering_DoesNotPublishUntilScopeIsFlushed()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = CreateBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var notification = new TestNotification();

        using var scope = buffer.Begin();
        await sender.SendAsync(notification);

        await mediator.DidNotReceive().SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());

        await scope.FlushAsync();

        await mediator.Received(1).SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_AfterScopeIsFlushed_PublishesImmediately()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = CreateBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var bufferedNotification = new TestNotification();
        var subsequentNotification = new TestNotification();
        var notificationPublishingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueNotificationPublishing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mediator
            .SendAsync(bufferedNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                notificationPublishingStarted.SetResult();
                await continueNotificationPublishing.Task;
            });

        using var scope = buffer.Begin();
        await sender.SendAsync(bufferedNotification);
        var flushTask = scope.FlushAsync();
        await notificationPublishingStarted.Task;
        continueNotificationPublishing.SetResult();
        await flushTask;
        await sender.SendAsync(subsequentNotification);

        await mediator.Received(1).SendAsync(bufferedNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
        await mediator.Received(1).SendAsync(subsequentNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenBufferingScopeIsDisposedWithoutFlush_DiscardsNotifications()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = CreateBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var notification = new TestNotification();

        using (buffer.Begin())
        {
            await sender.SendAsync(notification);
        }

        await mediator.DidNotReceive().SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_WhenNotificationFails_StillPublishesRemainingNotifications()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = CreateBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var failingNotification = new TestNotification();
        var succeedingNotification = new TestNotification();
        mediator
            .SendAsync(failingNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Handler failed"));

        using var scope = buffer.Begin();
        await sender.SendAsync(failingNotification);
        await sender.SendAsync(succeedingNotification);

        await Assert.ThrowsAsync<AggregateException>(() => scope.FlushAsync());

        await mediator.Received(1).SendAsync(failingNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
        await mediator.Received(1).SendAsync(succeedingNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_WhenNotificationIsCanceled_PropagatesCancellation()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = CreateBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var notification = new TestNotification();
        mediator
            .SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException());

        using var scope = buffer.Begin();
        await sender.SendAsync(notification);

        await Assert.ThrowsAsync<OperationCanceledException>(() => scope.FlushAsync());
    }

    private static WorkflowCommitNotificationBuffer CreateBuffer(IMediator mediator) => new(mediator, Substitute.For<ILogger<WorkflowCommitNotificationBuffer>>());

    private class TestNotification : INotification;
}
