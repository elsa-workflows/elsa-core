using Elsa.Common;
using Elsa.Common.DistributedHosting;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class ProcessWorkflowDispatchOutboxRecurringTaskTests
{
    [Fact]
    public async Task ExecuteAsync_DoesNothing_WhenTransactionalOutboxIsDisabled()
    {
        var processor = Substitute.For<IWorkflowDispatchOutboxProcessor>();
        var task = CreateTask(processor, useTransactionalOutbox: false);

        await task.ExecuteAsync();

        Assert.Empty(processor.ReceivedCalls());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsWithoutWaiting_WhenProcessorLockIsOwned()
    {
        var store = Substitute.For<IWorkflowDispatchOutboxStore>();
        var lockProvider = new OwnedLockProvider();
        var processor = new WorkflowDispatchOutboxProcessor(
            store,
            Substitute.For<IWorkflowInstanceStore>(),
            Substitute.For<IWorkflowDispatcher>(),
            lockProvider,
            Substitute.For<ISystemClock>(),
            Microsoft.Extensions.Options.Options.Create(new DistributedLockingOptions()),
            Microsoft.Extensions.Options.Options.Create(new WorkflowDispatcherOptions()),
            NullLogger<WorkflowDispatchOutboxProcessor>.Instance);
        var task = CreateTask(processor, useTransactionalOutbox: true);

        await task.ExecuteAsync();

        Assert.Equal(TimeSpan.Zero, Assert.Single(lockProvider.RequestedTimeouts));
        await store.DidNotReceiveWithAnyArgs().FindManyAsync(default, default);
    }

    private static ProcessWorkflowDispatchOutboxRecurringTask CreateTask(IWorkflowDispatchOutboxProcessor processor, bool useTransactionalOutbox)
    {
        var services = new ServiceCollection().AddSingleton(processor).BuildServiceProvider();
        var options = Microsoft.Extensions.Options.Options.Create(new WorkflowDispatcherOptions { UseTransactionalOutbox = useTransactionalOutbox });
        return new(services, options);
    }

    private sealed class OwnedLockProvider : IDistributedLockProvider
    {
        public List<TimeSpan> RequestedTimeouts { get; } = [];

        public IDistributedLock CreateLock(string name) => new OwnedLock(this, name);

        private sealed class OwnedLock(OwnedLockProvider provider, string name) : IDistributedLock
        {
            public string Name => name;

            public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
            {
                provider.RequestedTimeouts.Add(timeout);
                return new ValueTask<IDistributedSynchronizationHandle?>((IDistributedSynchronizationHandle?)null);
            }

            public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
