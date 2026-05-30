using System.Collections.Concurrent;
using Elsa.Mediator.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Options;
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

    private sealed class RecordingNotificationSender(WorkerScopeMarker scopeMarker, NotificationSenderRecorder recorder) : INotificationSender
    {
        public Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record(scopeMarker.Id);
            return Task.CompletedTask;
        }

        public Task SendAsync(INotification notification, IEventPublishingStrategy? strategy, CancellationToken cancellationToken = default)
        {
            recorder.Record(scopeMarker.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class NotificationSenderRecorder
    {
        private int _callCount;
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentBag<Guid> ScopeIds { get; } = [];

        public void Record(Guid scopeId)
        {
            ScopeIds.Add(scopeId);

            if (Interlocked.Increment(ref _callCount) == 2)
                _completed.TrySetResult();
        }

        public async Task WaitForCallsAsync(int expectedCallCount, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _callCount) >= expectedCallCount)
                return;

            await _completed.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class WorkerScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed record TestNotification : INotification;
}
