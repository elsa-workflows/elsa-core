namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class SqliteStoreSaveManyTenantOwnershipTests : StoreSaveManyTenantOwnershipTests
{
    protected override Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true) =>
        OwnershipStoreScenario.CreateSqliteAsync(tenantId, tenantsEnabled);

    [Fact]
    public async Task SaveManyAsync_WhenNocaseKeyDiffersOnlyInCasing_ThrowsAndLeavesOwner()
    {
        await using var scenario = await OwnershipStoreScenario.CreateSqliteAsync("tenant-a", tenantsEnabled: true, nocaseKey: true);
        await scenario.Store.SaveManyAsync([new OwnedRow { Id = "abc", TenantId = "tenant-a", Payload = "original" }], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([new OwnedRow { Id = "ABC", TenantId = "tenant-b", Payload = "stolen" }], x => x.Id, onSaving: null));
        }

        await AssertOriginalOwnerAsync(scenario);
    }

    [Fact]
    public async Task SaveManyAsync_WhenNocaseBatchPairsExactKeyWithForgedVariant_ThrowsAndLeavesOwner()
    {
        await using var scenario = await OwnershipStoreScenario.CreateSqliteAsync("tenant-a", tenantsEnabled: true, nocaseKey: true);
        await scenario.Store.SaveManyAsync([new OwnedRow { Id = "abc", TenantId = "tenant-a", Payload = "original" }], x => x.Id, onSaving: null);

        // The exact key passes its own check; the variant targets the same row under NOCASE with a forged tenant.
        await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Store.SaveManyAsync(
            [
                new OwnedRow { Id = "abc", TenantId = "tenant-a", Payload = "original" },
                new OwnedRow { Id = "ABC", TenantId = "tenant-b", Payload = "stolen" }
            ],
            x => x.Id,
            onSaving: null));

        await AssertOriginalOwnerAsync(scenario);
    }

    private static async Task AssertOriginalOwnerAsync(OwnershipStoreScenario scenario)
    {
        var remaining = await scenario.FindAsync("abc");
        Assert.NotNull(remaining);
        Assert.Equal("abc", remaining.Id);
        Assert.Equal("tenant-a", remaining.TenantId);
        Assert.Equal("original", remaining.Payload);
    }
}
