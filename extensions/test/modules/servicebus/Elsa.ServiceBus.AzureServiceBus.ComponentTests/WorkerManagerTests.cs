using Azure.Messaging.ServiceBus;
using Elsa.ServiceBus.AzureServiceBus.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.ServiceBus.AzureServiceBus.ComponentTests;

public class WorkerManagerTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DisposalWaitsForAnInProgressStartAndClosesItsWorker()
    {
        var start = new AsyncGate();
        var processor = new ControlledProcessor { OnStart = start.WaitAsync };
        using var services = CreateServices(_ => processor, out _);
        var manager = new WorkerManager(services);
        var starting = manager.StartWorkerAsync("a", null);
        await start.Entered.Task.WaitAsync(Watchdog);

        // Invoke directly: the old implementation finishes its empty snapshot here.
        var disposing = manager.DisposeAsync().AsTask();
        var wasPending = !disposing.IsCompleted;
        start.Release.SetResult();
        await Task.WhenAll(starting, disposing).WaitAsync(Watchdog);

        Assert.Equal(1, processor.CloseCalls);
        Assert.True(wasPending);
    }

    [Fact]
    public async Task StartsAreRejectedWhileClosingAndAfterDisposalIncludingDuplicates()
    {
        var close = new AsyncGate();
        var processor = new ControlledProcessor { OnClose = close.WaitAsync };
        using var services = CreateServices(_ => processor, out var client);
        var manager = new WorkerManager(services);
        await manager.StartWorkerAsync("a", null);
        var disposing = manager.DisposeAsync().AsTask();
        await close.Entered.Task.WaitAsync(Watchdog);
        try
        {
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.StartWorkerAsync("a", null).WaitAsync(Watchdog));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.StartWorkerAsync("b", null).WaitAsync(Watchdog));
        }
        finally
        {
            close.Release.SetResult();
            await disposing.WaitAsync(Watchdog);
        }

        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.StartWorkerAsync("a", null));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.StartWorkerAsync("b", null));
        client.DidNotReceive().CreateProcessor("b", Arg.Any<ServiceBusProcessorOptions>());
        Assert.Equal(1, processor.CloseCalls);
    }

    [Fact]
    public async Task OverlappingAndRepeatedDisposalShareOneClose()
    {
        var close = new AsyncGate();
        var processor = new ControlledProcessor { OnClose = close.WaitAsync };
        using var services = CreateServices(_ => processor, out _);
        var manager = new WorkerManager(services);
        await manager.StartWorkerAsync("a", null);
        var first = manager.DisposeAsync().AsTask();
        await close.Entered.Task.WaitAsync(Watchdog);
        var second = manager.DisposeAsync().AsTask();
        var wasPending = !second.IsCompleted;
        close.Release.SetResult();
        await Task.WhenAll(first, second).WaitAsync(Watchdog);
        await manager.DisposeAsync();

        Assert.True(wasPending);
        Assert.Equal(1, processor.CloseCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloseFailuresAreSharedWithoutSkippingOrRetryingWorkers(bool failEveryClose)
    {
        var close = new AsyncGate();
        var failure = new InvalidOperationException("Close failed");
        var attempts = 0;
        async Task CloseAsync()
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                await close.WaitAsync();
                throw failure;
            }
            if (failEveryClose)
            {
                throw failure;
            }
        }

        var processors = new[]
        {
            new ControlledProcessor { OnClose = CloseAsync },
            new ControlledProcessor { OnClose = CloseAsync }
        };
        using var services = CreateServices(queue => processors[queue == "a" ? 0 : 1], out _);
        var manager = new WorkerManager(services);
        await manager.StartWorkerAsync("a", null);
        await manager.StartWorkerAsync("b", null);
        var first = manager.DisposeAsync().AsTask();
        await close.Entered.Task.WaitAsync(Watchdog);
        var second = manager.DisposeAsync().AsTask();
        var wasPending = !second.IsCompleted;
        close.Release.SetResult();

        var firstFailure = await Assert.ThrowsAnyAsync<Exception>(() => first.WaitAsync(Watchdog));
        var secondFailure = await Assert.ThrowsAnyAsync<Exception>(() => second.WaitAsync(Watchdog));
        var repeatedFailure = await Assert.ThrowsAnyAsync<Exception>(() => manager.DisposeAsync().AsTask().WaitAsync(Watchdog));
        Assert.True(wasPending);
        Assert.Same(firstFailure, secondFailure);
        Assert.Same(firstFailure, repeatedFailure);
        if (failEveryClose)
        {
            var aggregate = Assert.IsType<AggregateException>(firstFailure);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
            Assert.All(aggregate.InnerExceptions, exception => Assert.Same(failure, exception));
        }
        else
        {
            Assert.Same(failure, firstFailure);
        }
        Assert.All(processors, processor => Assert.Equal(1, processor.CloseCalls));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.StartWorkerAsync("c", null));
    }

    [Fact]
    public async Task FailedStartupDuringShutdownClosesOnlyThroughStartupCleanup()
    {
        var start = new AsyncGate();
        var failure = new InvalidOperationException("Start failed");
        var processor = new ControlledProcessor
        {
            OnStart = async () =>
            {
                await start.WaitAsync();
                throw failure;
            }
        };
        using var services = CreateServices(_ => processor, out _);
        var manager = new WorkerManager(services);
        var starting = manager.StartWorkerAsync("a", null);
        await start.Entered.Task.WaitAsync(Watchdog);
        var disposing = manager.DisposeAsync().AsTask();
        start.Release.SetResult();

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => starting.WaitAsync(Watchdog)));
        await disposing.WaitAsync(Watchdog);
        Assert.Empty(manager.Workers);
        Assert.Equal(1, processor.CloseCalls);
    }

    private static ServiceProvider CreateServices(Func<string, ControlledProcessor> createProcessor, out ServiceBusClient client)
    {
        client = Substitute.For<ServiceBusClient>();
        client.CreateProcessor(Arg.Any<string>(), Arg.Any<ServiceBusProcessorOptions>())
            .Returns(call => createProcessor(call.Arg<string>()));
        return new ServiceCollection().AddLogging().AddSingleton(client).BuildServiceProvider();
    }

    private sealed class AsyncGate
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task WaitAsync()
        {
            Entered.SetResult();
            await Release.Task;
        }
    }

    // Like MockServiceBusProcessor, this controls lifecycle calls without starting
    // a real Azure receiver; it tests manager ownership, not SDK running state.
    private sealed class ControlledProcessor : ServiceBusProcessor
    {
        public Func<Task> OnStart { get; init; } = () => Task.CompletedTask;
        public Func<Task> OnClose { get; init; } = () => Task.CompletedTask;
        public int CloseCalls { get; private set; }

        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => OnStart();

        public override async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            CloseCalls++;
            await OnClose();
        }
    }
}
