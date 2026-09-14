using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

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
    public async Task DefinitionSaveManyRejectsDuplicateVersionKeysInTheBatch()
    {
        await using var scenario = await CreateScenarioAsync();
        await scenario.Definitions.SaveAsync(Definition("def-kept", "kept", "tenant-a"));

        await scenario.AssertUniquenessConflictAsync(() => scenario.Definitions.SaveManyAsync(
        [
            Definition("def-batch-1", "invoice", "tenant-a"),
            Definition("def-batch-2", "invoice", "tenant-a")
        ]));

        var invoices = (await scenario.Definitions.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = "invoice",
            TenantAgnostic = true
        })).ToList();
        Assert.Empty(invoices);
        Assert.NotNull(await scenario.Definitions.FindAsync(new WorkflowDefinitionFilter { Id = "def-kept", TenantAgnostic = true }));
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

    [Fact]
    public async Task BookmarkActivityExecutionAndExecutionLogFindsHonorFiltersAndIdUpsert()
    {
        await using var scenario = await CreateScenarioAsync();

        await scenario.Bookmarks.SaveAsync(Bookmark("bm-1", hash: "hash-http", workflowInstanceId: "instance-1"));
        await scenario.Bookmarks.SaveAsync(Bookmark("bm-2", hash: "hash-timer", workflowInstanceId: "instance-1"));
        await scenario.Bookmarks.SaveAsync(Bookmark("bm-3", hash: "hash-http", workflowInstanceId: "instance-2"));

        Assert.Equal("bm-1", (await scenario.Bookmarks.FindAsync(new BookmarkFilter { BookmarkId = "bm-1" }))!.Id);
        Assert.Equal("bm-2", (await scenario.Bookmarks.FindAsync(new BookmarkFilter { Hash = "hash-timer" }))!.Id);
        Assert.Null(await scenario.Bookmarks.FindAsync(new BookmarkFilter { BookmarkId = "missing" }));

        var instanceBookmarks = (await scenario.Bookmarks.FindManyAsync(new BookmarkFilter { WorkflowInstanceId = "instance-1" })).ToList();
        Assert.Equal(2, instanceBookmarks.Count);
        Assert.Contains(instanceBookmarks, x => x.Id == "bm-1");
        Assert.Contains(instanceBookmarks, x => x.Id == "bm-2");

        var hashed = (await scenario.Bookmarks.FindManyAsync(new BookmarkFilter { Hash = "hash-http" })).ToList();
        Assert.Equal(2, hashed.Count);

        var firstPage = await scenario.Bookmarks.FindManyAsync(new BookmarkFilter { WorkflowInstanceId = "instance-1" }, PageArgs.FromRange(0, 1));
        Assert.Equal(2, firstPage.TotalCount);
        Assert.Single(firstPage.Items);

        await scenario.Bookmarks.SaveAsync(Bookmark("bm-1", hash: "hash-updated", workflowInstanceId: "instance-1"));
        Assert.Equal("hash-updated", (await scenario.Bookmarks.FindAsync(new BookmarkFilter { BookmarkId = "bm-1" }))!.Hash);

        await scenario.ActivityExecutions.SaveAsync(ActivityExecution("ae-1", "instance-1", "activity-a", ActivityStatus.Running));
        await scenario.ActivityExecutions.SaveAsync(ActivityExecution("ae-2", "instance-1", "activity-b", ActivityStatus.Completed, completedAt: StartedAt.AddMinutes(1)));
        await scenario.ActivityExecutions.SaveAsync(ActivityExecution("ae-3", "instance-2", "activity-a", ActivityStatus.Running));

        Assert.Equal("ae-1", (await scenario.ActivityExecutions.FindAsync(new ActivityExecutionRecordFilter { Id = "ae-1" }))!.Id);
        Assert.Null(await scenario.ActivityExecutions.FindAsync(new ActivityExecutionRecordFilter { Id = "missing" }));

        var instanceActivities = (await scenario.ActivityExecutions.FindManyAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = "instance-1" })).ToList();
        Assert.Equal(2, instanceActivities.Count);
        Assert.Equal(2, await scenario.ActivityExecutions.CountAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = "instance-1" }));
        Assert.Equal("ae-1", Assert.Single(await scenario.ActivityExecutions.FindManyAsync(new ActivityExecutionRecordFilter { ActivityId = "activity-a", WorkflowInstanceId = "instance-1" })).Id);
        Assert.Equal("ae-2", Assert.Single(await scenario.ActivityExecutions.FindManyAsync(new ActivityExecutionRecordFilter { Status = ActivityStatus.Completed })).Id);

        await scenario.ActivityExecutions.SaveAsync(ActivityExecution("ae-1", "instance-1", "activity-a", ActivityStatus.Completed, completedAt: StartedAt.AddMinutes(2)));
        Assert.Equal(ActivityStatus.Completed, (await scenario.ActivityExecutions.FindAsync(new ActivityExecutionRecordFilter { Id = "ae-1" }))!.Status);

        Assert.Equal(2, await scenario.ActivityExecutions.DeleteManyAsync(new ActivityExecutionRecordFilter { WorkflowInstanceId = "instance-1" }));
        Assert.Equal("ae-3", Assert.Single(await scenario.ActivityExecutions.FindManyAsync(new ActivityExecutionRecordFilter())).Id);

        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-1", "instance-1", "activity-a", "Started"));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-2", "instance-1", "activity-b", "Completed", activityType: "Elsa.Delay"));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-3", "instance-2", "activity-a", "Started"));

        Assert.Equal("el-1", (await scenario.ExecutionLogs.FindAsync(new WorkflowExecutionLogRecordFilter { Id = "el-1" }))!.Id);
        Assert.Null(await scenario.ExecutionLogs.FindAsync(new WorkflowExecutionLogRecordFilter { Id = "missing" }));

        var instanceLogs = await scenario.ExecutionLogs.FindManyAsync(new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = "instance-1" }, PageArgs.FromRange(0, 10));
        Assert.Equal(2, instanceLogs.TotalCount);
        Assert.Equal(2, instanceLogs.Items.Count);

        var firstLogPage = await scenario.ExecutionLogs.FindManyAsync(new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = "instance-1" }, PageArgs.FromRange(0, 1));
        Assert.Equal(2, firstLogPage.TotalCount);
        Assert.Single(firstLogPage.Items);

        Assert.Equal("el-2", (await scenario.ExecutionLogs.FindAsync(new WorkflowExecutionLogRecordFilter { EventName = "Completed" }))!.Id);
        Assert.Equal("el-2", Assert.Single((await scenario.ExecutionLogs.FindManyAsync(new WorkflowExecutionLogRecordFilter { ActivityId = "activity-b" }, PageArgs.All)).Items).Id);

        var excluded = await scenario.ExecutionLogs.FindManyAsync(new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = "instance-1", ExcludeActivityType = "Elsa.WriteLine" }, PageArgs.All);
        Assert.Equal("el-2", Assert.Single(excluded.Items).Id);

        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-1", "instance-1", "activity-a", "Resumed"));
        Assert.Equal("Resumed", (await scenario.ExecutionLogs.FindAsync(new WorkflowExecutionLogRecordFilter { Id = "el-1" }))!.EventName);

        Assert.Equal(2, await scenario.ExecutionLogs.DeleteManyAsync(new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = "instance-1" }));
        Assert.Equal("el-3", Assert.Single((await scenario.ExecutionLogs.FindManyAsync(new WorkflowExecutionLogRecordFilter(), PageArgs.All)).Items).Id);
    }

    [Fact]
    public async Task ExecutionLogDefaultOrderIsTimestampThenSequenceAndLastEntryUsesSequence()
    {
        await using var scenario = await CreateScenarioAsync();
        var sameTimestamp = StartedAt;
        var laterTimestamp = StartedAt.AddMinutes(1);

        // Insert later Sequence first so dictionary/heap order cannot accidentally match the contract.
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-seq-2", "instance-1", "activity-a", "Completed", timestamp: sameTimestamp, sequence: 2));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-seq-3", "instance-1", "activity-a", "Faulted", timestamp: sameTimestamp, sequence: 3));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-seq-1", "instance-1", "activity-a", "Started", timestamp: sameTimestamp, sequence: 1));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-later", "instance-1", "activity-b", "Started", timestamp: laterTimestamp, sequence: 0));

        var filter = new WorkflowExecutionLogRecordFilter { WorkflowInstanceId = "instance-1" };
        var page = await scenario.ExecutionLogs.FindManyAsync(filter, PageArgs.All);
        Assert.Equal(["el-seq-1", "el-seq-2", "el-seq-3", "el-later"], page.Items.Select(x => x.Id).ToArray());

        var firstPage = await scenario.ExecutionLogs.FindManyAsync(filter, PageArgs.FromRange(0, 2));
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(["el-seq-1", "el-seq-2"], firstPage.Items.Select(x => x.Id).ToArray());

        var first = await scenario.ExecutionLogs.FindAsync(filter);
        Assert.Equal("el-seq-1", first!.Id);

        var sameTimestampFilter = new WorkflowExecutionLogRecordFilter
        {
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-a",
            EventNames = ["Started", "Completed", "Faulted"]
        };
        var lastEntryOrder = new WorkflowExecutionLogRecordOrder<long>(x => x.Sequence, OrderDirection.Descending);
        var last = await scenario.ExecutionLogs.FindAsync(sameTimestampFilter, lastEntryOrder);
        Assert.Equal("el-seq-3", last!.Id);
    }

    [Fact]
    public async Task ExecutionLogDefaultOrderUsesIdWhenTimestampAndSequenceTieAcrossInstances()
    {
        await using var scenario = await CreateScenarioAsync();
        var timestamp = StartedAt;
        const long sequence = 1;

        // Reverse Id insert order so dictionary/heap order cannot accidentally match Id ascending.
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-d", "instance-4", "activity-a", "Started", timestamp: timestamp, sequence: sequence));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-b", "instance-2", "activity-a", "Started", timestamp: timestamp, sequence: sequence));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-c", "instance-3", "activity-a", "Started", timestamp: timestamp, sequence: sequence));
        await scenario.ExecutionLogs.SaveAsync(ExecutionLog("el-a", "instance-1", "activity-a", "Started", timestamp: timestamp, sequence: sequence));

        var filter = new WorkflowExecutionLogRecordFilter();
        var all = await scenario.ExecutionLogs.FindManyAsync(filter, PageArgs.All);
        Assert.Equal(["el-a", "el-b", "el-c", "el-d"], all.Items.Select(x => x.Id).ToArray());

        var firstPage = await scenario.ExecutionLogs.FindManyAsync(filter, PageArgs.FromRange(0, 2));
        var secondPage = await scenario.ExecutionLogs.FindManyAsync(filter, PageArgs.FromRange(2, 2));
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(["el-a", "el-b"], firstPage.Items.Select(x => x.Id).ToArray());
        Assert.Equal(["el-c", "el-d"], secondPage.Items.Select(x => x.Id).ToArray());
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

    private static readonly DateTimeOffset StartedAt = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    private static StoredBookmark Bookmark(
        string id,
        string? tenantId = "tenant-a",
        string? hash = null,
        string workflowInstanceId = "instance-1") =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Hash = hash ?? id,
            WorkflowInstanceId = workflowInstanceId,
            Name = "Elsa.HttpEndpoint",
            CreatedAt = StartedAt
        };

    private static ActivityExecutionRecord ActivityExecution(
        string id,
        string workflowInstanceId,
        string activityId,
        ActivityStatus status,
        DateTimeOffset? completedAt = null) =>
        new()
        {
            Id = id,
            TenantId = "tenant-a",
            WorkflowInstanceId = workflowInstanceId,
            ActivityId = activityId,
            ActivityNodeId = $"node-{activityId}",
            ActivityType = "Elsa.WriteLine",
            ActivityTypeVersion = 1,
            ActivityName = activityId,
            Status = status,
            StartedAt = StartedAt,
            CompletedAt = completedAt
        };

    private static WorkflowExecutionLogRecord ExecutionLog(
        string id,
        string workflowInstanceId,
        string activityId,
        string eventName,
        string activityType = "Elsa.WriteLine",
        DateTimeOffset? timestamp = null,
        long sequence = 0) =>
        new()
        {
            Id = id,
            TenantId = "tenant-a",
            WorkflowDefinitionId = "workflow-1",
            WorkflowDefinitionVersionId = "v1",
            WorkflowInstanceId = workflowInstanceId,
            WorkflowVersion = 1,
            ActivityInstanceId = $"ai-{id}",
            ActivityId = activityId,
            ActivityType = activityType,
            ActivityTypeVersion = 1,
            ActivityNodeId = $"node-{activityId}",
            Timestamp = timestamp ?? StartedAt,
            Sequence = sequence,
            EventName = eventName
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
