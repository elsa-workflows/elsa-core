using System.Collections.Concurrent;
using Elsa.Mediator.Channels;
using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Options;
using Elsa.Mediator.PublishingStrategies;
using Elsa.Mediator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Mediator.UnitTests;

public class BackgroundNotificationProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesIndependentScopePerWorker()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationSenderRecorder>();
        services.AddScoped<WorkerScopeMarker>();
        services.AddScoped<INotificationSender, RecordingNotificationSender>();
        await using var serviceProvider = services.BuildServiceProvider();
        var channel = new NotificationsChannel();
        var processor = new BackgroundNotificationProcessor(
            Microsoft.Extensions.Options.Options.Create(new MediatorOptions { NotificationWorkerCount = 2 }),
            channel,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BackgroundNotificationProcessor>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var runTask = processor.ExecuteAsync(cancellationTokenSource.Token);
        var notificationStrategy = NotificationStrategy.Sequential;

        await channel.Writer.WriteAsync(new NotificationContext(new TestNotification(), notificationStrategy, serviceProvider), cancellationTokenSource.Token);
        await channel.Writer.WriteAsync(new NotificationContext(new TestNotification(), notificationStrategy, serviceProvider), cancellationTokenSource.Token);

        var recorder = serviceProvider.GetRequiredService<NotificationSenderRecorder>();
        await recorder.WaitForCallsAsync(2, cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();
        await runTask;

        Assert.Equal(2, recorder.ScopeIds.Distinct().Count());
    }

    [Fact]
    public async Task ExecuteAsync_UsesQueuedContextStrategy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NotificationSenderRecorder>();
        services.AddScoped<WorkerScopeMarker>();
        services.AddScoped<INotificationSender, RecordingNotificationSender>();
        await using var serviceProvider = services.BuildServiceProvider();
        var channel = new NotificationsChannel();
        var processor = new BackgroundNotificationProcessor(
            Microsoft.Extensions.Options.Options.Create(new MediatorOptions { NotificationWorkerCount = 1 }),
            channel,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BackgroundNotificationProcessor>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var runTask = processor.ExecuteAsync(cancellationTokenSource.Token);
        var strategy = new TestNotificationStrategy();

        await channel.Writer.WriteAsync(new NotificationContext(new TestNotification(), strategy, serviceProvider), cancellationTokenSource.Token);

        var recorder = serviceProvider.GetRequiredService<NotificationSenderRecorder>();
        var recordedCall = await recorder.WaitForCallAsync(cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();
        await runTask;

        Assert.Same(strategy, recordedCall.Strategy);
    }

    [Fact]
    public async Task BackgroundStrategy_QueuesExecutableContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INotificationsChannel, NotificationsChannel>();
        await using var serviceProvider = services.BuildServiceProvider();
        var channel = serviceProvider.GetRequiredService<INotificationsChannel>();
        var notificationContext = new NotificationContext(new TestNotification(), NotificationStrategy.Background, serviceProvider);
        var strategyContext = new NotificationStrategyContext(notificationContext, [], NullLogger.Instance, serviceProvider);

        await NotificationStrategy.Background.PublishAsync(strategyContext);

        var queuedContext = await channel.Reader.ReadAsync();

        Assert.Same(NotificationStrategy.Sequential, queuedContext.NotificationStrategy);
        Assert.Same(notificationContext.Notification, queuedContext.Notification);
    }

    private sealed class RecordingNotificationSender(WorkerScopeMarker scopeMarker, NotificationSenderRecorder recorder) : INotificationSender
    {
        public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record(scopeMarker.Id);
            return Task.CompletedTask;
        }

        public Task SendAsync(INotification notification, IEventPublishingStrategy? strategy, CancellationToken cancellationToken = default)
        {
            recorder.Record(scopeMarker.Id, strategy);
            return Task.CompletedTask;
        }
    }

    private sealed class NotificationSenderRecorder
    {
        private int _callCount;
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentBag<Guid> ScopeIds { get; } = [];
        private readonly TaskCompletionSource<RecordedNotificationCall> _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(Guid scopeId, IEventPublishingStrategy? strategy = null)
        {
            ScopeIds.Add(scopeId);
            _firstCall.TrySetResult(new(strategy));

            if (Interlocked.Increment(ref _callCount) == 2)
                _completed.TrySetResult();
        }

        public async Task WaitForCallsAsync(int expectedCallCount, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _callCount) >= expectedCallCount)
                return;

            await _completed.Task.WaitAsync(cancellationToken);
        }

        public Task<RecordedNotificationCall> WaitForCallAsync(CancellationToken cancellationToken) => _firstCall.Task.WaitAsync(cancellationToken);
    }

    private sealed class WorkerScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed record TestNotification : INotification;

    private sealed record RecordedNotificationCall(IEventPublishingStrategy? Strategy);

    private sealed class TestNotificationStrategy : SequentialProcessingStrategy
    {
    }
}
