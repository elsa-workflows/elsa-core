using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Identity.UnitTests.Services;

/// <summary>
/// Memory must enforce the same per-tenant application uniqueness that EF Core
/// already enforces via <c>PerTenantIdentityUniqueness</c> on
/// <c>(TenantId, Name)</c> and <c>(TenantId, ClientId)</c>.
/// </summary>
public class MemoryApplicationStoreUniquenessTests
{
    [Test]
    [DisplayName("SaveAsync rejects a different Id that repeats a name in the same tenant")]
    public async Task SaveAsync_WhenNameExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateApplication("app-1", "Studio", "client-1", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateApplication("app-2", "Studio", "client-2", "tenant-a")));

        await Assert.That(exception!.Message).Contains("already exists");
        await Assert.That(exception.Message).Contains("name");
        var stored = await store.FindAsync(new ApplicationFilter { Id = "app-1" });
        await Assert.That(stored).IsNotNull();
        await Assert.That(await store.FindAsync(new ApplicationFilter { Id = "app-2" })).IsNull();
    }

    [Test]
    [DisplayName("SaveAsync rejects a different Id that repeats a client id in the same tenant")]
    public async Task SaveAsync_WhenClientIdExistsUnderAnotherId_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateApplication("app-1", "Studio", "client-1", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateApplication("app-2", "Designer", "client-1", "tenant-a")));

        await Assert.That(exception!.Message).Contains("already exists");
        await Assert.That(exception.Message).Contains("client id");
        var stored = await store.FindAsync(new ApplicationFilter { Id = "app-1" });
        await Assert.That(stored).IsNotNull();
        await Assert.That(await store.FindAsync(new ApplicationFilter { Id = "app-2" })).IsNull();
    }

    [Test]
    [DisplayName("SaveAsync allows the same Id to update its own name and client id")]
    public async Task SaveAsync_WhenSameIdUpdatesNameAndClientId_Succeeds()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateApplication("app-1", "Studio", "client-1", "tenant-a"));

        await store.SaveAsync(CreateApplication("app-1", "Designer", "client-2", "tenant-a"));

        var stored = await store.FindAsync(new ApplicationFilter { Id = "app-1" });
        await Assert.That(stored!.Name).IsEqualTo("Designer");
        await Assert.That(stored.ClientId).IsEqualTo("client-2");
    }

    [Test]
    [DisplayName("SaveAsync allows the same name and client id in different tenants")]
    public async Task SaveAsync_WhenNameAndClientIdRepeatInAnotherTenant_Succeeds()
    {
        var backing = new MemoryStore<Application>();
        var tenantA = new MemoryApplicationStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryApplicationStore(backing, new TestTenantAccessor("tenant-b"));

        await tenantA.SaveAsync(CreateApplication("app-a", "Studio", "client-1", "tenant-a"));
        await tenantB.SaveAsync(CreateApplication("app-b", "Studio", "client-1", "tenant-b"));

        await Assert.That((await tenantA.FindAsync(new ApplicationFilter { Id = "app-a" }))!.Name).IsEqualTo("Studio");
        await Assert.That((await tenantB.FindAsync(new ApplicationFilter { Id = "app-b" }))!.Name).IsEqualTo("Studio");
    }

    [Test]
    [DisplayName("SaveAsync rejects renaming onto a name another Id already owns")]
    public async Task SaveAsync_WhenRenamingOntoAnotherIdsName_Throws()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateApplication("app-1", "Studio", "client-1", "tenant-a"));
        await store.SaveAsync(CreateApplication("app-2", "Designer", "client-2", "tenant-a"));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateApplication("app-2", "Studio", "client-2", "tenant-a")));

        await Assert.That(exception!.Message).Contains("already exists");
        await Assert.That((await store.FindAsync(new ApplicationFilter { Id = "app-2" }))!.Name).IsEqualTo("Designer");
    }

    [Test]
    [DisplayName("SaveAsync treats a stamped ambient tenant as the uniqueness tenant")]
    public async Task SaveAsync_WhenTenantIdUnset_UsesStampedAmbientTenantForUniqueness()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(CreateApplication("app-1", "Studio", "client-1", tenantId: null));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            store.SaveAsync(CreateApplication("app-2", "Studio", "client-2", tenantId: null)));

        await Assert.That(exception!.Message).Contains("already exists");
        await Assert.That((await store.FindAsync(new ApplicationFilter { Id = "app-1" }))!.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    [DisplayName("SaveAsync allows the same name for * and a tenant-scoped application")]
    public async Task SaveAsync_WhenAgnosticAndTenantScopedShareName_Succeeds()
    {
        var store = CreateStore("tenant-a");

        await store.SaveAsync(CreateApplication("app-star", "Studio", "client-star", Tenant.AgnosticTenantId));
        await store.SaveAsync(CreateApplication("app-a", "Studio", "client-a", "tenant-a"));

        await Assert.That(await store.FindAsync(new ApplicationFilter { Id = "app-star" })).IsNotNull();
        await Assert.That(await store.FindAsync(new ApplicationFilter { Id = "app-a" })).IsNotNull();
    }

    private static MemoryApplicationStore CreateStore(string tenantId) =>
        new(new MemoryStore<Application>(), new TestTenantAccessor(tenantId));

    private static Application CreateApplication(string id, string name, string clientId, string? tenantId) =>
        new()
        {
            Id = id,
            Name = name,
            ClientId = clientId,
            HashedApiKey = "",
            HashedApiKeySalt = "",
            HashedClientSecret = "",
            HashedClientSecretSalt = "",
            TenantId = tenantId
        };
}
