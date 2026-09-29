using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// <see cref="MongoWorkflowDefinitionStore.TryUpdateLatestAsync"/> is the compare-and-swap the BPMN document
/// PUT uses: load, match, apply, a snapshot-filtered write. Lost match is Conflict, not an overwrite.
/// </summary>
public sealed class MongoWorkflowDefinitionStoreCompareAndSwapTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
    private readonly TestTenantAccessor _tenantAccessor = new();
    private MongoClient? _client;
    private IMongoCollection<WorkflowDefinition> _collection = null!;
    private MongoWorkflowDefinitionStore _store = null!;

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();

            _client = new MongoClient(_container.GetConnectionString());
            var database = _client.GetDatabase($"elsa-definitions-{Guid.NewGuid():N}");
            _collection = database.GetCollection<WorkflowDefinition>("workflow_definitions");
            var mongoDbStore = new MongoDbStore<WorkflowDefinition>(_collection, _tenantAccessor);
            _store = new MongoWorkflowDefinitionStore(mongoDbStore);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            _client?.Dispose();
        }
        finally
        {
            await _container.DisposeAsync();
        }
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

    [Fact(DisplayName = "A row that changed after load is Conflict and the stored graph stays")]
    public async Task TryUpdateLatestAsync_WhenTheRowChangedAfterLoad_ReturnsConflictAndWritesNothing()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _store.SaveAsync(Definition("def-1", "id-1", name: "Original", stringData: "graph-v1"));

        var result = await _store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            loaded => loaded.StringData == "graph-v1",
            loaded =>
            {
                _collection.UpdateOne(
                    Builders<WorkflowDefinition>.Filter.Eq(x => x.Id, loaded.Id),
                    Builders<WorkflowDefinition>.Update.Set(x => x.StringData, "concurrent-write"));

                var next = loaded.ShallowClone();
                next.StringData = "stale-overwrite";
                return next;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Definition);

        var stored = await _store.FindAsync(LatestOf("def-1"));
        Assert.Equal("concurrent-write", stored!.StringData);
        Assert.Equal("Original", stored.Name);
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
