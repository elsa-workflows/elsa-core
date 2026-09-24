using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.Tasks;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Tasks;

public class RefreshWorkflowDefinitionActivityRegistryTaskTests
{
    [Fact]
    public async Task ExecuteAsync_ReconcilesOncePerGenerationAndCatchesUpAfterOfflineChanges()
    {
        var generations = new SharedGenerationStore();
        var updater = Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>();
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var task = new RefreshWorkflowDefinitionActivityRegistryTask(generations, updater, tenantAccessor, TimeProvider.System);

        await task.ExecuteAsync();
        await task.ExecuteAsync();
        await generations.IncrementAsync("tenant-a");
        await generations.IncrementAsync("tenant-a");
        await task.ExecuteAsync();
        await task.ExecuteAsync();

        await updater.Received(2).ReconcileRegistryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReconcilesTenantWhenAgnosticGenerationChanges()
    {
        var generations = new SharedGenerationStore();
        var updater = Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>();
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var task = new RefreshWorkflowDefinitionActivityRegistryTask(generations, updater, tenantAccessor, TimeProvider.System);

        await task.ExecuteAsync();
        await generations.IncrementAsync(Tenant.AgnosticTenantId);
        await task.ExecuteAsync();

        await updater.Received(2).ReconcileRegistryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotPollNodeLocalGenerationStore()
    {
        var generations = new MemoryWorkflowDefinitionRegistryGenerationStore();
        var updater = Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>();
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var task = new RefreshWorkflowDefinitionActivityRegistryTask(generations, updater, tenantAccessor, TimeProvider.System);

        await task.ExecuteAsync();

        await updater.DidNotReceive().ReconcileRegistryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FreshNodeReconcilesAfterOfflineChanges()
    {
        var generations = new SharedGenerationStore();
        await generations.IncrementAsync("tenant-a");
        await generations.IncrementAsync(Tenant.AgnosticTenantId);
        var updater = Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>();
        var task = new RefreshWorkflowDefinitionActivityRegistryTask(
            generations,
            updater,
            new TestTenantAccessor("tenant-a"),
            TimeProvider.System);

        await task.ExecuteAsync();

        await updater.Received(1).ReconcileRegistryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PerformsFullReconciliationAfterOneMinuteWithoutGenerationChange()
    {
        var generations = new SharedGenerationStore();
        var updater = Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>();
        var timeProvider = new ManualTimeProvider();
        var task = new RefreshWorkflowDefinitionActivityRegistryTask(
            generations,
            updater,
            new TestTenantAccessor("tenant-a"),
            timeProvider);

        await task.ExecuteAsync();
        await task.ExecuteAsync();
        await updater.Received(1).ReconcileRegistryAsync(Arg.Any<CancellationToken>());

        timeProvider.Advance(TimeSpan.FromMinutes(1));
        await task.ExecuteAsync();

        await updater.Received(2).ReconcileRegistryAsync(Arg.Any<CancellationToken>());
    }

    private sealed class SharedGenerationStore : IWorkflowDefinitionRegistryGenerationStore
    {
        private readonly MemoryWorkflowDefinitionRegistryGenerationStore _store = new();

        public bool IsShared => true;

        public Task<long> IncrementAsync(string? tenantId, CancellationToken cancellationToken = default) => _store.IncrementAsync(tenantId, cancellationToken);

        public Task<long> GetGenerationAsync(string? tenantId, CancellationToken cancellationToken = default) => _store.GetGenerationAsync(tenantId, cancellationToken);

    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

        public void Advance(TimeSpan duration) => Interlocked.Add(ref _timestamp, duration.Ticks);
    }
}
