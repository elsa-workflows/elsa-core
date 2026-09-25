using System.Text.Json;
using Dapper;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// <see cref="DapperWorkflowDefinitionStore.TryUpdateLatestAsync"/> is the compare-and-swap the BPMN document
/// PUT uses: load, match, apply, save. Lost match is Conflict, not an overwrite.
/// </summary>
public sealed class DapperWorkflowDefinitionStoreCompareAndSwapTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-definitions-{Guid.NewGuid():N}.db");
    private readonly DapperWorkflowDefinitionStore _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public DapperWorkflowDefinitionStoreCompareAndSwapTests()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
        var connectionProvider = new SqliteDbConnectionProvider(connectionString);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
                          create table WorkflowDefinitions (
                              Id text not null primary key,
                              DefinitionId text not null,
                              Name text null,
                              ToolVersion text null,
                              Description text null,
                              ProviderName text null,
                              MaterializerName text not null,
                              MaterializerContext text null,
                              Props text not null,
                              UsableAsActivity integer null,
                              StringData text null,
                              BinaryData blob null,
                              CreatedAt text not null,
                              Version integer not null,
                              IsLatest integer not null,
                              IsReadonly integer not null,
                              IsPublished integer not null,
                              IsSystem integer null,
                              TenantId text null
                          );
                          """);

        var store = new Store<WorkflowDefinitionRecord>(connectionProvider, _tenantAccessor, "WorkflowDefinitions");
        _store = new DapperWorkflowDefinitionStore(store, new JsonPayloadSerializer());
    }

    [Fact(DisplayName = "A missing definition is NotFound")]
    public async Task TryUpdateLatestAsync_WhenNothingMatchesTheFilter_ReturnsNotFound()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await _store.TryUpdateLatestAsync(
            LatestOf("missing"),
            _ => true,
            current => current);

        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, result.Outcome);
        Assert.Null(result.Definition);
    }

    [Fact(DisplayName = "A match that fails is Conflict and the stored row is unchanged")]
    public async Task TryUpdateLatestAsync_WhenTheRowDoesNotMatch_ReturnsConflictAndWritesNothing()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _store.SaveAsync(Definition("def-1", "id-1", name: "Original", stringData: "graph-v2"));

        var result = await _store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                var next = loaded.ShallowClone();
                next.StringData = "should-not-be-saved";
                return next;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Definition);

        var stored = await _store.FindAsync(LatestOf("def-1"));
        Assert.Equal("graph-v2", stored!.StringData);
        Assert.Equal("Original", stored.Name);
    }

    [Fact(DisplayName = "A loaded row that is no longer IsLatest is Conflict and writes nothing")]
    public async Task TryUpdateLatestAsync_WhenTheLoadedRowIsNoLongerLatest_ReturnsConflictAndWritesNothing()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var published = Definition("def-1", "id-1", name: "Published", stringData: "graph-v1");
        published.IsPublished = true;
        await _store.SaveAsync(published);

        var winner = await _store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            _ => true,
            loaded =>
            {
                var draft = loaded.ShallowClone();
                draft.Id = "id-2";
                draft.Version = loaded.Version + 1;
                draft.IsPublished = false;
                draft.StringData = "winner-draft";
                return draft;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, winner.Outcome);

        var loser = await _store.TryUpdateLatestAsync(
            new WorkflowDefinitionFilter { Id = published.Id },
            _ => true,
            loaded =>
            {
                var draft = loaded.ShallowClone();
                draft.Id = "id-3";
                draft.Version = loaded.Version + 1;
                draft.IsPublished = false;
                draft.StringData = "should-not-be-saved";
                return draft;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, loser.Outcome);
        Assert.Null(loser.Definition);

        var stored = await _store.FindAsync(LatestOf("def-1"));
        Assert.Equal("id-2", stored!.Id);
        Assert.Equal("winner-draft", stored.StringData);
    }

    [Fact(DisplayName = "A matching latest row is updated and the callback sees that just-loaded row")]
    public async Task TryUpdateLatestAsync_WhenTheRowMatches_SavesTheUpdateBuiltFromTheLoadedRow()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _store.SaveAsync(Definition("def-1", "id-1", name: "Original", stringData: "graph-v1"));

        var result = await _store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                var next = loaded.ShallowClone();
                next.StringData = "graph-v2";
                next.Name = loaded.Name + "-kept";
                return next;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal("graph-v2", result.Definition!.StringData);
        Assert.Equal("Original-kept", result.Definition.Name);

        var stored = await _store.FindAsync(LatestOf("def-1"));
        Assert.Equal("graph-v2", stored!.StringData);
        Assert.Equal("Original-kept", stored.Name);
        Assert.Equal("id-1", stored.Id);
    }

    [Fact(DisplayName = "A new draft from a published version unmarks the old latest")]
    public async Task TryUpdateLatestAsync_WhenANewDraftIsCreated_UnmarksThePreviousLatest()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var published = Definition("def-1", "id-1", name: "Published", stringData: "graph-v1");
        published.IsPublished = true;
        await _store.SaveAsync(published);

        var result = await _store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            _ => true,
            loaded =>
            {
                var draft = loaded.ShallowClone();
                draft.Id = "id-2";
                draft.Version = loaded.Version + 1;
                draft.IsPublished = false;
                draft.StringData = "draft-graph";
                return draft;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal("id-2", result.Definition!.Id);

        var latest = await _store.FindAsync(LatestOf("def-1"));
        Assert.Equal("id-2", latest!.Id);
        Assert.True(latest.IsLatest);

        var versions = (await _store.FindManyAsync(new WorkflowDefinitionFilter { DefinitionId = "def-1" })).ToList();
        Assert.Equal(2, versions.Count);
        Assert.Equal(1, versions.Count(x => x.IsLatest));
        Assert.False(versions.Single(x => x.Id == "id-1").IsLatest);
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private static WorkflowDefinitionFilter LatestOf(string definitionId) =>
        new() { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };

    private static WorkflowDefinition Definition(string definitionId, string id, string name, string stringData) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Name = name,
            StringData = stringData,
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };

    private sealed class JsonPayloadSerializer : IPayloadSerializer
    {
        public string Serialize(object payload) => JsonSerializer.Serialize(payload);

        public JsonElement SerializeToElement(object payload) => JsonSerializer.SerializeToElement(payload);

        public object Deserialize(string serializedData) => JsonSerializer.Deserialize<object>(serializedData)!;

        public object Deserialize(string serializedData, Type type) => JsonSerializer.Deserialize(serializedData, type)!;

        public object Deserialize(JsonElement serializedData) => serializedData.Deserialize<object>()!;

        public T Deserialize<T>(string serializedData) => JsonSerializer.Deserialize<T>(serializedData)!;

        public T Deserialize<T>(JsonElement serializedData) => serializedData.Deserialize<T>()!;

        public JsonSerializerOptions GetOptions() => new();
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
