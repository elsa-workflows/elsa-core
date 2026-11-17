using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Persistence.ConformanceTests;

/// <summary>
/// EF <c>Store.SaveManyAsync</c> must refuse a cross-tenant key collision before any row is written.
/// </summary>
[Collection(WorkflowStoreSqliteConformanceCollection.Name)]
public sealed class EFCoreSaveManyTenantOwnershipTests
{
    [Fact]
    public async Task SaveManyAsync_WhenOtherTenantOwnsId_ThrowsAndLeavesOwnerAndPayload()
    {
        await using var owner = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Bookmarks.SaveManyAsync([Bookmark("shared", "tenant-a", "original")], CancellationToken.None);

        using (owner.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                owner.Bookmarks.SaveManyAsync([Bookmark("shared", "tenant-b", "stolen")], CancellationToken.None).AsTask());
        }

        var remaining = await FindBookmarkAsync(owner, "shared");
        AssertUnchanged(remaining, "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenMixedBatchContainsForeignId_WritesNothing()
    {
        await using var owner = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await owner.Bookmarks.SaveManyAsync([Bookmark("owned", "tenant-a", "original")], CancellationToken.None);

        using (owner.UseTenant("tenant-b"))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => owner.Bookmarks.SaveManyAsync(
            [
                Bookmark("new-from-b", "tenant-b", "should-not-land"),
                Bookmark("owned", "tenant-b", "stolen")
            ], CancellationToken.None).AsTask());

            Assert.Null(await FindBookmarkAsync(owner, "new-from-b"));
        }

        var remaining = await FindBookmarkAsync(owner, "owned");
        AssertUnchanged(remaining, "tenant-a", "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenSameTenantOwnsId_UpdatesPayload()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Bookmarks.SaveManyAsync([Bookmark("bm-a", "tenant-a", "before")], CancellationToken.None);

        await scenario.Bookmarks.SaveManyAsync([Bookmark("bm-a", "tenant-a", "after")], CancellationToken.None);

        var found = await FindBookmarkAsync(scenario, "bm-a");
        AssertUnchanged(found, "tenant-a", "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenPopulatorResavesStarUnderNamedTenant_KeepsStar()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Bookmarks.SaveManyAsync([Bookmark("shared", Tenant.AgnosticTenantId, "before")], CancellationToken.None);

        await scenario.Bookmarks.SaveManyAsync([Bookmark("shared", Tenant.AgnosticTenantId, "after")], CancellationToken.None);

        var found = await FindBookmarkAsync(scenario, "shared");
        AssertUnchanged(found, Tenant.AgnosticTenantId, "after");
    }

    [Fact]
    public async Task SaveManyAsync_WhenNamedTenantSavesNullTenantIdOverStar_ThrowsAndLeavesStar()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Bookmarks.SaveManyAsync([Bookmark("shared", Tenant.AgnosticTenantId, "original")], CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scenario.Bookmarks.SaveManyAsync([Bookmark("shared", tenantId: null, hash: "stolen")], CancellationToken.None).AsTask());

        var remaining = await FindBookmarkAsync(scenario, "shared");
        AssertUnchanged(remaining, Tenant.AgnosticTenantId, "original");
    }

    [Fact]
    public async Task SaveManyAsync_WhenDefaultTenantUpdatesLegacyNullRow_Succeeds()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync(Tenant.DefaultTenantId);
        await scenario.Bookmarks.SaveManyAsync([Bookmark("legacy", Tenant.DefaultTenantId, "before")], CancellationToken.None);
        await scenario.ClearBookmarkTenantIdAsync("legacy");

        await scenario.Bookmarks.SaveManyAsync([Bookmark("legacy", Tenant.DefaultTenantId, "after")], CancellationToken.None);

        var found = await FindBookmarkAsync(scenario, "legacy");
        Assert.NotNull(found);
        Assert.Equal("after", found.Hash);
        Assert.True(string.IsNullOrEmpty(found.TenantId));
    }

    [Fact]
    public async Task SaveManyAsync_WhenTenancyIsDisabled_OverwritesForeignId()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync("tenant-b", tenantsEnabled: false);
        await scenario.Bookmarks.SaveManyAsync([Bookmark("shared", "tenant-a", "original")], CancellationToken.None);

        await scenario.Bookmarks.SaveManyAsync([Bookmark("shared", "tenant-b", "taken")], CancellationToken.None);

        var found = await FindBookmarkAsync(scenario, "shared");
        AssertUnchanged(found, "tenant-b", "taken");
    }

    private static async Task<StoredBookmark?> FindBookmarkAsync(WorkflowStoreScenario scenario, string id) =>
        await scenario.Bookmarks.FindAsync(new BookmarkFilter { BookmarkId = id, TenantAgnostic = true });

    private static void AssertUnchanged(StoredBookmark? bookmark, string tenantId, string hash)
    {
        Assert.NotNull(bookmark);
        Assert.Equal(tenantId, bookmark.TenantId);
        Assert.Equal(hash, bookmark.Hash);
    }

    private static StoredBookmark Bookmark(string id, string? tenantId, string hash) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Hash = hash,
            WorkflowInstanceId = "instance-1",
            Name = "Elsa.HttpEndpoint",
            CreatedAt = CreatedAt
        };

    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
}
