using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.KeyValues.Stores;
using Elsa.Testing.Shared.Multitenancy;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Memory KeyValue must honor ambient tenant the same way EF does via
/// <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>. Contracts have no TenantAgnostic flag.
/// </summary>
public class MemoryKeyValueStoreTenantIsolationTests
{
    [Fact(DisplayName = "FindAsync does not return another tenant's row")]
    public async Task FindAsync_WhenOtherTenant_ReturnsNull()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = await store.FindAsync(new KeyValueFilter { Key = "kv-b" }, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact(DisplayName = "FindAsync returns the ambient tenant's row and keeps * visible")]
    public async Task FindAsync_ReturnsOwnAndAgnosticRows()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var own = await store.FindAsync(new KeyValueFilter { Key = "kv-a" }, CancellationToken.None);
        var agnostic = await store.FindAsync(new KeyValueFilter { Key = "kv-star" }, CancellationToken.None);

        Assert.NotNull(own);
        Assert.Equal("a", own.SerializedValue);
        Assert.NotNull(agnostic);
        Assert.Equal("star", agnostic.SerializedValue);
    }

    [Fact(DisplayName = "FindManyAsync hides other tenants and keeps * visible")]
    public async Task FindManyAsync_HidesOtherTenants()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        var found = (await store.FindManyAsync(new KeyValueFilter { StartsWith = true, Key = "kv-" }, CancellationToken.None)).ToList();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, x => x.Key == "kv-a");
        Assert.Contains(found, x => x.Key == "kv-star");
        Assert.DoesNotContain(found, x => x.Key == "kv-b");
    }

    [Fact(DisplayName = "FindManyAsync applies Take after tenant admission")]
    public async Task FindManyAsync_AppliesTakeAfterTenantFilter()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);
        await store.SaveAsync(Pair("kv-a2", "a2", "tenant-a"), CancellationToken.None);

        var found = (await store.FindManyAsync(new KeyValueFilter
        {
            StartsWith = true,
            Key = "kv-",
            OrderByKey = true,
            Take = 3
        }, CancellationToken.None)).ToList();

        Assert.Equal(["kv-a", "kv-a2", "kv-star"], found.Select(x => x.Key).ToList());
        Assert.DoesNotContain(found, x => x.Key == "kv-b");
    }

    [Fact(DisplayName = "DeleteAsync does not remove another tenant's key")]
    public async Task DeleteAsync_DoesNotDeleteOtherTenantKey()
    {
        var backing = new MemoryStore<SerializedKeyValuePair>();
        var tenantA = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-b"));
        await SeedMixedTenantsAsync(tenantA);

        await tenantA.DeleteAsync("kv-b", CancellationToken.None);
        var remaining = await tenantB.FindAsync(new KeyValueFilter { Key = "kv-b" }, CancellationToken.None);

        Assert.NotNull(remaining);
        Assert.Equal("b", remaining.SerializedValue);
    }

    [Fact(DisplayName = "DeleteAsync removes the ambient tenant's own key")]
    public async Task DeleteAsync_RemovesOwnKey()
    {
        var store = CreateStore("tenant-a");
        await SeedMixedTenantsAsync(store);

        await store.DeleteAsync("kv-a", CancellationToken.None);
        var remaining = await store.FindAsync(new KeyValueFilter { Key = "kv-a" }, CancellationToken.None);

        Assert.Null(remaining);
    }

    [Fact(DisplayName = "DeleteAsync leaves a same-key row after another tenant replaced it")]
    public async Task DeleteAsync_WhenSameKeyWasReplacedByOtherTenant_LeavesReplacement()
    {
        var backing = new MemoryStore<SerializedKeyValuePair>();
        var tenantA = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-b"));
        backing.Save(Pair("shared", "a", "tenant-a"), x => x.Id);
        backing.Save(Pair("shared", "b", "tenant-b"), x => x.Id);

        await tenantA.DeleteAsync("shared", CancellationToken.None);
        var remaining = await tenantB.FindAsync(new KeyValueFilter { Key = "shared" }, CancellationToken.None);

        Assert.NotNull(remaining);
        Assert.Equal("tenant-b", remaining.TenantId);
        Assert.Equal("b", remaining.SerializedValue);
    }

    [Fact(DisplayName = "SaveAsync stamps the ambient tenant when TenantId is unset")]
    public async Task SaveAsync_WhenTenantIdUnset_StampsAmbientTenant()
    {
        var store = CreateStore("tenant-a");
        var pair = Pair("kv-new", "new");

        await store.SaveAsync(pair, CancellationToken.None);
        var found = await store.FindAsync(new KeyValueFilter { Key = "kv-new" }, CancellationToken.None);

        Assert.Equal("tenant-a", pair.TenantId);
        Assert.NotNull(found);
        Assert.Equal("tenant-a", found.TenantId);
    }

    [Fact(DisplayName = "SaveAsync does not overwrite * or an explicit TenantId")]
    public async Task SaveAsync_DoesNotOverwriteAgnosticOrExplicitTenantId()
    {
        var store = CreateStore("tenant-a");
        var agnostic = Pair("kv-star", "star", Tenant.AgnosticTenantId);
        var explicitTenant = Pair("kv-a", "a", "tenant-a");

        await store.SaveAsync(agnostic, CancellationToken.None);
        await store.SaveAsync(explicitTenant, CancellationToken.None);

        Assert.Equal(Tenant.AgnosticTenantId, agnostic.TenantId);
        Assert.Equal("tenant-a", explicitTenant.TenantId);
    }

    [Fact(DisplayName = "FindManyAsync on the default tenant includes null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var store = StoreWithPreassignedRows(Tenant.DefaultTenantId);

        var found = (await store.FindManyAsync(new KeyValueFilter { StartsWith = true, Key = "kv-" }, CancellationToken.None)).ToList();

        Assert.Single(found);
        Assert.Equal("kv-null", found[0].Key);
    }

    [Fact(DisplayName = "FindManyAsync on a named tenant hides null TenantId rows")]
    public async Task FindManyAsync_WhenAmbientIsNamed_HidesNullTenantId()
    {
        var store = StoreWithPreassignedRows("tenant-a");

        var found = (await store.FindManyAsync(new KeyValueFilter { StartsWith = true, Key = "kv-" }, CancellationToken.None)).ToList();

        Assert.Single(found);
        Assert.Equal("kv-a", found[0].Key);
    }

    [Fact(DisplayName = "SaveAsync throws when another tenant already owns the key and leaves that value intact")]
    public async Task SaveAsync_WhenOtherTenantOwnsKey_ThrowsAndLeavesOriginal()
    {
        var backing = new MemoryStore<SerializedKeyValuePair>();
        var tenantA = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-b"));
        await tenantA.SaveAsync(Pair("shared", "a", "tenant-a"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenantB.SaveAsync(Pair("shared", "b", "tenant-b"), CancellationToken.None));

        Assert.Contains("shared", ex.Message);
        var remaining = await tenantA.FindAsync(new KeyValueFilter { Key = "shared" }, CancellationToken.None);
        Assert.NotNull(remaining);
        Assert.Equal("a", remaining.SerializedValue);
        Assert.Equal("tenant-a", remaining.TenantId);
    }

    [Fact(DisplayName = "SaveAsync lets only an agnostic writer replace a * key")]
    public async Task SaveAsync_WhenAgnosticKey_NamedTenantThrowsAndAgnosticWriterReplaces()
    {
        var backing = new MemoryStore<SerializedKeyValuePair>();
        var named = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-a"));
        var agnostic = new MemoryKeyValueStore(backing, new TestTenantAccessor(Tenant.AgnosticTenantId));
        await agnostic.SaveAsync(Pair("kv-star", "star", Tenant.AgnosticTenantId), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            named.SaveAsync(Pair("kv-star", "stolen"), CancellationToken.None));

        var stillStar = await named.FindAsync(new KeyValueFilter { Key = "kv-star" }, CancellationToken.None);
        Assert.NotNull(stillStar);
        Assert.Equal("star", stillStar.SerializedValue);
        Assert.Equal(Tenant.AgnosticTenantId, stillStar.TenantId);

        await agnostic.SaveAsync(Pair("kv-star", "star-2", Tenant.AgnosticTenantId), CancellationToken.None);
        var replaced = await named.FindAsync(new KeyValueFilter { Key = "kv-star" }, CancellationToken.None);
        Assert.NotNull(replaced);
        Assert.Equal("star-2", replaced.SerializedValue);
        Assert.Equal(Tenant.AgnosticTenantId, replaced.TenantId);
    }

    [Fact(DisplayName = "SaveAsync same-tenant update keeps the existing TenantId")]
    public async Task SaveAsync_WhenSameTenant_KeepsExistingTenantId()
    {
        var store = CreateStore("tenant-a");
        await store.SaveAsync(Pair("kv-a", "a", "tenant-a"), CancellationToken.None);

        await store.SaveAsync(Pair("kv-a", "a2"), CancellationToken.None);
        var found = await store.FindAsync(new KeyValueFilter { Key = "kv-a" }, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("a2", found.SerializedValue);
        Assert.Equal("tenant-a", found.TenantId);
    }

    [Fact(DisplayName = "DeleteAsync removes a * key that is visible to the ambient tenant")]
    public async Task DeleteAsync_WhenAgnosticKey_NamedTenantRemovesIt()
    {
        var backing = new MemoryStore<SerializedKeyValuePair>();
        var tenantB = new MemoryKeyValueStore(backing, new TestTenantAccessor("tenant-b"));
        backing.Save(Pair("kv-star", "star", Tenant.AgnosticTenantId), x => x.Id);

        await tenantB.DeleteAsync("kv-star", CancellationToken.None);
        var remaining = await tenantB.FindAsync(new KeyValueFilter { Key = "kv-star" }, CancellationToken.None);

        Assert.Null(remaining);
    }

    private static MemoryKeyValueStore CreateStore(string tenantId) =>
        new(new MemoryStore<SerializedKeyValuePair>(), new TestTenantAccessor(tenantId));

    private static MemoryKeyValueStore StoreWithPreassignedRows(string ambientTenantId)
    {
        var backing = new MemoryStore<SerializedKeyValuePair>();
        backing.Save(Pair("kv-null", "null"), x => x.Id);
        backing.Save(Pair("kv-a", "a", "tenant-a"), x => x.Id);
        return new MemoryKeyValueStore(backing, new TestTenantAccessor(ambientTenantId));
    }

    private static async Task SeedMixedTenantsAsync(MemoryKeyValueStore store)
    {
        await store.SaveAsync(Pair("kv-a", "a", "tenant-a"), CancellationToken.None);
        await store.SaveAsync(Pair("kv-b", "b", "tenant-b"), CancellationToken.None);
        await store.SaveAsync(Pair("kv-star", "star", Tenant.AgnosticTenantId), CancellationToken.None);
    }

    private static SerializedKeyValuePair Pair(string key, string value, string? tenantId = null) =>
        new()
        {
            Key = key,
            SerializedValue = value,
            TenantId = tenantId
        };
}
