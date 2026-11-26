using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

/// <summary>
/// A tenant that overrides a tenant-agnostic definition does not see its shared triggers, and paged lookups such as the startup scheduling
/// still return full pages and exact counts.
/// </summary>
public class TenantOverrideTriggerStoreTests
{
    private readonly TenantOverrideTriggerStore _store;

    public TenantOverrideTriggerStoreTests()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var memoryStore = new MemoryStore<StoredTrigger>();
        var definitionStore = Substitute.For<IWorkflowDefinitionStore>();
        definitionStore.FindSummariesAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(
        [
            new WorkflowDefinitionSummary { Id = "shared-v1", DefinitionId = "overridden", IsPublished = true, TenantId = Tenant.AgnosticTenantId },
            new WorkflowDefinitionSummary { Id = "own-v1", DefinitionId = "overridden", IsPublished = true, TenantId = "tenant-a" }
        ]);
        var services = new ServiceCollection()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .AddSingleton(definitionStore)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .BuildServiceProvider();

        memoryStore.Save(Trigger("a-hidden", Tenant.AgnosticTenantId, "overridden", "shared-v1"), x => x.Id);
        memoryStore.Save(Trigger("b-own", "tenant-a", "overridden", "own-v1"), x => x.Id);
        memoryStore.Save(Trigger("c-shared", Tenant.AgnosticTenantId, "other", "other-v1"), x => x.Id);
        _store = new(new MemoryTriggerStore(memoryStore, tenantAccessor), services);
    }

    [Fact(DisplayName = "A lookup leaves out the shared triggers of an overridden definition")]
    public async Task FindMany_HidesSharedTriggersOfOverriddenDefinition()
    {
        var triggers = await _store.FindManyAsync(new TriggerFilter { Name = "Elsa.Timer" });

        Assert.Equal(["b-own", "c-shared"], triggers.Select(x => x.Id).Order());
    }

    [Fact(DisplayName = "A page is filled past a hidden trigger and counts only visible triggers")]
    public async Task FindManyPaged_FillsPagePastHiddenTriggers()
    {
        var page = await _store.FindManyAsync(new TriggerFilter { Name = "Elsa.Timer" }, PageArgs.FromRange(0, 1));
        var nextPage = await _store.FindManyAsync(new TriggerFilter { Name = "Elsa.Timer" }, PageArgs.FromRange(1, 1));

        Assert.Equal(["b-own"], page.Items.Select(x => x.Id));
        Assert.Equal(["c-shared"], nextPage.Items.Select(x => x.Id));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact(DisplayName = "An ordered page is filled past a hidden trigger and counts only visible triggers")]
    public async Task FindManyOrderedPaged_FillsPagePastHiddenTriggers()
    {
        var order = new StoredTriggerOrder<string>(x => x.Id, OrderDirection.Descending);

        var page = await _store.FindManyAsync(new TriggerFilter { Name = "Elsa.Timer" }, PageArgs.FromRange(1, 1), order);

        Assert.Equal(["b-own"], page.Items.Select(x => x.Id));
        Assert.Equal(2, page.TotalCount);
    }

    private static StoredTrigger Trigger(string id, string tenantId, string definitionId, string versionId) => new()
    {
        Id = id,
        TenantId = tenantId,
        WorkflowDefinitionId = definitionId,
        WorkflowDefinitionVersionId = versionId,
        Name = "Elsa.Timer",
        ActivityId = "timer"
    };
}
