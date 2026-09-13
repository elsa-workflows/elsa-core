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
    [Fact(DisplayName = "FindByWorkflowDefinitionVersionIdAsync hides other tenants and keeps * visible")]
    public async Task FindByWorkflowDefinitionVersionIdAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindByWorkflowDefinitionVersionIdAsync("order:1")).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "assoc-a");
        Assert.Contains(found, x => x.Id == "assoc-star");
        Assert.DoesNotContain(found, x => x.Id == "assoc-b");
    }

    [Fact(DisplayName = "FindByLabelIdsAsync hides other tenants")]
    public async Task FindByLabelIdsAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindByLabelIdsAsync(["red"])).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "assoc-a");
        Assert.Contains(found, x => x.Id == "assoc-star");
        Assert.DoesNotContain(found, x => x.Id == "assoc-b");
    }

    [Fact(DisplayName = "DeleteAsync does not remove another tenant's row")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        var tenantA = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        var deleted = await tenantA.DeleteAsync("assoc-b");
        var remainingForB = (await tenantB.FindByLabelIdsAsync(["red"])).ToList();

        Assert.False(deleted);
        Assert.Single(remainingForB);
        Assert.Equal("assoc-b", remainingForB[0].Id);
    }

    [Fact(DisplayName = "DeleteByWorkflowDefinitionIdAsync leaves the other tenant's associations")]
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

        Assert.Equal(1, deleted);
        Assert.Empty(remainingForA);
        Assert.Single(remainingForB);
        Assert.Equal("assoc-b", remainingForB[0].Id);
    }

    [Fact(DisplayName = "ReplaceAsync does not delete another tenant's rows")]
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

        Assert.Contains(remainingForA, x => x.Id == "assoc-a2");
        Assert.Contains(remainingForA, x => x.Id == "assoc-star");
        Assert.DoesNotContain(remainingForA, x => x.Id == "assoc-a");
        Assert.Single(remainingForB);
        Assert.Equal("assoc-b", remainingForB[0].Id);
    }

    [Fact(DisplayName = "SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var association = Association("assoc-new", tenantId: null);

        await store.SaveAsync(association);

        Assert.Equal("tenant-a", association.TenantId);
        var found = (await store.FindByLabelIdsAsync(["red"])).Single();
        Assert.Equal("tenant-a", found.TenantId);
    }

    [Fact(DisplayName = "FindByLabelIdsAsync on the default tenant includes null TenantId rows")]
    public async Task FindByLabelIdsAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var backing = new MemoryStore<WorkflowDefinitionLabel>();
        backing.Save(Association("assoc-null", tenantId: null), x => x.Id);
        backing.Save(Association("assoc-a", "tenant-a"), x => x.Id);
        var store = new InMemoryWorkflowDefinitionLabelStore(backing, new TestTenantAccessor(Tenant.DefaultTenantId));

        var found = (await store.FindByLabelIdsAsync(["red"])).ToList();

        Assert.Single(found);
        Assert.Equal("assoc-null", found[0].Id);
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
