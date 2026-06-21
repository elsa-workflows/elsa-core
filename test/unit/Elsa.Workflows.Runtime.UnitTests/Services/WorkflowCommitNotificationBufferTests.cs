using Elsa.Mediator.Contracts;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class WorkflowCommitNotificationBufferTests
{
    [Fact]
    public async Task SendAsync_WhenBuffering_DoesNotPublishUntilScopeIsFlushed()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = new WorkflowCommitNotificationBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var notification = new TestNotification();

        using var scope = buffer.Begin();
        await sender.SendAsync(notification);

        await mediator.DidNotReceive().SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());

        await scope.FlushAsync();

        await mediator.Received(1).SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenBufferingScopeIsDisposedWithoutFlush_DiscardsNotifications()
    {
        var mediator = Substitute.For<IMediator>();
        var buffer = new WorkflowCommitNotificationBuffer(mediator);
        var sender = new WorkflowCommitNotificationSender(mediator, buffer);
        var notification = new TestNotification();

        using (buffer.Begin())
        {
            await sender.SendAsync(notification);
        }

        await mediator.DidNotReceive().SendAsync(notification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>());
    }

    private class TestNotification : INotification;
}
