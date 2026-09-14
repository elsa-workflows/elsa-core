using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;
using System.Threading.Tasks;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

/// <summary>
/// Memory must honor ambient tenant + <see cref="TriggerFilter.TenantAgnostic"/>
/// the same way EF does via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>.
/// </summary>
public class MemoryTriggerStoreTenantIsolationTests
{
    [Test]
    [DisplayName("FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_WhenNotTenantAgnostic_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new TriggerFilter())).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "id-a");
        await Assert.That(found).Contains(x => x.Id == "id-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "id-b");
    }

    [Test]
    [DisplayName("FindManyAsync with TenantAgnostic returns every tenant")]
    public async Task FindManyAsync_WhenTenantAgnostic_ReturnsAllTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();

        await Assert.That(found.Count).IsEqualTo(3);
        await Assert.That(found).Contains(x => x.Id == "id-a");
        await Assert.That(found).Contains(x => x.Id == "id-b");
        await Assert.That(found).Contains(x => x.Id == "id-star");
    }

    [Test]
    [DisplayName("FindAsync does not return another tenant's row by Id")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new TriggerFilter { Id = "id-b" });

        await Assert.That(found).IsNull();
    }

    [Test]
    [DisplayName("DeleteManyAsync does not remove another tenant's rows")]
    public async Task DeleteManyAsync_DoesNotDeleteOtherTenantRows()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var deleted = await store.DeleteManyAsync(new TriggerFilter());
        var remaining = (await store.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();

        await Assert.That(deleted).IsEqualTo(2);
        await Assert.That(remaining).HasSingleItem();
        await Assert.That(remaining[0].Id).IsEqualTo("id-b");
    }

    [Test]
    [DisplayName("FindManyAsync on the default tenant includes null TenantId rows")]
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

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("id-null");
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
