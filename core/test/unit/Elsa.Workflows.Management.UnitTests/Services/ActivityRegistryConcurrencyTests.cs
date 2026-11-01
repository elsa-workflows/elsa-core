using Elsa.Common.Multitenancy;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class ActivityRegistryConcurrencyTests
{
    private const int DescriptorCount = 50;

    [Fact(DisplayName = "Parallel refreshes of one tenant-agnostic group replace it cleanly instead of interleaving")]
    public async Task ParallelRefreshesOfAgnosticGroupDoNotInterleave()
    {
        var logger = new WarningCountingLogger();
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns("tenant-1");
        var registry = new ActivityRegistry(Substitute.For<IActivityDescriber>(), [], tenantAccessor, logger);
        var provider = new AgnosticDescriptorsProvider();

        for (var round = 0; round < 20; round++)
            await Parallel.ForEachAsync(Enumerable.Range(0, 16), async (_, token) => await registry.RefreshDescriptorsAsync(provider, token));

        Assert.Equal(0, logger.WarningCount);
        Assert.Equal(DescriptorCount, registry.ListByProvider(typeof(AgnosticDescriptorsProvider)).Count());
    }

    [Fact(DisplayName = "Concurrent refreshes do not serialize the provider call")]
    public async Task ConcurrentRefreshesRunProviderCallsInParallel()
    {
        var registry = CreateRegistry();
        var provider = new RendezvousProvider(expectedCallers: 2);

        var first = registry.RefreshDescriptorsAsync(provider);
        var second = registry.RefreshDescriptorsAsync(provider);

        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(DescriptorCount, registry.ListByProvider(typeof(RendezvousProvider)).Count());
    }

    [Fact(DisplayName = "Readers never miss a descriptor that is present before and after a refresh")]
    public async Task ReadersNeverSeeGapDuringRefresh()
    {
        var registry = CreateRegistry();
        var provider = new AgnosticDescriptorsProvider();
        await registry.RefreshDescriptorsAsync(provider);

        using var cts = new CancellationTokenSource();
        var misses = 0;

        var reader = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                if (registry.Find("Agnostic0") is null || registry.Find("Agnostic0", 1) is null)
                    Interlocked.Increment(ref misses);
            }
        });

        for (var i = 0; i < 200; i++)
            await registry.RefreshDescriptorsAsync(provider);

        await cts.CancelAsync();
        await reader;

        Assert.Equal(0, misses);
    }

    [Fact(DisplayName = "Provider mutations and refreshes writing to the same registry stay consistent")]
    public async Task ProviderMutationsAndRefreshesStayConsistent()
    {
        var registry = CreateRegistry();
        var refreshProvider = new AgnosticDescriptorsProvider();
        var manualProviderType = typeof(ManualProvider);

        var refreshes = Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
                await registry.RefreshDescriptorsAsync(refreshProvider);
        });

        var mutations = Parallel.ForEachAsync(Enumerable.Range(0, 100), (i, _) =>
        {
            var descriptor = CreateDescriptor($"Manual{i}");
            registry.Add(manualProviderType, descriptor);

            if (i % 2 == 0)
                registry.Remove(manualProviderType, descriptor);

            return ValueTask.CompletedTask;
        });

        await Task.WhenAll(refreshes, mutations).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(DescriptorCount, registry.ListByProvider(typeof(AgnosticDescriptorsProvider)).Count());
        Assert.Equal(50, registry.ListByProvider(manualProviderType).Count());
        Assert.Equal(DescriptorCount + 50, registry.ListAll().Count());

        var clears = Task.Run(() => registry.ClearProvider(manualProviderType));
        var moreRefreshes = Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
                await registry.RefreshDescriptorsAsync(refreshProvider);
        });

        await Task.WhenAll(clears, moreRefreshes).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Empty(registry.ListByProvider(manualProviderType));
        Assert.Equal(DescriptorCount, registry.ListAll().Count());
    }

    private static ActivityRegistry CreateRegistry()
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns("tenant-1");
        return new(Substitute.For<IActivityDescriber>(), [], tenantAccessor, new WarningCountingLogger());
    }

    private static ActivityDescriptor CreateDescriptor(string typeName) => new()
    {
        TypeName = typeName,
        Version = 1,
        TenantId = Tenant.AgnosticTenantId,
        Kind = ActivityKind.Action
    };

    private sealed class ManualProvider;

    /// <summary>
    /// Completes only once all expected callers are inside <see cref="GetDescriptorsAsync"/> at the same time.
    /// </summary>
    private sealed class RendezvousProvider(int expectedCallers) : IActivityProvider
    {
        private readonly TaskCompletionSource _allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callers;

        public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _callers) == expectedCallers)
                _allArrived.SetResult();

            await _allArrived.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            return Enumerable.Range(0, DescriptorCount).Select(i => CreateDescriptor($"Agnostic{i}")).ToList();
        }
    }

    /// <summary>
    /// A tenant-sensitive provider, like the workflow-definition provider, whose result includes tenant-agnostic
    /// descriptors that every tenant's refresh writes into the shared agnostic registry.
    /// </summary>
    private sealed class AgnosticDescriptorsProvider : IActivityProvider
    {
        public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            return Enumerable.Range(0, DescriptorCount).Select(i => new ActivityDescriptor
            {
                TypeName = $"Agnostic{i}",
                Version = 1,
                TenantId = Tenant.AgnosticTenantId,
                Kind = ActivityKind.Action
            }).ToList();
        }
    }

    private sealed class WarningCountingLogger : ILogger<ActivityRegistry>
    {
        private int _warningCount;

        public int WarningCount => Volatile.Read(ref _warningCount);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Interlocked.Increment(ref _warningCount);
        }
    }
}
