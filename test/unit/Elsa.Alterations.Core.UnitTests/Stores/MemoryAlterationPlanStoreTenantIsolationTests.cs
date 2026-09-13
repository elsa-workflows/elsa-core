using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Stores;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Alterations.Core.UnitTests.Stores;

/// <summary>
/// Memory alteration plans must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>. Contracts have no TenantAgnostic flag.
/// </summary>
public class MemoryAlterationPlanStoreTenantIsolationTests
{
    [Fact(DisplayName = "FindAsync hides other tenants and keeps * visible")]
    public async Task FindAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var own = await store.FindAsync(new AlterationPlanFilter { Id = "plan-a" });
        var star = await store.FindAsync(new AlterationPlanFilter { Id = "plan-star" });
        var other = await store.FindAsync(new AlterationPlanFilter { Id = "plan-b" });

        Assert.NotNull(own);
        Assert.Equal("plan-a", own.Id);
        Assert.NotNull(star);
        Assert.Equal("plan-star", star.Id);
        Assert.Null(other);
    }

    [Fact(DisplayName = "CountAsync hides other tenants and keeps * visible")]
    public async Task CountAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountAsync(new AlterationPlanFilter());

        Assert.Equal(2, count);
    }

    [Fact(DisplayName = "CountAsync does not count another tenant's row by Id")]
    public async Task CountAsync_WhenOtherTenant_ReturnsZero()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountAsync(new AlterationPlanFilter { Id = "plan-b" });

        Assert.Equal(0, count);
    }

    [Fact(DisplayName = "FindAsync does not return a same-ID row after another tenant replaces it")]
    public async Task FindAsync_WhenSameIdWasReplacedByOtherTenant_ReturnsNullAndLeavesReplacement()
    {
        var backing = new MemoryStore<AlterationPlan>();
        var tenantA = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Plan("shared", "tenant-a"));
        await tenantB.SaveAsync(Plan("shared", "tenant-b"));

        var foundByA = await tenantA.FindAsync(new AlterationPlanFilter { Id = "shared" });
        var foundByB = await tenantB.FindAsync(new AlterationPlanFilter { Id = "shared" });

        Assert.Null(foundByA);
        Assert.NotNull(foundByB);
        Assert.Equal("tenant-b", foundByB.TenantId);
    }

    [Fact(DisplayName = "SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var plan = Plan("plan-new", tenantId: null);

        await store.SaveAsync(plan);

        Assert.Equal("tenant-a", plan.TenantId);
        Assert.Equal("tenant-a", (await store.FindAsync(new AlterationPlanFilter { Id = "plan-new" }))!.TenantId);
    }

    [Fact(DisplayName = "SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = Plan("plan-star", Tenant.AgnosticTenantId);
        var explicitTenant = Plan("plan-a", "tenant-a");

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        Assert.Equal(Tenant.AgnosticTenantId, agnostic.TenantId);
        Assert.Equal("tenant-a", explicitTenant.TenantId);
    }

    [Fact(DisplayName = "CountAsync on the default tenant includes null TenantId rows")]
    public async Task CountAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var count = await store.CountAsync(new AlterationPlanFilter());
        var found = await store.FindAsync(new AlterationPlanFilter { Id = "plan-null" });

        Assert.Equal(1, count);
        Assert.NotNull(found);
        Assert.Equal("plan-null", found.Id);
    }

    [Fact(DisplayName = "CountAsync on a named tenant hides null TenantId rows")]
    public async Task CountAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var count = await store.CountAsync(new AlterationPlanFilter());
        var found = await store.FindAsync(new AlterationPlanFilter { Id = "plan-a" });

        Assert.Equal(1, count);
        Assert.NotNull(found);
        Assert.Equal("plan-a", found.Id);
    }

    private static MemoryAlterationPlanStore CreateStore(string tenantId) =>
        new(new MemoryStore<AlterationPlan>(), new TestTenantAccessor(tenantId));

    private static MemoryAlterationPlanStore StoreWithPreassignedRows(string ambientTenantId)
    {
        var backing = new MemoryStore<AlterationPlan>();
        backing.Save(Plan("plan-null", tenantId: null), x => x.Id);
        backing.Save(Plan("plan-a", "tenant-a"), x => x.Id);
        return new MemoryAlterationPlanStore(backing, new TestTenantAccessor(ambientTenantId));
    }

    private static async Task SeedMixedTenantsAsync(MemoryAlterationPlanStore store)
    {
        await store.SaveAsync(Plan("plan-a", "tenant-a"));
        await store.SaveAsync(Plan("plan-b", "tenant-b"));
        await store.SaveAsync(Plan("plan-star", Tenant.AgnosticTenantId));
    }

    private static AlterationPlan Plan(string id, string? tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId
        };
}
