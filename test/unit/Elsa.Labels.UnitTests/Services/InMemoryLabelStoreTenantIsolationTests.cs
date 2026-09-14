using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Labels.UnitTests.Services;

/// <summary>
/// Memory Labels must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>. Contracts have no TenantAgnostic flag.
/// </summary>
public class InMemoryLabelStoreTenantIsolationTests
{
    [Test]
    [DisplayName("ListAsync hides other tenants and keeps * visible")]
    public async Task ListAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.ListAsync()).Items.ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "label-a");
        await Assert.That(found).Contains(x => x.Id == "label-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "label-b");
    }

    [Test]
    [DisplayName("FindByIdAsync does not return another tenant's row")]
    public async Task FindByIdAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindByIdAsync("label-b");

        await Assert.That(found).IsNull();
    }

    [Test]
    [DisplayName("FindManyByIdAsync hides other tenants")]
    public async Task FindManyByIdAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyByIdAsync(["label-a", "label-b", "label-star"], CancellationToken.None)).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "label-a");
        await Assert.That(found).Contains(x => x.Id == "label-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "label-b");
    }

    [Test]
    [DisplayName("DeleteAsync does not remove another tenant's rows or associations")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var labels = new MemoryStore<Label>();
        var associations = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-b"));
        var tenantBAssociations = new InMemoryWorkflowDefinitionLabelStore(associations, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);
        await tenantBAssociations.SaveAsync(Association("assoc-b", "label-b", "tenant-b"));

        var deleted = await tenantA.DeleteAsync("label-b");
        var remainingForB = await tenantB.FindByIdAsync("label-b");
        var remainingAssociations = (await tenantBAssociations.FindByLabelIdsAsync(["label-b"])).ToList();

        await Assert.That(deleted).IsFalse();
        await Assert.That(remainingForB).IsNotNull();
        await Assert.That(remainingForB.Id).IsEqualTo("label-b");
        await Assert.That(remainingAssociations).HasSingleItem();
        await Assert.That(remainingAssociations[0].Id).IsEqualTo("assoc-b");
    }

    [Test]
    [DisplayName("DeleteManyAsync does not wipe other tenants' rows")]
    public async Task DeleteManyAsync_DoesNotWipeOtherTenantRows()
    {
        var labels = new MemoryStore<Label>();
        var associations = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        var deleted = await tenantA.DeleteManyAsync(["label-a", "label-b", "label-star"]);
        var remainingForA = (await tenantA.ListAsync()).Items.ToList();
        var remainingForB = await tenantB.FindByIdAsync("label-b");

        await Assert.That(deleted).IsEqualTo(2);
        await Assert.That(remainingForA).IsEmpty();
        await Assert.That(remainingForB).IsNotNull();
        await Assert.That(remainingForB.Id).IsEqualTo("label-b");
    }

    [Test]
    [DisplayName("DeleteAsync leaves a same-ID row after another tenant replaces it")]
    public async Task DeleteAsync_WhenSameIdWasReplacedByOtherTenant_LeavesReplacement()
    {
        var labels = new MemoryStore<Label>();
        var associations = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryLabelStore(labels, associations, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(new Label { Id = "shared", Name = "A", TenantId = "tenant-a" });
        await tenantB.SaveAsync(new Label { Id = "shared", Name = "B", TenantId = "tenant-b" });

        var deleted = await tenantA.DeleteAsync("shared");
        var remaining = await tenantB.FindByIdAsync("shared");

        await Assert.That(deleted).IsFalse();
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-b");
    }

    [Test]
    [DisplayName("SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var label = new Label { Id = "label-new", Name = "New" };

        await store.SaveAsync(label);

        await Assert.That(label.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await store.FindByIdAsync("label-new"))!.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = new Label { Id = "label-star", Name = "Star", TenantId = Tenant.AgnosticTenantId };
        var explicitTenant = new Label { Id = "label-a", Name = "A", TenantId = "tenant-a" };

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        await Assert.That(agnostic.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That(explicitTenant.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("ListAsync on the default tenant includes null TenantId rows")]
    public async Task ListAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var found = (await store.ListAsync()).Items.ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("label-null");
    }

    [Test]
    [DisplayName("ListAsync on a named tenant hides null TenantId rows")]
    public async Task ListAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var found = (await store.ListAsync()).Items.ToList();

        await Assert.That(found).HasSingleItem();
        await Assert.That(found[0].Id).IsEqualTo("label-a");
    }

    private static InMemoryLabelStore CreateStore(string tenantId) =>
        new(new MemoryStore<Label>(), new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor(tenantId));

    private static InMemoryLabelStore StoreWithPreassignedRows(string ambientTenantId)
    {
        var labels = new MemoryStore<Label>();
        labels.Save(new Label { Id = "label-null", Name = "Null", TenantId = null }, x => x.Id);
        labels.Save(new Label { Id = "label-a", Name = "A", TenantId = "tenant-a" }, x => x.Id);
        return new InMemoryLabelStore(labels, new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor(ambientTenantId));
    }

    private static async Task SeedMixedTenantsAsync(InMemoryLabelStore store)
    {
        await store.SaveAsync(new Label { Id = "label-a", Name = "A", TenantId = "tenant-a" });
        await store.SaveAsync(new Label { Id = "label-b", Name = "B", TenantId = "tenant-b" });
        await store.SaveAsync(new Label { Id = "label-star", Name = "Star", TenantId = Tenant.AgnosticTenantId });
    }

    private static WorkflowDefinitionLabel Association(string id, string labelId, string tenantId) =>
        new()
        {
            Id = id,
            LabelId = labelId,
            WorkflowDefinitionId = "order",
            WorkflowDefinitionVersionId = "order:1",
            TenantId = tenantId
        };
}
