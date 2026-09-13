using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Persistence.ConformanceTests;

/// <summary>
/// Shared Memory / EF Core store-contract assertions for workflow management and runtime ports.
/// </summary>
public abstract class WorkflowStoreConformanceTests
{
    protected abstract Task<WorkflowStoreScenario> CreateScenarioAsync();

    [Fact]
    public async Task TriggerLogicalKeysAreUniqueAndReplaceAsyncSkipsExistingKeys()
    {
        await using var scenario = await CreateScenarioAsync();
        var existing = Trigger("existing-id", hash: "hash-1");

        await scenario.Triggers.SaveAsync(existing);
        await scenario.AssertUniquenessConflictAsync(() => scenario.Triggers.SaveAsync(Trigger("other-id", hash: "hash-1")).AsTask());

        var afterConflict = (await scenario.Triggers.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();
        Assert.Equal("existing-id", Assert.Single(afterConflict).Id);

        await scenario.Triggers.SaveAsync(Trigger("existing-id", hash: "hash-2", workflowDefinitionVersionId: "v2"));
        var updated = await scenario.Triggers.FindAsync(new TriggerFilter { Id = "existing-id" });
        Assert.Equal("hash-2", updated!.Hash);
        Assert.Equal("v2", updated.WorkflowDefinitionVersionId);

        await scenario.Triggers.ReplaceAsync([], [Trigger("batch-1"), Trigger("batch-2")]);
        var afterBatch = (await scenario.Triggers.FindManyAsync(new TriggerFilter { Hash = "hash-1", TenantAgnostic = true })).ToList();
        Assert.Equal("batch-1", Assert.Single(afterBatch).Id);

        await scenario.Triggers.ReplaceAsync([], [Trigger("skipped-id")]);
        var afterSkip = (await scenario.Triggers.FindManyAsync(new TriggerFilter { Hash = "hash-1", TenantAgnostic = true })).ToList();
        Assert.Equal("batch-1", Assert.Single(afterSkip).Id);

        await scenario.Triggers.ReplaceAsync(afterSkip, [Trigger("replacement-id", workflowDefinitionVersionId: "v3")]);
        var afterReplace = (await scenario.Triggers.FindManyAsync(new TriggerFilter { Hash = "hash-1", TenantAgnostic = true })).ToList();
        var replacement = Assert.Single(afterReplace);
        Assert.Equal("replacement-id", replacement.Id);
        Assert.Equal("v3", replacement.WorkflowDefinitionVersionId);

        var tenantA = Trigger("id-a", hash: "shared-hash");
        tenantA.TenantId = "tenant-a";
        var tenantB = Trigger("id-b", hash: "shared-hash");
        tenantB.TenantId = "tenant-b";
        await scenario.Triggers.ReplaceAsync([], [tenantA, tenantB]);
        var bothTenants = (await scenario.Triggers.FindManyAsync(new TriggerFilter { Hash = "shared-hash", TenantAgnostic = true })).ToList();
        Assert.Equal(2, bothTenants.Count);
        Assert.Contains(bothTenants, x => x.Id == "id-a");
        Assert.Contains(bothTenants, x => x.Id == "id-b");
    }

    [Fact]
    public async Task TriggerTenantIsolationHonorsAmbientTenantAndTenantAgnostic()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedTriggersAsync(scenario);

        var visible = (await scenario.Triggers.FindManyAsync(new TriggerFilter())).ToList();
        Assert.Equal(2, visible.Count);
        Assert.Contains(visible, x => x.Id == "id-a");
        Assert.Contains(visible, x => x.Id == "id-star");
        Assert.DoesNotContain(visible, x => x.Id == "id-b");

        var all = (await scenario.Triggers.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, x => x.Id == "id-b");

        Assert.Null(await scenario.Triggers.FindAsync(new TriggerFilter { Id = "id-b" }));

        var deleted = await scenario.Triggers.DeleteManyAsync(new TriggerFilter());
        var remaining = (await scenario.Triggers.FindManyAsync(new TriggerFilter { TenantAgnostic = true })).ToList();
        Assert.Equal(2, deleted);
        Assert.Equal("id-b", Assert.Single(remaining).Id);

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Triggers.SaveAsync(Trigger("id-null", hash: "hash-null", tenantId: null));
            await scenario.Triggers.SaveAsync(Trigger("id-named", hash: "hash-named", tenantId: "tenant-a"));

            var defaultVisible = (await scenario.Triggers.FindManyAsync(new TriggerFilter())).ToList();
            Assert.Contains(defaultVisible, x => x.Id == "id-null");
            Assert.DoesNotContain(defaultVisible, x => x.Id == "id-named");
        }

        using (scenario.UseTenant("tenant-a"))
        {
            var namedVisible = (await scenario.Triggers.FindManyAsync(new TriggerFilter { Hash = "hash-null" })).ToList();
            Assert.Empty(namedVisible);
        }
    }

    [Fact]
    public async Task DefinitionVersionsAreUniquePerDefinitionId()
    {
        await using var scenario = await CreateScenarioAsync();
        var first = Definition("def-v1", "order", "tenant-a");

        await scenario.Definitions.SaveAsync(first);
        await scenario.AssertUniquenessConflictAsync(() => scenario.Definitions.SaveAsync(Definition("def-v1-dup", "order", "tenant-a")));

        var afterConflict = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = "order",
            VersionOptions = VersionOptions.SpecificVersion(1),
            TenantAgnostic = true
        })).ToList();
        Assert.Equal("def-v1", Assert.Single(afterConflict).Id);

        first.Name = "Order Updated";
        await scenario.Definitions.SaveAsync(first);
        var updated = await scenario.Definitions.FindAsync(new WorkflowDefinitionFilter { Id = "def-v1" });
        Assert.Equal("Order Updated", updated!.Name);

        await scenario.Definitions.SaveAsync(Definition("def-v2", "order", "tenant-a", version: 2));
        var versions = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = "order", TenantAgnostic = true })).ToList();
        Assert.Equal(2, versions.Count);
        Assert.Contains(versions, x => x.Id == "def-v1" && x.Version == 1);
        Assert.Contains(versions, x => x.Id == "def-v2" && x.Version == 2);
    }

    [Fact]
    public async Task DefinitionTenantIsolationHonorsAmbientTenantAndTenantAgnostic()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedDefinitionsAsync(scenario);

        var visible = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter())).ToList();
        Assert.Equal(2, visible.Count);
        Assert.Contains(visible, x => x.Id == "def-a");
        Assert.Contains(visible, x => x.Id == "def-star");
        Assert.DoesNotContain(visible, x => x.Id == "def-b");

        var all = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, x => x.Id == "def-b");

        Assert.Null(await scenario.Definitions.FindAsync(new WorkflowDefinitionFilter { Id = "def-b" }));
        Assert.False(await scenario.Definitions.AnyAsync(new WorkflowDefinitionFilter { Id = "def-b" }));
        Assert.Equal(2, await scenario.Definitions.CountDistinctAsync());

        var deleted = await scenario.Definitions.DeleteAsync(new WorkflowDefinitionFilter());
        var remaining = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter { TenantAgnostic = true })).ToList();
        Assert.Equal(2, deleted);
        Assert.Equal("def-b", Assert.Single(remaining).Id);

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Definitions.SaveAsync(Definition("def-null", "Null", tenantId: null));
            await scenario.Definitions.SaveAsync(Definition("def-named", "Named", "tenant-a"));

            var defaultVisible = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter())).ToList();
            Assert.Contains(defaultVisible, x => x.Id == "def-null");
            Assert.DoesNotContain(defaultVisible, x => x.Id == "def-named");
        }
    }

    [Fact]
    public async Task BookmarkTenantIsolationHonorsAmbientTenantAndTenantAgnostic()
    {
        await using var scenario = await CreateScenarioAsync();
        await SeedMixedBookmarksAsync(scenario);

        var visible = (await scenario.Bookmarks.FindManyAsync(new BookmarkFilter())).ToList();
        Assert.Equal(2, visible.Count);
        Assert.Contains(visible, x => x.Id == "bm-a");
        Assert.Contains(visible, x => x.Id == "bm-star");
        Assert.DoesNotContain(visible, x => x.Id == "bm-b");

        var all = (await scenario.Bookmarks.FindManyAsync(new BookmarkFilter { TenantAgnostic = true })).ToList();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, x => x.Id == "bm-b");

        Assert.Null(await scenario.Bookmarks.FindAsync(new BookmarkFilter { BookmarkId = "bm-b" }));

        var deleted = await scenario.Bookmarks.DeleteAsync(new BookmarkFilter());
        var remaining = (await scenario.Bookmarks.FindManyAsync(new BookmarkFilter { TenantAgnostic = true })).ToList();
        Assert.Equal(2, deleted);
        Assert.Equal("bm-b", Assert.Single(remaining).Id);

        using (scenario.UseTenant(Tenant.DefaultTenantId))
        {
            await scenario.Bookmarks.SaveAsync(Bookmark("bm-null", tenantId: null));
            await scenario.Bookmarks.SaveAsync(Bookmark("bm-named", tenantId: "tenant-a"));

            var defaultVisible = (await scenario.Bookmarks.FindManyAsync(new BookmarkFilter())).ToList();
            Assert.Contains(defaultVisible, x => x.Id == "bm-null");
            Assert.DoesNotContain(defaultVisible, x => x.Id == "bm-named");
        }
    }

    [Fact]
    public async Task DeadLetterOriginalQueueItemIdIsUniqueAndAddOrGetIsIdempotent()
    {
        await using var scenario = await CreateScenarioAsync();
        var first = DeadLetter("dl-1", "queue-1");

        await scenario.DeadLetters.SaveAsync(first);
        await scenario.AssertUniquenessConflictAsync(() => scenario.DeadLetters.SaveAsync(DeadLetter("dl-2", "queue-1")));

        var afterConflict = (await scenario.DeadLetters.FindManyAsync(new BookmarkQueueDeadLetterFilter { OriginalQueueItemId = "queue-1" })).ToList();
        Assert.Equal("dl-1", Assert.Single(afterConflict).Id);

        var existing = await scenario.DeadLetters.AddOrGetExistingAsync(DeadLetter("dl-3", "queue-1"));
        Assert.Equal("dl-1", existing.Id);

        var created = await scenario.DeadLetters.AddOrGetExistingAsync(DeadLetter("dl-4", "queue-2"));
        Assert.Equal("dl-4", created.Id);

        var all = (await scenario.DeadLetters.FindManyAsync(new BookmarkQueueDeadLetterFilter())).ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.Id == "dl-1");
        Assert.Contains(all, x => x.Id == "dl-4");
    }

    private static async Task SeedMixedTriggersAsync(WorkflowStoreScenario scenario)
    {
        await scenario.Triggers.SaveAsync(Trigger("id-a", hash: "hash-a", tenantId: "tenant-a"));
        await scenario.Triggers.SaveAsync(Trigger("id-b", hash: "hash-b", tenantId: "tenant-b"));
        await scenario.Triggers.SaveAsync(Trigger("id-star", hash: "hash-star", tenantId: Tenant.AgnosticTenantId));
    }

    private static async Task SeedMixedDefinitionsAsync(WorkflowStoreScenario scenario)
    {
        await scenario.Definitions.SaveAsync(Definition("def-a", "A", "tenant-a"));
        await scenario.Definitions.SaveAsync(Definition("def-b", "B", "tenant-b"));
        await scenario.Definitions.SaveAsync(Definition("def-star", "Star", Tenant.AgnosticTenantId));
    }

    private static async Task SeedMixedBookmarksAsync(WorkflowStoreScenario scenario)
    {
        await scenario.Bookmarks.SaveAsync(Bookmark("bm-a", tenantId: "tenant-a"));
        await scenario.Bookmarks.SaveAsync(Bookmark("bm-b", tenantId: "tenant-b"));
        await scenario.Bookmarks.SaveAsync(Bookmark("bm-star", tenantId: Tenant.AgnosticTenantId));
    }

    private static StoredTrigger Trigger(
        string id,
        string workflowDefinitionId = "workflow-1",
        string workflowDefinitionVersionId = "v1",
        string activityId = "activity-1",
        string? hash = "hash-1",
        string? tenantId = "tenant-a") =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            WorkflowDefinitionId = workflowDefinitionId,
            WorkflowDefinitionVersionId = workflowDefinitionVersionId,
            ActivityId = activityId,
            Hash = hash,
            Name = "Elsa.HttpEndpoint"
        };

    private static WorkflowDefinition Definition(string id, string name, string? tenantId, string? definitionId = null, int version = 1) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId ?? name.ToLowerInvariant(),
            Name = name,
            TenantId = tenantId,
            Version = version,
            IsLatest = version == 1,
            MaterializerName = "Json",
            CreatedAt = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero)
        };

    private static StoredBookmark Bookmark(string id, string? tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Hash = id,
            WorkflowInstanceId = "instance-1",
            Name = "Elsa.HttpEndpoint"
        };

    private static BookmarkQueueDeadLetterItem DeadLetter(string id, string originalQueueItemId) =>
        new()
        {
            Id = id,
            TenantId = "tenant-a",
            OriginalQueueItemId = originalQueueItemId,
            WorkflowInstanceId = "instance-1",
            Reason = "delivery-failed",
            OriginalCreatedAt = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero),
            DeadLetteredAt = new DateTimeOffset(2026, 9, 13, 12, 5, 0, TimeSpan.Zero)
        };
}

[CollectionDefinition(Name)]
public sealed class WorkflowStoreInMemoryConformanceCollection
{
    public const string Name = "WorkflowStores:InMemory";
}

[CollectionDefinition(Name)]
public sealed class WorkflowStoreSqliteConformanceCollection
{
    public const string Name = "WorkflowStores:EFCore.Sqlite";
}

[Collection(WorkflowStoreInMemoryConformanceCollection.Name)]
public sealed class InMemoryWorkflowStoreConformanceTests : WorkflowStoreConformanceTests
{
    protected override Task<WorkflowStoreScenario> CreateScenarioAsync() => WorkflowStoreScenario.CreateInMemoryAsync();
}

[Collection(WorkflowStoreSqliteConformanceCollection.Name)]
public sealed class SqliteWorkflowStoreConformanceTests : WorkflowStoreConformanceTests
{
    protected override Task<WorkflowStoreScenario> CreateScenarioAsync() => WorkflowStoreScenario.CreateSqliteAsync();
}
