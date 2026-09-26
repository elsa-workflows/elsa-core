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

        var remaining = await scenario.FindAsync("abc");
        Assert.NotNull(remaining);
        Assert.Equal("abc", remaining.Id);
        Assert.Equal("tenant-a", remaining.TenantId);
        Assert.Equal("original", remaining.Payload);
    }
}
