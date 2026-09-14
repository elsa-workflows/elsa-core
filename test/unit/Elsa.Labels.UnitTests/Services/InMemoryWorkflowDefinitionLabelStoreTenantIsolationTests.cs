using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Labels.UnitTests.Services;

/// <summary>
/// Memory workflow-definition labels must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>. Contracts have no TenantAgnostic flag.
/// </summary>
public class InMemoryWorkflowDefinitionLabelStoreTenantIsolationTests
{
    [Test]
    [DisplayName("FindByWorkflowDefinitionVersionIdAsync hides other tenants and keeps * visible")]
    public async Task FindByWorkflowDefinitionVersionIdAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "assoc-a");
        await Assert.That(found).Contains(x => x.Id == "assoc-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "assoc-b");
    }

    [Test]
    [DisplayName("FindByLabelIdsAsync hides other tenants")]
    public async Task FindByLabelIdsAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindByLabelIdsAsync(["red"])).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "assoc-a");
        await Assert.That(found).Contains(x => x.Id == "assoc-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "assoc-b");
    }

    [Test]
    [DisplayName("DeleteAsync does not remove another tenant's row")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        var deleted = await tenantA.DeleteAsync("assoc-b");
        var remainingForB = (await tenantB.FindByLabelIdsAsync(["red"])).ToList();

        await Assert.That(deleted).IsFalse();
        await Assert.That(remainingForB).Contains(x => x.Id == "assoc-b");
        await Assert.That(remainingForB).Contains(x => x.Id == "assoc-star");
        await Assert.That(remainingForB).DoesNotContain(x => x.Id == "assoc-a");
    }

    [Test]
    [DisplayName("DeleteAsync leaves a same-ID row after another tenant replaces it")]
    public async Task DeleteAsync_WhenSameIdWasReplacedByOtherTenant_LeavesReplacement()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Association("shared", "tenant-a"));
        await tenantB.SaveAsync(Association("shared", "tenant-b"));

        var deleted = await tenantA.DeleteAsync("shared");
        var remaining = (await tenantB.FindByLabelIdsAsync(["red"])).ToList();

        await Assert.That(deleted).IsFalse();
        await Assert.That(remaining).HasSingleItem();
        await Assert.That(remaining[0].Id).IsEqualTo("shared");
        await Assert.That(remaining[0].TenantId).IsEqualTo("tenant-b");
    }

    [Test]
    [DisplayName("DeleteByWorkflowDefinitionIdAsync leaves the other tenant's associations")]
    public async Task DeleteByWorkflowDefinitionIdAsync_WhenDefinitionIdIsShared_LeavesOtherTenantRows()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Association("assoc-a", "tenant-a"));
        await tenantB.SaveAsync(Association("assoc-b", "tenant-b"));

        var deleted = await tenantA.DeleteByWorkflowDefinitionIdAsync("order");
        var remainingForA = (await tenantA.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        var remainingForB = (await tenantB.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();

        await Assert.That(deleted).IsEqualTo(1);
        await Assert.That(remainingForA).IsEmpty();
        await Assert.That(remainingForB).HasSingleItem();
        await Assert.That(remainingForB[0].Id).IsEqualTo("assoc-b");
    }

    [Test]
    [DisplayName("ReplaceAsync does not delete another tenant's rows")]
    public async Task ReplaceAsync_DoesNotDeleteOtherTenantRows()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        await tenantA.ReplaceAsync(
            [Association("assoc-a", "tenant-a"), Association("assoc-b", "tenant-b")],
            [Association("assoc-a2", "tenant-a")]);

        var remainingForA = (await tenantA.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();
        var remainingForB = (await tenantB.FindByLabelIdsAsync(["red"])).ToList();

        await Assert.That(remainingForA).Contains(x => x.Id == "assoc-a2");
        await Assert.That(remainingForA).Contains(x => x.Id == "assoc-star");
        await Assert.That(remainingForA).DoesNotContain(x => x.Id == "assoc-a");
        await Assert.That(remainingForB).Contains(x => x.Id == "assoc-b");
        await Assert.That(remainingForB).Contains(x => x.Id == "assoc-star");
        await Assert.That(remainingForB).DoesNotContain(x => x.Id == "assoc-a");
    }

    [Test]
    [DisplayName("SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var association = Association("assoc-new", tenantId: null);

        await store.SaveAsync(association);

        await Assert.That(association.TenantId).IsEqualTo("tenant-a");
        var found = (await store.FindByLabelIdsAsync(["red"])).Single();
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("FindByLabelIdsAsync on the default tenant includes null TenantId rows")]
    public async Task FindByLabelIdsAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        backing.Save(Association("assoc-null", tenantId: null), x => x.Id);
        backing.Save(Association("assoc-a", "tenant-a"), x => x.Id);
        var store = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor(Tenant.DefaultTenantId));

        var found = (await store.FindByLabelIdsAsync(["red"])).ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("assoc-null");
    }

    private static InMemoryWorkflowDefinitionLabelStore CreateStore(string tenantId) =>
        new(new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor(tenantId));

    private static async Task SeedMixedTenantsAsync(InMemoryWorkflowDefinitionLabelStore store)
    {
        await store.SaveAsync(Association("assoc-a", "tenant-a"));
        await store.SaveAsync(Association("assoc-b", "tenant-b"));
        await store.SaveAsync(Association("assoc-star", Tenant.AgnosticTenantId));
    }

    private static WorkflowDefinitionLabel Association(string id, string? tenantId) =>
        new()
        {
            Id = id,
            LabelId = "red",
            WorkflowDefinitionId = "order",
            WorkflowDefinitionVersionId = "order:1",
            TenantId = tenantId
        };
}
