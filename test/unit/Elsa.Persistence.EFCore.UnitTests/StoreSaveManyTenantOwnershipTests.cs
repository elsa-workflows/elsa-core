using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;

namespace Elsa.Persistence.EFCore.UnitTests;

public abstract class StoreSaveManyTenantOwnershipTests
{
    protected abstract Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true);

    [Fact]
    public async Task SaveManyAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", "tenant-a", "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("shared", "tenant-b", "stolen")], x => x.Id, onSaving: null));
        }

        AssertUnchanged(await scenario.FindAsync("shared"), "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenMixedBatchContainsForeignId_WritesNothing()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("owned", "tenant-a", "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Store.SaveManyAsync(
            [
                Row("new-from-b", "tenant-b", "should-not-land"),
                Row("owned", "tenant-b", "stolen")
            ], x => x.Id, onSaving: null));
        }

        Assert.Null(await scenario.FindAsync("new-from-b"));
        AssertUnchanged(await scenario.FindAsync("owned"), "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenSameTenantOwnsId_UpdatesPayload()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("row-a", "tenant-a", "before")], x => x.Id, onSaving: null);

        await scenario.Store.SaveManyAsync([Row("row-a", "tenant-a", "after")], x => x.Id, onSaving: null);

        AssertUnchanged(await scenario.FindAsync("row-a"), "tenant-a", "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenPopulatorResavesStarUnderNamedTenant_KeepsStar()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "before")], x => x.Id, onSaving: null);

        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "after")], x => x.Id, onSaving: null);

        AssertUnchanged(await scenario.FindAsync("shared"), Tenant.AgnosticTenantId, "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenNamedTenantSavesNullTenantIdOverStar_ThrowsAndLeavesStar()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "original")], x => x.Id, onSaving: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.Store.SaveManyAsync([Row("shared", tenantId: null, payload: "stolen")], x => x.Id, onSaving: null));

        AssertUnchanged(await scenario.FindAsync("shared"), Tenant.AgnosticTenantId, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenDefaultTenantUpdatesLegacyNullRow_Succeeds()
    {
        await using var scenario = await CreateScenarioAsync(Tenant.DefaultTenantId);
        await scenario.Store.SaveManyAsync([Row("legacy", Tenant.DefaultTenantId, "before")], x => x.Id, onSaving: null);
        await scenario.ClearTenantIdAsync("legacy");

        await scenario.Store.SaveManyAsync([Row("legacy", Tenant.DefaultTenantId, "after")], x => x.Id, onSaving: null);

        var found = await scenario.FindAsync("legacy");
        Assert.NotNull(found);
        Assert.Equal("after", found.Payload);
        Assert.True(string.IsNullOrEmpty(found.TenantId));
    }

    [Fact]
    public async Task SaveManyAsync_WhenTenancyIsDisabled_OverwritesForeignId()
    {
        await using var scenario = await CreateScenarioAsync("tenant-b", tenantsEnabled: false);
        await scenario.Store.SaveManyAsync([Row("shared", "tenant-a", "original")], x => x.Id, onSaving: null);

        await scenario.Store.SaveManyAsync([Row("shared", "tenant-b", "taken")], x => x.Id, onSaving: null);

        AssertUnchanged(await scenario.FindAsync("shared"), "tenant-b", "taken");
    }

    [Fact]
    public async Task SaveManyAsync_WhenNamedTenantSavesOverNullRow_ThrowsAndLeavesNull()
    {
        await using var scenario = await CreateScenarioAsync(Tenant.DefaultTenantId);
        await scenario.Store.SaveManyAsync([Row("legacy", Tenant.DefaultTenantId, "original")], x => x.Id, onSaving: null);
        await scenario.ClearTenantIdAsync("legacy");

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("legacy", "tenant-b", "stolen")], x => x.Id, onSaving: null));
        }

        var remaining = await scenario.FindAsync("legacy");
        Assert.NotNull(remaining);
        Assert.Null(remaining.TenantId);
        Assert.Equal("original", remaining.Payload);
    }

    [Fact]
    public async Task SaveManyAsync_WhenNamedTenantSavesOverEmptyRow_ThrowsAndLeavesEmpty()
    {
        await using var scenario = await CreateScenarioAsync(Tenant.DefaultTenantId);
        await scenario.Store.SaveManyAsync([Row("owned", Tenant.DefaultTenantId, "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("owned", "tenant-b", "stolen")], x => x.Id, onSaving: null));
        }

        AssertUnchanged(await scenario.FindAsync("owned"), Tenant.DefaultTenantId, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenTenantNamedDefaultSavesOverEmpty_Throws()
    {
        await using var scenario = await CreateScenarioAsync(Tenant.DefaultTenantId);
        await scenario.Store.SaveManyAsync([Row("owned", Tenant.DefaultTenantId, "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("default"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("owned", "default", "stolen")], x => x.Id, onSaving: null));
        }

        AssertUnchanged(await scenario.FindAsync("owned"), Tenant.DefaultTenantId, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenEmptySavesOverTenantNamedDefault_Throws()
    {
        await using var scenario = await CreateScenarioAsync("default");
        await scenario.Store.SaveManyAsync([Row("owned", "default", "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("owned", Tenant.DefaultTenantId, "stolen")], x => x.Id, onSaving: null));
        }

        AssertUnchanged(await scenario.FindAsync("owned"), "default", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenWriterSavesExplicitForeignTenantId_ThrowsAndLeavesPayload()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", "tenant-a", "original")], x => x.Id, onSaving: null);

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync([Row("shared", "tenant-a", "stolen")], x => x.Id, onSaving: null));
        }

        AssertUnchanged(await scenario.FindAsync("shared"), "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenLaterChunkHasForeignId_WritesNothing()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("owned", "tenant-a", "original")], x => x.Id, onSaving: null);

        var batch = Enumerable.Range(0, 50)
            .Select(index => Row($"new-{index:000}", "tenant-b", "should-not-land"))
            .Append(Row("owned", "tenant-b", "stolen"))
            .ToList();

        using (scenario.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scenario.Store.SaveManyAsync(batch, x => x.Id, onSaving: null));
        }

        for (var index = 0; index < 50; index++)
            Assert.Null(await scenario.FindAsync($"new-{index:000}"));

        AssertUnchanged(await scenario.FindAsync("owned"), "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenDuplicateKeyHasEarlierViolation_ThrowsAndWritesNothing()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "original")], x => x.Id, onSaving: null);

        var batch = new[] { Row("shared", tenantId: null, payload: "stolen") }
            .Concat(Enumerable.Range(0, 60).Select(index => Row($"filler-{index:000}", "tenant-a", "filler")))
            .Append(Row("shared", Tenant.AgnosticTenantId, "after"))
            .ToList();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.Store.SaveManyAsync(batch, x => x.Id, onSaving: null));

        AssertUnchanged(await scenario.FindAsync("shared"), Tenant.AgnosticTenantId, "original");
        Assert.Null(await scenario.FindAsync("filler-000"));
    }

    [Fact]
    public async Task SaveManyAsync_WhenDuplicateKeyHasLaterViolation_ThrowsAndWritesNothing()
    {
        await using var scenario = await CreateScenarioAsync("tenant-a");
        await scenario.Store.SaveManyAsync([Row("shared", Tenant.AgnosticTenantId, "original")], x => x.Id, onSaving: null);

        var batch = new[] { Row("shared", Tenant.AgnosticTenantId, "after") }
            .Concat(Enumerable.Range(0, 60).Select(index => Row($"filler-{index:000}", "tenant-a", "filler")))
            .Append(Row("shared", tenantId: null, payload: "stolen"))
            .ToList();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.Store.SaveManyAsync(batch, x => x.Id, onSaving: null));

        AssertUnchanged(await scenario.FindAsync("shared"), Tenant.AgnosticTenantId, "original");
        Assert.Null(await scenario.FindAsync("filler-000"));
    }

    private static void AssertUnchanged(OwnedRow? row, string tenantId, string payload)
    {
        Assert.NotNull(row);
        Assert.Equal(tenantId, row.TenantId);
        Assert.Equal(payload, row.Payload);
    }

    private static OwnedRow Row(string id, string? tenantId, string payload) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Payload = payload
        };
}
