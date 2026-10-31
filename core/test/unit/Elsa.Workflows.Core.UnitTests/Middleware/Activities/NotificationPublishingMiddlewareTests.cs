using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Middleware.Activities;

public class NotificationPublishingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PublishesExecutingAndExecuted_WhenNextCompletes()
    {
        var handler = new RecordingHandler();

        await InvokeAsync(_ => ValueTask.CompletedTask, handler);

        Assert.Equal([typeof(ActivityExecuting), typeof(ActivityExecuted)], handler.NotificationTypes);
    }

    [Fact]
    public async Task InvokeAsync_PublishesFaultedWithTheThrownException_WhenNextThrows()
    {
        var thrown = new InvalidOperationException("thrown fault");
        var handler = new RecordingHandler();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(context =>
        {
            // The invoker transitions to Running before ExecuteAsync; this middleware sits above it.
            context.TransitionTo(ActivityStatus.Running);
            throw thrown;
        }, handler));

        Assert.Same(thrown, actual);

        var faulted = Assert.Single(handler.Notifications.OfType<ActivityFaulted>());
        Assert.Same(thrown, faulted.Exception);
        Assert.Equal(ActivityStatus.Running, faulted.ActivityExecutionContext.Status);
        Assert.Contains(typeof(ActivityExecuted), handler.NotificationTypes);
    }

    [Fact]
    public async Task InvokeAsync_PublishesFaultedWithTheRecordedException_WhenNextFaults()
    {
        var fault = new InvalidOperationException("direct fault");
        var handler = new RecordingHandler();

        await InvokeAsync(context =>
        {
            context.Fault(fault);
            return ValueTask.CompletedTask;
        }, handler);

        var faulted = Assert.Single(handler.Notifications.OfType<ActivityFaulted>());
        Assert.Same(fault, faulted.Exception);
        Assert.Equal(ActivityStatus.Faulted, faulted.ActivityExecutionContext.Status);
        Assert.Contains(typeof(ActivityExecuted), handler.NotificationTypes);
    }

    [Fact]
    public async Task InvokeAsync_PublishesTheSameNotifications_WhenThrowingAndFaulting()
    {
        var thrownHandler = new RecordingHandler();
        var faultedHandler = new RecordingHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(_ => throw new InvalidOperationException("thrown fault"), thrownHandler));
        await InvokeAsync(context =>
        {
            context.Fault(new InvalidOperationException("direct fault"));
            return ValueTask.CompletedTask;
        }, faultedHandler);

        Assert.Equal(thrownHandler.NotificationTypes, faultedHandler.NotificationTypes);
        Assert.Equal([typeof(ActivityExecuting), typeof(ActivityExecuted), typeof(ActivityFaulted)], thrownHandler.NotificationTypes);
    }

    [Fact]
    public async Task InvokeAsync_PreservesOriginalException_WhenFaultHandlerThrows()
    {
        var thrown = new InvalidOperationException("thrown fault");
        var handlerFailure = new InvalidOperationException("handler failed");
        var handler = new RecordingHandler
        {
            ThrowOn = typeof(ActivityFaulted),
            ExceptionToThrow = handlerFailure
        };
        var logger = new RecordingLogger();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(_ => throw thrown, handler, logger));

        Assert.Same(thrown, actual);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Exception == handlerFailure);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotThrow_WhenFaultedHandlerThrowsAfterFault()
    {
        var fault = new InvalidOperationException("direct fault");
        var handlerFailure = new InvalidOperationException("handler failed");
        var handler = new RecordingHandler
        {
            ThrowOn = typeof(ActivityFaulted),
            ExceptionToThrow = handlerFailure
        };
        var logger = new RecordingLogger();

        await InvokeAsync(context =>
        {
            context.Fault(fault);
            return ValueTask.CompletedTask;
        }, handler, logger);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Exception == handlerFailure);
        Assert.Single(handler.Notifications.OfType<ActivityFaulted>());
    }

    [Fact]
    public async Task InvokeAsync_StillPublishesFaulted_WhenExecutedHandlerThrows()
    {
        var thrown = new InvalidOperationException("thrown fault");
        var handlerFailure = new InvalidOperationException("executed handler failed");
        var handler = new RecordingHandler
        {
            ThrowOn = typeof(ActivityExecuted),
            ExceptionToThrow = handlerFailure
        };
        var logger = new RecordingLogger();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(_ => throw thrown, handler, logger));

        Assert.Same(thrown, actual);
        var faulted = Assert.Single(handler.Notifications.OfType<ActivityFaulted>());
        Assert.Same(thrown, faulted.Exception);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Exception == handlerFailure);
    }

    [Fact]
    public async Task InvokeAsync_PublishesExecutedAndFaulted_WhenNextThrowsTaskCanceledException()
    {
        var thrown = new TaskCanceledException("http timeout");
        var handler = new RecordingHandler();

        var actual = await Assert.ThrowsAsync<TaskCanceledException>(() => InvokeAsync(_ => throw thrown, handler));

        Assert.Same(thrown, actual);
        Assert.False(actual.CancellationToken.IsCancellationRequested);

        var faulted = Assert.Single(handler.Notifications.OfType<ActivityFaulted>());
        Assert.Same(thrown, faulted.Exception);
        Assert.False(faulted.ActivityExecutionContext.CancellationToken.IsCancellationRequested);
        Assert.Equal([typeof(ActivityExecuting), typeof(ActivityExecuted), typeof(ActivityFaulted)], handler.NotificationTypes);
    }

    private static async Task InvokeAsync(ActivityMiddlewareDelegate next, RecordingHandler handler, ILogger<NotificationPublishingMiddleware>? logger = null)
    {
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(call => handler.HandleAsync(call.Arg<INotification>(), call.Arg<CancellationToken>()));

        var context = await new ActivityTestFixture(new NoOpActivity()).BuildAsync();
        var middleware = new NotificationPublishingMiddleware(next, notifications, logger);
        await middleware.InvokeAsync(context);
    }

    private sealed class RecordingHandler
    {
        public List<INotification> Notifications { get; } = [];
        public Type? ThrowOn { get; init; }
        public Exception? ExceptionToThrow { get; init; }
        public IReadOnlyList<Type> NotificationTypes => Notifications.Select(notification => notification.GetType()).ToList();

        public Task HandleAsync(INotification notification, CancellationToken cancellationToken)
        {
            Notifications.Add(notification);

            if (ThrowOn is not null && notification.GetType() == ThrowOn)
                throw ExceptionToThrow ?? new InvalidOperationException("handler failed");

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLogger : ILogger<NotificationPublishingMiddleware>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception));
    }

    private sealed class NoOpActivity : Activity;
}
