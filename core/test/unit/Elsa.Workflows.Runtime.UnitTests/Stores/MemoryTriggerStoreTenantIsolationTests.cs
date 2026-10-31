using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

/// <summary>
/// Memory must honor ambient tenant + <see cref="TriggerFilter.TenantAgnostic"/>
/// the same way EF does via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>.
/// </summary>
public class MemoryTriggerStoreTenantIsolationTests
{
    [Fact(DisplayName = "FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_WhenNotTenantAgnostic_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new TriggerFilter())).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "id-a");
        Assert.Contains(found, x => x.Id == "id-star");
        Assert.DoesNotContain(found, x => x.Id == "id-b");
    }

    [Fact(DisplayName = "FindManyAsync with TenantAgnostic returns every tenant")]
    public async Task FindManyAsync_WhenTenantAgnostic_ReturnsAllTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();

        Assert.Equal(3, found.Count);
        Assert.Contains(found, x => x.Id == "id-a");
        Assert.Contains(found, x => x.Id == "id-b");
        Assert.Contains(found, x => x.Id == "id-star");
    }

    [Fact(DisplayName = "FindAsync does not return another tenant's row by Id")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new TriggerFilter { Id = "id-b" });

        Assert.Null(found);
    }

    [Fact(DisplayName = "DeleteManyAsync does not remove another tenant's rows")]
    public async Task DeleteManyAsync_DoesNotDeleteOtherTenantRows()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var deleted = await store.DeleteManyAsync(new TriggerFilter());
        var remaining = (await store.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();

        Assert.Equal(2, deleted);
        Assert.Single(remaining);
        Assert.Equal("id-b", remaining[0].Id);
    }

    [Fact(DisplayName = "FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = new MemoryTriggerStore(new MemoryStore<StoredTrigger>());
        var unassigned = Trigger("id-null");
        unassigned.TenantId = null;
        var other = Trigger("id-a", hash: "hash-a");
        other.TenantId = "tenant-a";
        await store.SaveAsync(unassigned);
        await store.SaveAsync(other);

        var found = (await store.FindManyAsync(new TriggerFilter())).ToList();

        Assert.Single(found);
        Assert.Equal("id-null", found[0].Id);
    }

    private static MemoryTriggerStore CreateStore(string tenantId) =>
        new(new MemoryStore<StoredTrigger>(), new TestTenantAccessor(tenantId));

    private static async Task SeedMixedTenantsAsync(MemoryTriggerStore store)
    {
        await store.SaveAsync(Trigger("id-a", tenantId: "tenant-a", hash: "hash-a"));
        await store.SaveAsync(Trigger("id-b", tenantId: "tenant-b", hash: "hash-b"));
        await store.SaveAsync(Trigger("id-star", tenantId: Tenant.AgnosticTenantId, hash: "hash-star"));
    }

    private static StoredTrigger Trigger(string id, string? tenantId = null, string hash = "hash-1") =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            WorkflowDefinitionId = "workflow-1",
            WorkflowDefinitionVersionId = "v1",
            ActivityId = id,
            Hash = hash,
            Name = "Elsa.HttpEndpoint"
        };
}
