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
    [Fact(DisplayName = "ListAsync hides other tenants and keeps * visible")]
    public async Task ListAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.ListAsync()).Items.ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "label-a");
        Assert.Contains(found, x => x.Id == "label-star");
        Assert.DoesNotContain(found, x => x.Id == "label-b");
    }

    [Fact(DisplayName = "FindByIdAsync does not return another tenant's row")]
    public async Task FindByIdAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindByIdAsync("label-b");

        Assert.Null(found);
    }

    [Fact(DisplayName = "FindManyByIdAsync hides other tenants")]
    public async Task FindManyByIdAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyByIdAsync(["label-a", "label-b", "label-star"], CancellationToken.None)).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "label-a");
        Assert.Contains(found, x => x.Id == "label-star");
        Assert.DoesNotContain(found, x => x.Id == "label-b");
    }

    [Fact(DisplayName = "DeleteAsync does not remove another tenant's rows or associations")]
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

        Assert.False(deleted);
        Assert.NotNull(remainingForB);
        Assert.Equal("label-b", remainingForB.Id);
        Assert.Single(remainingAssociations);
        Assert.Equal("assoc-b", remainingAssociations[0].Id);
    }

    [Fact(DisplayName = "DeleteManyAsync does not wipe other tenants' rows")]
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

        Assert.Equal(2, deleted);
        Assert.Empty(remainingForA);
        Assert.NotNull(remainingForB);
        Assert.Equal("label-b", remainingForB.Id);
    }

    [Fact(DisplayName = "SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var label = new Label { Id = "label-new", Name = "New" };

        await store.SaveAsync(label);

        Assert.Equal("tenant-a", label.TenantId);
        Assert.Equal("tenant-a", (await store.FindByIdAsync("label-new"))!.TenantId);
    }

    [Fact(DisplayName = "SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = new Label { Id = "label-star", Name = "Star", TenantId = Tenant.AgnosticTenantId };
        var explicitTenant = new Label { Id = "label-a", Name = "A", TenantId = "tenant-a" };

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        Assert.Equal(Tenant.AgnosticTenantId, agnostic.TenantId);
        Assert.Equal("tenant-a", explicitTenant.TenantId);
    }

    [Fact(DisplayName = "ListAsync on the default tenant includes null TenantId rows")]
    public async Task ListAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var found = (await store.ListAsync()).Items.ToList();

        Assert.Single(found);
        Assert.Equal("label-null", found[0].Id);
    }

    [Fact(DisplayName = "ListAsync on a named tenant hides null TenantId rows")]
    public async Task ListAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var found = (await store.ListAsync()).Items.ToList();

        Assert.Single(found);
        Assert.Equal("label-a", found[0].Id);
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
