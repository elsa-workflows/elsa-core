using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Api.Endpoints.WorkflowInstances.Import;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.State;
using NSubstitute;

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

    [Fact]
    public async Task Import_WhenBookmarkHasForeignId_Refuses_AndForeignRecordTenantIdIsStampedToWriter()
    {
        await using var scenario = await WorkflowStoreScenario.CreateSqliteAsync("tenant-a");
        await scenario.Bookmarks.SaveManyAsync([Bookmark("foreign-bookmark", "tenant-a", "original")], CancellationToken.None);

        using (scenario.UseTenant("tenant-b"))
        {
            var endpoint = CreateImportEndpoint(scenario);
            var serializer = new ConformanceSafeSerializer();

            await ImportAsync(endpoint, new ExportedWorkflowState(
                JsonSerializer.SerializeToElement(new { }),
                Bookmarks: null,
                ActivityExecutionRecords: serializer.SerializeToElement(new[]
                {
                    ActivityExecution("ae-forged", "tenant-a")
                }),
                WorkflowExecutionLogRecords: serializer.SerializeToElement(new[]
                {
                    ExecutionLog("el-forged", "tenant-a")
                })));

            var activity = Assert.Single(await scenario.ActivityExecutions.FindManyAsync(new ActivityExecutionRecordFilter { Id = "ae-forged" }));
            Assert.Equal("tenant-b", activity.TenantId);

            var log = Assert.Single((await scenario.ExecutionLogs.FindManyAsync(new WorkflowExecutionLogRecordFilter { Id = "el-forged" }, Elsa.Common.Models.PageArgs.All)).Items);
            Assert.Equal("tenant-b", log.TenantId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ImportAsync(endpoint, new ExportedWorkflowState(
                    JsonSerializer.SerializeToElement(new { }),
                    Bookmarks: ForeignBookmarkElement("foreign-bookmark"),
                    ActivityExecutionRecords: null,
                    WorkflowExecutionLogRecords: null)));
        }

        var remaining = await FindBookmarkAsync(scenario, "foreign-bookmark");
        AssertUnchanged(remaining, "tenant-a", "original");
    }

    [Fact]
    public async Task Import_StampsActivityAndLogRecordsWithImportingTenant_OnNonStampingStore()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-b");
        IReadOnlyList<ActivityExecutionRecord>? savedActivities = null;
        IReadOnlyList<WorkflowExecutionLogRecord>? savedLogs = null;
        var activityStore = Substitute.For<IActivityExecutionStore>();
        activityStore.SaveManyAsync(Arg.Do<IEnumerable<ActivityExecutionRecord>>(records => savedActivities = records.ToList()), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var logStore = Substitute.For<IWorkflowExecutionLogStore>();
        logStore.SaveManyAsync(Arg.Do<IEnumerable<WorkflowExecutionLogRecord>>(records => savedLogs = records.ToList()), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var serializer = new ConformanceSafeSerializer();

        await ImportAsync(CreateImportEndpoint(tenantAccessor, activityStore, logStore), new ExportedWorkflowState(
            JsonSerializer.SerializeToElement(new { }),
            Bookmarks: null,
            ActivityExecutionRecords: serializer.SerializeToElement(new[] { ActivityExecution("ae-1", "forged") }),
            WorkflowExecutionLogRecords: serializer.SerializeToElement(new[] { ExecutionLog("el-1", "forged") })));

        Assert.Equal("tenant-b", Assert.Single(savedActivities!).TenantId);
        Assert.Equal("tenant-b", Assert.Single(savedLogs!).TenantId);
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

    private static ActivityExecutionRecord ActivityExecution(string id, string? tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityType = "Elsa.WriteLine",
            ActivityTypeVersion = 1,
            Status = Workflows.ActivityStatus.Running,
            StartedAt = CreatedAt
        };

    private static WorkflowExecutionLogRecord ExecutionLog(string id, string? tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            WorkflowDefinitionId = "workflow-1",
            WorkflowDefinitionVersionId = "v1",
            WorkflowInstanceId = "instance-1",
            WorkflowVersion = 1,
            ActivityInstanceId = "ai-1",
            ActivityId = "activity-1",
            ActivityType = "Elsa.WriteLine",
            ActivityTypeVersion = 1,
            ActivityNodeId = "node-1",
            Timestamp = CreatedAt,
            EventName = "Started"
        };

    private static Import CreateImportEndpoint(WorkflowStoreScenario scenario) =>
        CreateImportEndpoint(scenario.TenantAccessor, scenario.ActivityExecutions, scenario.ExecutionLogs, scenario.Bookmarks);

    private static Import CreateImportEndpoint(
        ITenantAccessor tenantAccessor,
        IActivityExecutionStore activityExecutions,
        IWorkflowExecutionLogStore executionLogs,
        IBookmarkStore? bookmarks = null)
    {
        var stateSerializer = Substitute.For<IWorkflowStateSerializer>();
        stateSerializer.Deserialize(Arg.Any<JsonElement>()).Returns(new WorkflowState
        {
            Id = "instance-1",
            DefinitionId = "workflow-1",
            DefinitionVersionId = "v1"
        });

        var instanceManager = Substitute.For<IWorkflowInstanceManager>();
        instanceManager.SaveAsync(Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>())
            .Returns(new WorkflowInstance { Id = "instance-1" });

        return new Import(
            instanceManager,
            Substitute.For<IWorkflowInstanceStore>(),
            activityExecutions,
            executionLogs,
            bookmarks ?? Substitute.For<IBookmarkStore>(),
            stateSerializer,
            new ConformancePayloadSerializer(),
            new ConformanceSafeSerializer(),
            tenantAccessor);
    }

    private static async Task ImportAsync(Import endpoint, ExportedWorkflowState model)
    {
        var method = typeof(Import).GetMethod("ImportSingleWorkflowInstanceAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await (Task)method.Invoke(endpoint, [model, CancellationToken.None])!;
    }

    private static JsonElement ForeignBookmarkElement(string id) =>
        JsonSerializer.SerializeToElement(new[]
        {
            new
            {
                id,
                activityTypeName = "Elsa.HttpEndpoint",
                workflowInstanceId = "instance-1",
                activityInstanceId = (string?)null,
                hash = "stolen",
                correlationId = (string?)null,
                createdAt = CreatedAt,
                payload = new { },
                metadata = new Dictionary<string, string>()
            }
        });

    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private sealed class ConformancePayloadSerializer : IPayloadSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public string Serialize(object payload) => JsonSerializer.Serialize(payload, Options);
        public JsonElement SerializeToElement(object payload) => JsonSerializer.SerializeToElement(payload, Options);
        public object Deserialize(string serializedData) => JsonSerializer.Deserialize<object>(serializedData, Options)!;
        public object Deserialize(string serializedData, Type type) => JsonSerializer.Deserialize(serializedData, type, Options)!;
        public object Deserialize(JsonElement serializedData) => serializedData.Deserialize<object>(Options)!;
        public T Deserialize<T>(string serializedData) => JsonSerializer.Deserialize<T>(serializedData, Options)!;
        public T Deserialize<T>(JsonElement serializedData) => serializedData.Deserialize<T>(Options)!;
        public JsonSerializerOptions GetOptions() => Options;
    }

    private sealed class ConformanceSafeSerializer : ISafeSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ValueTask<string> SerializeAsync(object? value, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Serialize(value));

        public ValueTask<JsonElement> SerializeToElementAsync(object? value, CancellationToken cancellationToken = default) =>
            new(SerializeToElement(value));

        public ValueTask<T> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default) =>
            new(Deserialize<T>(json));

        public ValueTask<T> DeserializeAsync<T>(JsonElement element, CancellationToken cancellationToken = default) =>
            new(Deserialize<T>(element));

        public string Serialize(object? value) => JsonSerializer.Serialize(value, Options);
        public JsonElement SerializeToElement(object? value) => JsonSerializer.SerializeToElement(value, Options);
        public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;
        public T Deserialize<T>(JsonElement element) => element.Deserialize<T>(Options)!;
        public JsonSerializerOptions GetOptions() => Options;
    }
}
