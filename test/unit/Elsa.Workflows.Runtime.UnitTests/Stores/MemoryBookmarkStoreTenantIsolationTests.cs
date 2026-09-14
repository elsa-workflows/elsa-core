using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

/// <summary>
/// Memory must honor ambient tenant + <see cref="BookmarkFilter.TenantAgnostic"/>
/// the same way EF does via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>.
/// </summary>
public class MemoryBookmarkStoreTenantIsolationTests
{
    [Fact(DisplayName = "FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_WhenNotTenantAgnostic_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new BookmarkFilter())).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Id == "bm-a");
        Assert.Contains(found, x => x.Id == "bm-star");
        Assert.DoesNotContain(found, x => x.Id == "bm-b");
    }

    [Fact(DisplayName = "FindManyAsync with TenantAgnostic returns every tenant")]
    public async Task FindManyAsync_WhenTenantAgnostic_ReturnsAllTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new BookmarkFilter { TenantAgnostic = true })).ToList();

        Assert.Equal(3, found.Count);
        Assert.Contains(found, x => x.Id == "bm-a");
        Assert.Contains(found, x => x.Id == "bm-b");
        Assert.Contains(found, x => x.Id == "bm-star");
    }

    [Fact(DisplayName = "FindAsync does not return another tenant's row by Id")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new BookmarkFilter { BookmarkId = "bm-b" });

        Assert.Null(found);
    }

    [Fact(DisplayName = "DeleteAsync does not remove another tenant's rows")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var deleted = await store.DeleteAsync(new BookmarkFilter());
        var remaining = (await store.FindManyAsync(new BookmarkFilter { TenantAgnostic = true })).ToList();

        Assert.Equal(2, deleted);
        Assert.Single(remaining);
        Assert.Equal("bm-b", remaining[0].Id);
    }

    [Fact(DisplayName = "FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = CreateStore(Tenant.DefaultTenantId);
        await store.SaveAsync(Bookmark("bm-null", tenantId: null));
        await store.SaveAsync(Bookmark("bm-a", tenantId: "tenant-a"));

        var found = (await store.FindManyAsync(new BookmarkFilter())).ToList();

        Assert.Single(found);
        Assert.Equal("bm-null", found[0].Id);
    }

    [Fact(DisplayName = "FindManyAsync on a named tenant hides null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Bookmark("bm-null", tenantId: null));
        await store.SaveAsync(Bookmark("bm-a", tenantId: "tenant-a"));

        var found = (await store.FindManyAsync(new BookmarkFilter())).ToList();

        Assert.Single(found);
        Assert.Equal("bm-a", found[0].Id);
    }

    private static MemoryBookmarkStore CreateStore(string tenantId) =>
        new(new MemoryStore<StoredBookmark>(), new TestTenantAccessor(tenantId));

    private static async Task SeedMixedTenantsAsync(MemoryBookmarkStore store)
    {
        await store.SaveAsync(Bookmark("bm-a", tenantId: "tenant-a"));
        await store.SaveAsync(Bookmark("bm-b", tenantId: "tenant-b"));
        await store.SaveAsync(Bookmark("bm-star", tenantId: Tenant.AgnosticTenantId));
    }

    private static StoredBookmark Bookmark(string id, string? tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Hash = id,
            WorkflowInstanceId = "instance-1",
            Name = "Elsa.HttpEndpoint"
        };
}
