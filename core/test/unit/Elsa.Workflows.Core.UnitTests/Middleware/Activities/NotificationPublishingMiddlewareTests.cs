using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.ActivityExecution;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Middleware.Activities;

public class NotificationPublishingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PublishesExecutingAndExecuted_WhenNextCompletes()
    {
        var sent = await PublishAsync(_ => ValueTask.CompletedTask);

        Assert.Equal([typeof(ActivityExecuting), typeof(ActivityExecuted)], sent);
    }

    [Fact]
    public async Task InvokeAsync_PublishesExecuted_WhenNextThrows()
    {
        var sent = await PublishAsync(_ => throw new InvalidOperationException("thrown fault"));

        Assert.Contains(typeof(ActivityExecuting), sent);
        Assert.Contains(typeof(ActivityExecuted), sent);
    }

    [Fact]
    public async Task InvokeAsync_PublishesExecuted_WhenNextFaults()
    {
        var sent = await PublishAsync(context =>
        {
            context.Fault(new InvalidOperationException("direct fault"));
            return ValueTask.CompletedTask;
        });

        Assert.Contains(typeof(ActivityExecuting), sent);
        Assert.Contains(typeof(ActivityExecuted), sent);
    }

    [Fact]
    public async Task InvokeAsync_PublishesTheSameNotifications_WhenThrowingAndFaulting()
    {
        var thrown = await PublishAsync(_ => throw new InvalidOperationException("thrown fault"));
        var faulted = await PublishAsync(context =>
        {
            context.Fault(new InvalidOperationException("direct fault"));
            return ValueTask.CompletedTask;
        });

        Assert.Equal(thrown, faulted);
        Assert.Equal([typeof(ActivityExecuting), typeof(ActivityExecuted)], thrown);
    }

    private static async Task<IReadOnlyList<Type>> PublishAsync(ActivityMiddlewareDelegate next)
    {
        var sent = new List<Type>();
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent.Add(call.Arg<INotification>().GetType());
                return Task.CompletedTask;
            });

        var context = await new ActivityTestFixture(new NoOpActivity()).BuildAsync();
        var middleware = new NotificationPublishingMiddleware(next, notifications);

        try
        {
            await middleware.InvokeAsync(context);
        }
        catch (InvalidOperationException)
        {
            // The throw path must still publish ActivityExecuted before the exception escapes.
        }

        return sent;
    }

    private sealed class NoOpActivity : Activity;
}
