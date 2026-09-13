using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;

namespace Elsa.Workflows.Management.UnitTests.Stores;

/// <summary>
/// Memory must honor ambient tenant + <see cref="WorkflowDefinitionFilter.TenantAgnostic"/>
/// the same way EF does via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>.
/// </summary>
public class MemoryWorkflowDefinitionStoreTenantIsolationTests
{
    [Fact(DisplayName = "FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_WhenNotTenantAgnostic_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter())).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "def-a");
        Assert.Contains(found, x => x.Id == "def-star");
        Assert.DoesNotContain(found, x => x.Id == "def-b");
    }

    [Fact(DisplayName = "FindManyAsync with TenantAgnostic returns every tenant")]
    public async Task FindManyAsync_WhenTenantAgnostic_ReturnsAllTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();

        Assert.Equal(3, found.Count);
        Assert.Contains(found, x => x.Id == "def-a");
        Assert.Contains(found, x => x.Id == "def-b");
        Assert.Contains(found, x => x.Id == "def-star");
    }

    [Fact(DisplayName = "FindAsync does not return another tenant's row by Id")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new WorkflowDefinitionFilter { Id = "def-b" });

        Assert.Null(found);
    }

    [Fact(DisplayName = "AnyAsync is false when only another tenant matches")]
    public async Task AnyAsync_WhenOnlyOtherTenantMatches_ReturnsFalse()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var exists = await store.AnyAsync(new WorkflowDefinitionFilter { Id = "def-b" });

        Assert.False(exists);
    }

    [Fact(DisplayName = "CountDistinctAsync counts only the current tenant's definition IDs")]
    public async Task CountDistinctAsync_CountsOnlyVisibleDefinitions()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountDistinctAsync();

        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "GetIsNameUnique allows the same name in another tenant")]
    public async Task GetIsNameUnique_AllowsSameNameInAnotherTenant()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Definition("def-b", "order", "tenant-b"));

        var unique = await store.GetIsNameUnique("order");

        Assert.True(unique);
    }

    [Fact(DisplayName = "GetIsNameUnique is false when the current tenant already has the name")]
    public async Task GetIsNameUnique_WhenCurrentTenantOwnsTheName_ReturnsFalse()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Definition("def-a", "order", "tenant-a"));

        var unique = await store.GetIsNameUnique("order");

        Assert.False(unique);
    }

    [Fact(DisplayName = "DeleteAsync does not remove another tenant's rows")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var deleted = await store.DeleteAsync(new WorkflowDefinitionFilter());
        var remaining = (await store.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();

        Assert.Equal(2, deleted);
        Assert.Single(remaining);
        Assert.Equal("def-b", remaining[0].Id);
    }

    [Fact(DisplayName = "FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = CreateStore(Tenant.DefaultTenantId);
        await store.SaveAsync(Definition("def-null", "Null", tenantId: null));
        await store.SaveAsync(Definition("def-a", "A", "tenant-a"));

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter())).ToList();

        Assert.Single(found);
        Assert.Equal("def-null", found[0].Id);
    }

    [Fact(DisplayName = "FindManyAsync on a named tenant hides null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Definition("def-null", "Null", tenantId: null));
        await store.SaveAsync(Definition("def-a", "A", "tenant-a"));

        var found = (await store.FindManyAsync(new WorkflowDefinitionFilter())).ToList();

        Assert.Single(found);
        Assert.Equal("def-a", found[0].Id);
    }

    private static MemoryWorkflowDefinitionStore CreateStore(string tenantId) =>
        new(new MemoryStore<WorkflowDefinition>(), new TestTenantAccessor(tenantId));

    private static async Task SeedMixedTenantsAsync(MemoryWorkflowDefinitionStore store)
    {
        await store.SaveAsync(Definition("def-a", "A", "tenant-a"));
        await store.SaveAsync(Definition("def-b", "B", "tenant-b"));
        await store.SaveAsync(Definition("def-star", "Star", Tenant.AgnosticTenantId));
    }

    private static WorkflowDefinition Definition(string id, string name, string? tenantId) =>
        new()
        {
            Id = id,
            DefinitionId = id,
            Name = name,
            TenantId = tenantId,
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };
}
