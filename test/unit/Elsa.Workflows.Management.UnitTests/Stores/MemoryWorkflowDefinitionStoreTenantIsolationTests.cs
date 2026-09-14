using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using System.Threading.Tasks;

namespace Elsa.Workflows.Management.UnitTests.Stores;

/// <summary>
/// Memory must honor ambient tenant + <see cref="WorkflowDefinitionFilter.TenantAgnostic"/>
/// the same way EF does via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>.
/// </summary>
public class MemoryWorkflowDefinitionStoreTenantIsolationTests
{
    [Test]
    [DisplayName("FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_WhenNotTenantAgnostic_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter())).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "def-a");
        await Assert.That(found).Contains(x => x.Id == "def-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "def-b");
    }

    [Test]
    [DisplayName("FindManyAsync with TenantAgnostic returns every tenant")]
    public async Task FindManyAsync_WhenTenantAgnostic_ReturnsAllTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();

        await Assert.That(found.Count).IsEqualTo(3);
        await Assert.That(found).Contains(x => x.Id == "def-a");
        await Assert.That(found).Contains(x => x.Id == "def-b");
        await Assert.That(found).Contains(x => x.Id == "def-star");
    }

    [Test]
    [DisplayName("FindAsync does not return another tenant's row by Id")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new WorkflowDefinitionFilter { Id = "def-b" });

        await Assert.That(found).IsNull();
    }

    [Test]
    [DisplayName("AnyAsync is false when only another tenant matches")]
    public async Task AnyAsync_WhenOnlyOtherTenantMatches_ReturnsFalse()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var exists = await store.AnyAsync(new WorkflowDefinitionFilter { Id = "def-b" });

        await Assert.That(exists).IsFalse();
    }

    [Test]
    [DisplayName("CountDistinctAsync counts only the current tenant's definition IDs")]
    public async Task CountDistinctAsync_CountsOnlyVisibleDefinitions()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountDistinctAsync();

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    [DisplayName("GetIsNameUnique allows the same name in another tenant")]
    public async Task GetIsNameUnique_AllowsSameNameInAnotherTenant()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Definition("def-b", "order", "tenant-b"));

        var unique = await store.GetIsNameUnique("order");

        await Assert.That(unique).IsTrue();
    }

    [Test]
    [DisplayName("GetIsNameUnique is false when the current tenant already has the name")]
    public async Task GetIsNameUnique_WhenCurrentTenantOwnsTheName_ReturnsFalse()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Definition("def-a", "order", "tenant-a"));

        var unique = await store.GetIsNameUnique("order");

        await Assert.That(unique).IsFalse();
    }

    [Test]
    [DisplayName("DeleteAsync does not remove another tenant's rows")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var deleted = await store.DeleteAsync(new WorkflowDefinitionFilter());
        var remaining = (await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();

        await Assert.That(deleted).IsEqualTo(2);
        await Assert.That(remaining).HasSingleItem();
        await Assert.That(remaining[0].Id).IsEqualTo("def-b");
    }

    [Test]
    [DisplayName("DeleteAsync with a shared DefinitionId leaves the other tenant's versions")]
    public async Task DeleteAsync_WhenDefinitionIdIsShared_LeavesOtherTenantRows()
    {
        var backing = new MemoryStore<WorkflowDefinition>();
        var tenantA = new MemoryWorkflowDefinitionStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryWorkflowDefinitionStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Definition("def-a", "Order", "tenant-a", definitionId: "order"));
        await tenantB.SaveAsync(Definition("def-b", "Order", "tenant-b", definitionId: "order"));

        var deleted = await tenantA.DeleteAsync(new WorkflowDefinitionFilter { DefinitionId = "order" });
        var remainingForA = (await tenantA.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = "order" })).ToList();
        var remainingForB = (await tenantB.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = "order" })).ToList();

        await Assert.That(deleted).IsEqualTo(1);
        await Assert.That(remainingForA).IsEmpty();
        await Assert.That(remainingForB).HasSingleItem();
        await Assert.That(remainingForB[0].Id).IsEqualTo("def-b");
    }

    [Test]
    [DisplayName("FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = CreateStore(Tenant.DefaultTenantId);
        await store.SaveAsync(Definition("def-null", "Null", tenantId: null));
        await store.SaveAsync(Definition("def-a", "A", "tenant-a"));

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter())).ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("def-null");
    }

    [Test]
    [DisplayName("FindManyAsync on a named tenant hides null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Definition("def-null", "Null", tenantId: null));
        await store.SaveAsync(Definition("def-a", "A", "tenant-a"));

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter())).ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("def-a");
    }

    private static MemoryWorkflowDefinitionStore CreateStore(string tenantId) =>
        new(new MemoryStore<WorkflowDefinition>(), new TestTenantAccessor(tenantId));

    private static async Task SeedMixedTenantsAsync(MemoryWorkflowDefinitionStore store)
    {
        await store.SaveAsync(Definition("def-a", "A", "tenant-a"));
        await store.SaveAsync(Definition("def-b", "B", "tenant-b"));
        await store.SaveAsync(Definition("def-star", "Star", Tenant.AgnosticTenantId));
    }

    private static WorkflowDefinition Definition(string id, string name, string? tenantId, string? definitionId = null) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId ?? id,
            Name = name,
            TenantId = tenantId,
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };
}
