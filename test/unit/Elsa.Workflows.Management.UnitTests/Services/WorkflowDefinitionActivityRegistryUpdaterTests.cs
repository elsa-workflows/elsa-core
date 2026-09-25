using Elsa.Caching;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionActivityRegistryUpdaterTests
{
    [Fact]
    public async Task ReconcileRegistryAsync_ClearsEmptyCurrentTenantAndAgnosticEntries_AndPreservesOtherTenants()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var store = new MemoryStore<WorkflowDefinition>();
        var workflowDefinitionStore = new MemoryWorkflowDefinitionStore(store, tenantAccessor);
        var provider = new WorkflowDefinitionActivityProvider(workflowDefinitionStore, new WorkflowDefinitionActivityDescriptorFactory(), tenantAccessor);
        var registry = new ActivityRegistry(Substitute.For<IActivityDescriber>(), [], tenantAccessor, NullLogger<ActivityRegistry>.Instance);
        var providerType = typeof(WorkflowDefinitionActivityProvider);
        registry.Add(providerType, CreateDescriptor("Current", "tenant-a"));
        registry.Add(providerType, CreateDescriptor("Other", "tenant-b"));
        registry.Add(providerType, CreateDescriptor("Agnostic", Tenant.AgnosticTenantId));
        var updater = new WorkflowDefinitionActivityRegistryUpdater(provider, registry, Substitute.For<ICacheManager>(), tenantAccessor);

        await updater.ReconcileRegistryAsync();

        Assert.Null(registry.Find("Current"));
        Assert.Null(registry.Find("Agnostic"));
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            Assert.NotNull(registry.Find("Other"));
        }
    }

    [Fact]
    public async Task ReconcileRegistryAsync_ReplacesVisibleDescriptorsAndRetainsAgnosticDefinitions()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var store = new MemoryStore<WorkflowDefinition>();
        store.Add(CreateDefinition("agnostic-v1", "AgnosticDefinition", Tenant.AgnosticTenantId), x => x.Id);
        store.Add(CreateDefinition("tenant-v1", "TenantDefinition", "tenant-a"), x => x.Id);
        var workflowDefinitionStore = new MemoryWorkflowDefinitionStore(store, tenantAccessor);
        var provider = new WorkflowDefinitionActivityProvider(workflowDefinitionStore, new WorkflowDefinitionActivityDescriptorFactory(), tenantAccessor);
        var registry = new ActivityRegistry(Substitute.For<IActivityDescriber>(), [], tenantAccessor, NullLogger<ActivityRegistry>.Instance);
        var providerType = typeof(WorkflowDefinitionActivityProvider);
        registry.Add(providerType, CreateDescriptor("Stale", "tenant-a"));
        registry.Add(providerType, CreateDescriptor("AgnosticDefinition", Tenant.AgnosticTenantId));
        var cacheManager = Substitute.For<ICacheManager>();
        var updater = new WorkflowDefinitionActivityRegistryUpdater(provider, registry, cacheManager, tenantAccessor);

        await updater.ReconcileRegistryAsync();

        Assert.Null(registry.Find("Stale"));
        Assert.NotNull(registry.Find("TenantDefinition"));
        Assert.NotNull(registry.Find("AgnosticDefinition"));
        await cacheManager.Received(1).TriggerTokenAsync($"{typeof(CachingWorkflowDefinitionStore).FullName}:Reconcile:tenant-a", Arg.Any<CancellationToken>());
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            Assert.Null(registry.Find("TenantDefinition"));
            Assert.NotNull(registry.Find("AgnosticDefinition"));
        }
    }

    [Fact]
    public async Task ReconcileRegistryAsync_WhenProviderReadFails_PreservesExistingDescriptors()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
        workflowDefinitionStore.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns<Task<IEnumerable<WorkflowDefinition>>>(_ => throw new InvalidOperationException("Store unavailable."));
        var provider = new WorkflowDefinitionActivityProvider(workflowDefinitionStore, new WorkflowDefinitionActivityDescriptorFactory(), tenantAccessor);
        var registry = new ActivityRegistry(Substitute.For<IActivityDescriber>(), [], tenantAccessor, NullLogger<ActivityRegistry>.Instance);
        var providerType = typeof(WorkflowDefinitionActivityProvider);
        registry.Add(providerType, CreateDescriptor("Existing", "tenant-a"));
        var updater = new WorkflowDefinitionActivityRegistryUpdater(provider, registry, Substitute.For<ICacheManager>(), tenantAccessor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => updater.ReconcileRegistryAsync());

        Assert.NotNull(registry.Find("Existing"));
    }

    [Fact]
    public async Task ReconcileRegistryAsync_SlowTenantReadDoesNotBlockAnotherTenant()
    {
        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRead = new TaskCompletionSource<IEnumerable<WorkflowDefinition>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowStore = Substitute.For<IWorkflowDefinitionStore>();
        slowStore.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                readStarted.TrySetResult();
                return releaseRead.Task;
            });
        var tenantA = new TestTenantAccessor("tenant-a");
        var tenantB = new TestTenantAccessor("tenant-b");
        var updaterA = CreateUpdater(slowStore, tenantA);
        var updaterB = CreateUpdater(new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>(), tenantB), tenantB);

        var slowReconciliation = updaterA.ReconcileRegistryAsync();
        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await updaterB.ReconcileRegistryAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            releaseRead.TrySetResult([]);
            await slowReconciliation;
        }
    }

    private static WorkflowDefinitionActivityRegistryUpdater CreateUpdater(IWorkflowDefinitionStore store, TestTenantAccessor tenantAccessor)
    {
        var provider = new WorkflowDefinitionActivityProvider(store, new WorkflowDefinitionActivityDescriptorFactory(), tenantAccessor);
        var registry = new ActivityRegistry(Substitute.For<IActivityDescriber>(), [], tenantAccessor, NullLogger<ActivityRegistry>.Instance);
        return new WorkflowDefinitionActivityRegistryUpdater(provider, registry, Substitute.For<ICacheManager>(), tenantAccessor);
    }

    private static WorkflowDefinition CreateDefinition(string id, string name, string tenantId) => new()
    {
        Id = id,
        DefinitionId = id,
        Name = name,
        TenantId = tenantId,
        Version = 1,
        Options = new() { UsableAsActivity = true },
        Inputs = [],
        Outputs = [],
        Outcomes = [],
        Variables = [],
        CustomProperties = new Dictionary<string, object>()
    };

    private static ActivityDescriptor CreateDescriptor(string typeName, string tenantId) => new()
    {
        TypeName = typeName,
        Name = typeName,
        TenantId = tenantId,
        Version = 1
    };
}
