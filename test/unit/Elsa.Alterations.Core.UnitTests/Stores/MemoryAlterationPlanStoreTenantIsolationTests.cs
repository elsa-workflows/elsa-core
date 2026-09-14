using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
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
    [Test]
    [DisplayName("FindAsync hides other tenants and keeps * visible")]
    public async Task FindAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var own = await store.FindAsync(new AlterationPlanFilter { Id = "plan-a" });
        var star = await store.FindAsync(new AlterationPlanFilter { Id = "plan-star" });
        var other = await store.FindAsync(new AlterationPlanFilter { Id = "plan-b" });

        await Assert.That(own).IsNotNull();
        await Assert.That(own.Id).IsEqualTo("plan-a");
        await Assert.That(star).IsNotNull();
        await Assert.That(star.Id).IsEqualTo("plan-star");
        await Assert.That(other).IsNull();
    }

    [Test]
    [DisplayName("CountAsync hides other tenants and keeps * visible")]
    public async Task CountAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountAsync(new AlterationPlanFilter());

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    [DisplayName("CountAsync does not count another tenant's row by Id")]
    public async Task CountAsync_WhenOtherTenant_ReturnsZero()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var count = await store.CountAsync(new AlterationPlanFilter { Id = "plan-b" });

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    [DisplayName("SaveAsync refuses to overwrite another tenant's row by Id")]
    public async Task SaveAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationPlan>();
        var tenantA = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Plan("shared", "tenant-a"));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(Plan("shared", "tenant-b")));
        var remaining = await tenantA.FindAsync(new AlterationPlanFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync refuses a forged owner TenantId from another ambient tenant")]
    public async Task SaveAsync_WhenAmbientForgesOwnerTenantId_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationPlan>();
        var tenantA = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Plan("shared", "tenant-a"));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(Plan("shared", "tenant-a")));
        var remaining = await tenantA.FindAsync(new AlterationPlanFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync refuses to overwrite a tenant-agnostic row by Id")]
    public async Task SaveAsync_WhenAgnosticRowExists_NamedTenantThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationPlan>();
        var agnostic = new MemoryAlterationPlanStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        var tenantB = new MemoryAlterationPlanStore(backing, new TestTenantAccessor("tenant-b"));
        await agnostic.SaveAsync(Plan("shared", Tenant.AgnosticTenantId));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tenantB.SaveAsync(Plan("shared", "tenant-b")));
        var remaining = await agnostic.FindAsync(new AlterationPlanFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveAsync refuses a named source when an agnostic writer updates a * row")]
    public async Task SaveAsync_WhenAgnosticAmbientReceivesNamedSource_ThrowsAndLeavesExisting()
    {
        var backing = new MemoryStore<AlterationPlan>();
        var agnostic = new MemoryAlterationPlanStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        await agnostic.SaveAsync(Plan("shared", Tenant.AgnosticTenantId));

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => agnostic.SaveAsync(Plan("shared", "tenant-b")));
        var remaining = await agnostic.FindAsync(new AlterationPlanFilter { Id = "shared" });

        await Assert.That(ex.Message).Contains("shared");
        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveAsync still lets an agnostic writer update a * row")]
    public async Task SaveAsync_WhenAmbientIsAgnostic_UpsertsAgnosticRow()
    {
        var store = CreateStore(Tenant.AgnosticTenantId);
        await store.SaveAsync(Plan("shared", Tenant.AgnosticTenantId));
        var updated = Plan("shared", Tenant.AgnosticTenantId);
        updated.Status = AlterationPlanStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationPlanFilter { Id = "shared" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationPlanStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
    }

    [Test]
    [DisplayName("SaveAsync still upserts a visible same-tenant row")]
    public async Task SaveAsync_WhenSameTenantOwnsId_Upserts()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Plan("plan-a", "tenant-a"));
        var updated = Plan("plan-a", "tenant-a");
        updated.Status = AlterationPlanStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationPlanFilter { Id = "plan-a" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationPlanStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync preserves the stored TenantId on an accepted update")]
    public async Task SaveAsync_WhenIncomingTenantDiffers_PreservesExistingTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Plan("plan-a", "tenant-a"));
        var updated = Plan("plan-a", "tenant-b");
        updated.Status = AlterationPlanStatus.Completed;

        await store.SaveAsync(updated);

        var found = await store.FindAsync(new AlterationPlanFilter { Id = "plan-a" });
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Status).IsEqualTo(AlterationPlanStatus.Completed);
        await Assert.That(found.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var plan = Plan("plan-new", tenantId: null);

        await store.SaveAsync(plan);

        await Assert.That(plan.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await store.FindAsync(new AlterationPlanFilter { Id = "plan-new" }))!.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = Plan("plan-star", Tenant.AgnosticTenantId);
        var explicitTenant = Plan("plan-a", "tenant-a");

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        await Assert.That(agnostic.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That(explicitTenant.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("CountAsync on the default tenant includes null TenantId rows")]
    public async Task CountAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var count = await store.CountAsync(new AlterationPlanFilter());
        var found = await store.FindAsync(new AlterationPlanFilter { Id = "plan-null" });

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Id).IsEqualTo("plan-null");
    }

    [Test]
    [DisplayName("CountAsync on a named tenant hides null TenantId rows")]
    public async Task CountAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var count = await store.CountAsync(new AlterationPlanFilter());
        var found = await store.FindAsync(new AlterationPlanFilter { Id = "plan-a" });

        await Assert.That(count).IsEqualTo(1);
        await Assert.That(found).IsNotNull();
        await Assert.That(found.Id).IsEqualTo("plan-a");
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
