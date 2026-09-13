using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// Memory applications must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>.
/// </summary>
public class MemoryApplicationStoreTenantIsolationTests
{
    [Fact(DisplayName = "FindAsync hides other tenants and keeps * visible")]
    public async Task FindAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var own = await store.FindAsync(new ApplicationFilter { Id = "app-a" });
        var star = await store.FindAsync(new ApplicationFilter { Id = "app-star" });
        var other = await store.FindAsync(new ApplicationFilter { Id = "app-b" });

        Assert.NotNull(own);
        Assert.Equal("app-a", own.Id);
        Assert.NotNull(star);
        Assert.Equal("app-star", star.Id);
        Assert.Null(other);
    }

    [Fact(DisplayName = "FindAsync on a named tenant hides null TenantId applications")]
    public async Task FindAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var found = await store.FindAsync(new ApplicationFilter { Id = "app-null" });
        var own = await store.FindAsync(new ApplicationFilter { Id = "app-a" });

        Assert.Null(found);
        Assert.NotNull(own);
        Assert.Equal("app-a", own.Id);
    }

    [Fact(DisplayName = "FindAsync on the default tenant includes null TenantId applications")]
    public async Task FindAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var found = await store.FindAsync(new ApplicationFilter { Id = "app-null" });

        Assert.NotNull(found);
        Assert.Equal("app-null", found.Id);
        Assert.Null(found.TenantId);
    }

    [Fact(DisplayName = "DeleteAsync does not remove another tenant's applications")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantRows()
    {
        var backing = new MemoryStore<Application>();
        var tenantA = new MemoryApplicationStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryApplicationStore(backing, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        await tenantA.DeleteAsync(new ApplicationFilter { Id = "app-b" });
        var remaining = await tenantB.FindAsync(new ApplicationFilter { Id = "app-b" });

        Assert.NotNull(remaining);
        Assert.Equal("app-b", remaining.Id);
    }

    [Fact(DisplayName = "DeleteAsync leaves a same-ID row after another tenant replaces it")]
    public async Task DeleteAsync_WhenSameIdWasReplacedByOtherTenant_LeavesReplacement()
    {
        var backing = new MemoryStore<Application>();
        var tenantA = new MemoryApplicationStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryApplicationStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(CreateApplication("shared", "tenant-a"));
        await tenantB.SaveAsync(CreateApplication("shared", "tenant-b"));

        await tenantA.DeleteAsync(new ApplicationFilter { Id = "shared" });
        var remaining = await tenantB.FindAsync(new ApplicationFilter { Id = "shared" });

        Assert.NotNull(remaining);
        Assert.Equal("tenant-b", remaining.TenantId);
    }

    [Fact(DisplayName = "SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var application = CreateApplication("app-new", tenantId: null);

        await store.SaveAsync(application);

        Assert.Equal("tenant-a", application.TenantId);
        Assert.Equal("tenant-a", (await store.FindAsync(new ApplicationFilter { Id = "app-new" }))!.TenantId);
    }

    [Fact(DisplayName = "SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = CreateApplication("app-star", Tenant.AgnosticTenantId);
        var explicitTenant = CreateApplication("app-a", "tenant-a");

        await store.SaveAsync(agnostic);
        await store.SaveAsync(explicitTenant);

        Assert.Equal(Tenant.AgnosticTenantId, agnostic.TenantId);
        Assert.Equal("tenant-a", explicitTenant.TenantId);
    }

    private static MemoryApplicationStore CreateStore(string tenantId) =>
        new(new MemoryStore<Application>(), new TestTenantAccessor(tenantId));

    private static MemoryApplicationStore StoreWithPreassignedRows(string ambientTenantId)
    {
        var backing = new MemoryStore<Application>();
        backing.Save(CreateApplication("app-null", tenantId: null), x => x.Id);
        backing.Save(CreateApplication("app-a", "tenant-a"), x => x.Id);
        return new MemoryApplicationStore(backing, new TestTenantAccessor(ambientTenantId));
    }

    private static async Task SeedMixedTenantsAsync(MemoryApplicationStore store)
    {
        await store.SaveAsync(CreateApplication("app-a", "tenant-a"));
        await store.SaveAsync(CreateApplication("app-b", "tenant-b"));
        await store.SaveAsync(CreateApplication("app-star", Tenant.AgnosticTenantId));
    }

    private static Application CreateApplication(string id, string? tenantId) =>
        new()
        {
            Id = id,
            ClientId = id,
            Name = id,
            HashedApiKey = "",
            HashedApiKeySalt = "",
            HashedClientSecret = "",
            HashedClientSecretSalt = "",
            TenantId = tenantId
        };
}
