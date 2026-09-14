using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// Memory users must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>.
/// </summary>
public class MemoryUserStoreTenantIsolationTests
{
    [Test]
    [DisplayName("FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new UserFilter())).ToList();

        await Assert.That(found.Count).IsEqualTo(2);
        await Assert.That(found).Contains(x => x.Id == "user-a");
        await Assert.That(found).Contains(x => x.Id == "user-star");
        await Assert.That(found).DoesNotContain(x => x.Id == "user-b");
    }

    [Test]
    [DisplayName("FindAsync does not return another tenant's user")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new UserFilter { Id = "user-b" });

        await Assert.That(found).IsNull();
    }

    [Test]
    [DisplayName("FindAsync on a named tenant hides null TenantId users")]
    public async Task FindAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var found = await store.FindAsync(new UserFilter { Id = "user-null" });
        var own = await store.FindAsync(new UserFilter { Id = "user-a" });

        await Assert.That(found).IsNull();
        await Assert.That(own).IsNotNull();
        await Assert.That(own.Id).IsEqualTo("user-a");
    }

    [Test]
    [DisplayName("FindAsync on the default tenant includes null TenantId users")]
    public async Task FindAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var found = await store.FindAsync(new UserFilter { Id = "user-null" });

        await Assert.That(found).IsNotNull();
        await Assert.That(found.Id).IsEqualTo("user-null");
        await Assert.That(found.TenantId).IsNull();
    }

    [Test]
    [DisplayName("DeleteAsync does not remove another tenant's users")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var backing = new MemoryStore<User>();
        var tenantA = new MemoryUserStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryUserStore(backing, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        await tenantA.DeleteAsync(new UserFilter { Id = "user-b" });
        var remaining = await tenantB.FindAsync(new UserFilter { Id = "user-b" });

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.Id).IsEqualTo("user-b");
    }

    [Test]
    [DisplayName("DeleteAsync leaves a same-ID row after another tenant replaces it")]
    public async Task DeleteAsync_WhenSameIdWasReplacedByOtherTenant_LeavesReplacement()
    {
        var backing = new MemoryStore<User>();
        var tenantA = new MemoryUserStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryUserStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(CreateUser("shared", "tenant-a"));
        await tenantB.SaveAsync(CreateUser("shared", "tenant-b"));

        await tenantA.DeleteAsync(new UserFilter { Id = "shared" });
        var remaining = await tenantB.FindAsync(new UserFilter { Id = "shared" });

        await Assert.That(remaining).IsNotNull();
        await Assert.That(remaining.TenantId).IsEqualTo("tenant-b");
    }

    [Test]
    [DisplayName("SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var user = CreateUser("user-new", tenantId: null);

        await store.SaveAsync(user);

        await Assert.That(user.TenantId).IsEqualTo("tenant-a");
        await Assert.That((await store.FindAsync(new UserFilter { Id = "user-new" }))!.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = CreateUser("user-star", Tenant.AgnosticTenantId);
        var explicitTenant = CreateUser("user-a", "tenant-a");

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        await Assert.That(agnostic.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
        await Assert.That(explicitTenant.TenantId).IsEqualTo("tenant-a");
    }

    private static MemoryUserStore CreateStore(string tenantId) =>
        new(new MemoryStore<User>(), new TestTenantAccessor(tenantId));

    private static MemoryUserStore StoreWithPreassignedRows(string ambientTenantId)
    {
        var backing = new MemoryStore<User>();
        backing.Save(CreateUser("user-null", tenantId: null), x => x.Id);
        backing.Save(CreateUser("user-a", "tenant-a"), x => x.Id);
        return new MemoryUserStore(backing, new TestTenantAccessor(ambientTenantId));
    }

    private static async Task SeedMixedTenantsAsync(MemoryUserStore store)
    {
        await store.SaveAsync(CreateUser("user-a", "tenant-a"));
        await store.SaveAsync(CreateUser("user-b", "tenant-b"));
        await store.SaveAsync(CreateUser("user-star", Tenant.AgnosticTenantId));
    }

    private static User CreateUser(string id, string? tenantId) =>
        new()
        {
            Id = id,
            Name = id,
            TenantId = tenantId
        };
}
