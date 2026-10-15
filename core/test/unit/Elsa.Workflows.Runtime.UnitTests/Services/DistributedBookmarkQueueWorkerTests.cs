using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Distributed;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DistributedBookmarkQueueWorkerTests
{
    [Theory]
    [InlineData(null, nameof(DistributedBookmarkQueueWorker))]
    [InlineData("", nameof(DistributedBookmarkQueueWorker))]
    [InlineData("tenant-a", $"{nameof(DistributedBookmarkQueueWorker)}:tenant-a")]
    public void GetLockName_KeepsLegacyNameForDefaultTenant(string? tenantId, string expected)
    {
        Assert.Equal(expected, DistributedBookmarkQueueWorker.GetLockName(tenantId));
    }

    [Fact]
    public async Task ProcessAsync_LockNameIncludesCapturedTenantId()
    {
        var accessor = new DefaultTenantAccessor();
        var signaler = new BookmarkQueueSignaler(accessor);
        var lockProvider = new RecordingLockProvider { Succeeds = true };
        var tenant = new Tenant { Id = "tenant-a", Name = "A" };
        var processor = new CountingProcessor();
        var worker = CreateWorker(signaler, accessor, tenant, lockProvider, processor);

        try
        {
            using (accessor.PushContext(tenant))
                worker.Start();

            using (accessor.PushContext(tenant))
                await signaler.TriggerAsync();

            await WaitUntilAsync(() => processor.Calls == 1);

            Assert.Equal([$"{nameof(DistributedBookmarkQueueWorker)}:tenant-a"], lockProvider.Names);
        }
        finally
        {
            worker.Stop();
        }
    }

    [Fact]
    public async Task ProcessAsync_DefaultTenant_UsesLegacyLockName()
    {
        var accessor = new DefaultTenantAccessor();
        var signaler = new BookmarkQueueSignaler(accessor);
        var lockProvider = new RecordingLockProvider { Succeeds = true };
        var processor = new CountingProcessor();
        var worker = CreateWorker(signaler, accessor, Elsa.Common.Multitenancy.Tenant.Default, lockProvider, processor);

        try
        {
            using (accessor.PushContext(Elsa.Common.Multitenancy.Tenant.Default))
                worker.Start();

            await signaler.TriggerAsync();
            await WaitUntilAsync(() => processor.Calls == 1);

            Assert.Equal([nameof(DistributedBookmarkQueueWorker)], lockProvider.Names);
        }
        finally
        {
            worker.Stop();
        }
    }

    [Fact]
    public async Task LockRetry_SignalsCapturedTenant_NotAnotherTenant()
    {
        var accessor = new DefaultTenantAccessor();
        var signaler = new BookmarkQueueSignaler(accessor);
        var tenantA = new Tenant { Id = "tenant-a", Name = "A" };
        var tenantB = new Tenant { Id = "tenant-b", Name = "B" };
        var lockProviderA = new RecordingLockProvider { Succeeds = false };
        var processorA = new CountingProcessor();
        var processorB = new CountingProcessor();
        var workerA = CreateWorker(signaler, accessor, tenantA, lockProviderA, processorA);
        var workerB = CreateWorker(signaler, accessor, tenantB, new RecordingLockProvider { Succeeds = true }, processorB);

        try
        {
            using (accessor.PushContext(tenantA))
                workerA.Start();
            using (accessor.PushContext(tenantB))
                workerB.Start();

            using (accessor.PushContext(tenantA))
                await signaler.TriggerAsync();

            await WaitUntilAsync(() => lockProviderA.Names.Count >= 2);

            Assert.Equal(0, processorB.Calls);
            Assert.All(lockProviderA.Names, name => Assert.Equal($"{nameof(DistributedBookmarkQueueWorker)}:tenant-a", name));
        }
        finally
        {
            workerA.Stop();
            workerB.Stop();
        }
    }

    private static FastRetryDistributedBookmarkQueueWorker CreateWorker(
        BookmarkQueueSignaler signaler,
        DefaultTenantAccessor accessor,
        Tenant tenant,
        RecordingLockProvider lockProvider,
        IBookmarkQueueProcessor processor)
    {
        var services = new ServiceCollection()
            .AddSingleton(processor)
            .BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        using (accessor.PushContext(tenant))
        {
            return new FastRetryDistributedBookmarkQueueWorker(
                lockProvider,
                signaler,
                scopeFactory,
                NullLogger<DistributedBookmarkQueueWorker>.Instance,
                new DefaultTenantScopeFactory(accessor, scopeFactory),
                accessor);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Condition was not met in time.");
            await Task.Delay(10);
        }
    }

    private sealed class FastRetryDistributedBookmarkQueueWorker(
        IDistributedLockProvider distributedLockProvider,
        IBookmarkQueueSignaler signaler,
        IServiceScopeFactory scopeFactory,
        Microsoft.Extensions.Logging.ILogger<DistributedBookmarkQueueWorker> logger,
        ITenantScopeFactory? tenantScopeFactory,
        ITenantAccessor? tenantAccessor)
        : DistributedBookmarkQueueWorker(distributedLockProvider, signaler, scopeFactory, logger, tenantScopeFactory, tenantAccessor, TimeSpan.Zero)
    {
        protected override TimeSpan LockRetryDelay => TimeSpan.FromMilliseconds(20);
    }

    private sealed class CountingProcessor : IBookmarkQueueProcessor
    {
        public int Calls { get; private set; }

        public Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLockProvider : IDistributedLockProvider
    {
        public List<string> Names { get; } = [];
        public bool Succeeds { get; set; }

        public IDistributedLock CreateLock(string name)
        {
            Names.Add(name);
            return new StubDistributedLock(Succeeds);
        }
    }

    private sealed class StubDistributedLock(bool succeeds) : IDistributedLock
    {
        public string Name => "stub";

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            new StubHandle();

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IDistributedSynchronizationHandle>(new StubHandle());

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            succeeds ? new StubHandle() : null;

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(succeeds ? (IDistributedSynchronizationHandle?)new StubHandle() : null);
    }

    private sealed class StubHandle : IDistributedSynchronizationHandle
    {
        public CancellationToken HandleLostToken => CancellationToken.None;
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
