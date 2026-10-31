using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Activities.HostMethod;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class ActivityRegistryPopulatorTests
{
    [Fact]
    public async Task EnsureRegistryPopulatedAsync_InitializesAgnosticProvidersOnceAcrossScopedPopulators_AndRefreshesTenantSensitiveProvidersPerPass()
    {
        // Arrange
        var agnosticProviderCallCount = new CallCounter();
        var tenantSensitiveProviderCallCount = new CallCounter();
        var registry = CreateRegistry();
        IActivityRegistryPopulator firstScopePopulator = new ActivityRegistryPopulator(
            [new CountingTenantAgnosticProvider(agnosticProviderCallCount), new CountingTenantSensitiveProvider(tenantSensitiveProviderCallCount)], registry);
        IActivityRegistryPopulator secondScopePopulator = new ActivityRegistryPopulator(
            [new CountingTenantAgnosticProvider(agnosticProviderCallCount), new CountingTenantSensitiveProvider(tenantSensitiveProviderCallCount)], registry);

        // Act
        await firstScopePopulator.EnsureRegistryPopulatedAsync();
        await secondScopePopulator.EnsureRegistryPopulatedAsync();

        // Assert
        Assert.Equal(1, agnosticProviderCallCount.Value);
        Assert.Equal(2, tenantSensitiveProviderCallCount.Value);
    }

    [Fact]
    public async Task PopulateRegistryAsync_ForceRefreshesAllProvidersOnEveryPass()
    {
        // Arrange
        var agnosticProviderCallCount = new CallCounter();
        var tenantSensitiveProviderCallCount = new CallCounter();
        var agnosticProvider = new CountingTenantAgnosticProvider(agnosticProviderCallCount);
        var tenantSensitiveProvider = new CountingTenantSensitiveProvider(tenantSensitiveProviderCallCount);
        IActivityRegistryPopulator populator = new ActivityRegistryPopulator([agnosticProvider, tenantSensitiveProvider], CreateRegistry());

        // Act
        await populator.PopulateRegistryAsync();
        await populator.PopulateRegistryAsync();

        // Assert
        Assert.Equal(2, agnosticProviderCallCount.Value);
        Assert.Equal(2, tenantSensitiveProviderCallCount.Value);
    }

    [Fact]
    public void BuiltInTenantAgnosticProviders_OptIntoOncePerRegistryPopulation()
    {
        Assert.True(typeof(ITenantAgnosticActivityProvider).IsAssignableFrom(typeof(TypedActivityProvider)));
        Assert.True(typeof(ITenantAgnosticActivityProvider).IsAssignableFrom(typeof(HostMethodActivityProvider)));
        Assert.False(typeof(ITenantAgnosticActivityProvider).IsAssignableFrom(typeof(WorkflowDefinitionActivityProvider)));
    }

    private static ActivityRegistry CreateRegistry()
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.TenantId.Returns("tenant-1");
        return new ActivityRegistry(
            Substitute.For<IActivityDescriber>(),
            [],
            tenantAccessor,
            Substitute.For<ILogger<ActivityRegistry>>());
    }

    private sealed class CountingTenantAgnosticProvider(CallCounter callCounter) : ITenantAgnosticActivityProvider
    {
        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            callCounter.Increment();
            return new([new ActivityDescriptor
            {
                TypeName = nameof(CountingTenantAgnosticProvider),
                Version = 1,
                TenantId = Tenant.AgnosticTenantId,
                Kind = ActivityKind.Action
            }]);
        }
    }

    private sealed class CountingTenantSensitiveProvider(CallCounter callCounter) : IActivityProvider
    {
        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            callCounter.Increment();
            return new([new ActivityDescriptor
            {
                TypeName = nameof(CountingTenantSensitiveProvider),
                Version = 1,
                TenantId = "tenant-1",
                Kind = ActivityKind.Action
            }]);
        }
    }

    private sealed class CallCounter
    {
        private int _value;

        public int Value => Volatile.Read(ref _value);

        public void Increment() => Interlocked.Increment(ref _value);
    }
}
