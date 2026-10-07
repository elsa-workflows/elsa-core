using System.Diagnostics;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

[CollectionDefinition(nameof(MongoReplicaSetCollection), DisableParallelization = true)]
public sealed class MongoReplicaSetCollection : ICollectionFixture<MongoReplicaSetFixture>
{
}

public sealed class MongoReplicaSetFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7.0.24")
        .WithReplicaSet("rs1")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
            await MongoContainerProof.VerifyAndCaptureAsync(_container, "rs1");
        }
        catch
        {
            await _container.DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[Collection(nameof(MongoReplicaSetCollection))]
public sealed class MongoWorkflowDefinitionStoreCompareAndSwapTests : IDisposable
{
    private readonly TestTenantAccessor _tenantAccessor = new();
    private readonly MongoClient _client;
    private readonly IMongoCollection<WorkflowDefinition> _collection;
    private readonly MongoWorkflowDefinitionStore _store;

    public MongoWorkflowDefinitionStoreCompareAndSwapTests(MongoReplicaSetFixture fixture)
    {
        var settings = MongoClientSettings.FromConnectionString(fixture.ConnectionString);
        settings.ReadPreference = ReadPreference.Nearest;
        _client = new MongoClient(settings);
        var database = _client.GetDatabase($"elsa-cas-{Guid.NewGuid():N}");
        _collection = database.GetCollection<WorkflowDefinition>("workflow_definitions");
        var genericStore = new MongoDbStore<WorkflowDefinition>(_collection, _tenantAccessor);
        _store = new MongoWorkflowDefinitionStore(genericStore);
        _collection.Indexes.CreateOne(
            new CreateIndexModel<WorkflowDefinition>(
                Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.DefinitionId).Ascending(x => x.Version),
                new CreateIndexOptions { Unique = true }));
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task TryUpdateLatestAsync_WhenNothingMatchesTheFilter_ReturnsNotFound()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var callbackCalls = 0;

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("missing"),
            _ =>
            {
                callbackCalls++;
                return true;
            },
            current =>
            {
                callbackCalls++;
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, result.Outcome);
        Assert.Null(result.Definition);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenTheRowDoesNotMatch_ReturnsConflictAndWritesNothing()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", "tenant-a"));
        var updateCalls = 0;

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => false,
            current =>
            {
                updateCalls++;
                current.StringData = "should not be saved";
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Definition);
        Assert.Equal(0, updateCalls);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal("initial graph", stored.StringData);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenTheLoadedRowIsNoLongerLatest_ReturnsConflictAndWritesNothing()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var previous = Definition("previous", "tenant-a");
        previous.IsLatest = false;
        await _collection.InsertOneAsync(previous);
        var latest = Definition("latest", "tenant-a");
        latest.Version = 2;
        await _collection.InsertOneAsync(latest);
        var callbackCalls = 0;

        var result = await _store.TryUpdateLatestAsync(
            new WorkflowDefinitionFilter { Id = previous.Id },
            _ =>
            {
                callbackCalls++;
                return true;
            },
            current =>
            {
                callbackCalls++;
                current.StringData = "should not be saved";
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Definition);
        Assert.Equal(0, callbackCalls);
        var stored = await _collection.Find(x => x.Id == "previous").SingleAsync();
        Assert.Equal("initial graph", stored.StringData);
        Assert.False(stored.IsLatest);
        Assert.Equal("latest", (await _store.FindAsync(LatestFilter("definition")))!.Id);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenSnapshotMatches_UpdatesOnceUsingPrimaryTransaction()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", tenantId: "tenant-a"));
        var matchCalls = 0;
        var updateCalls = 0;

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            current =>
            {
                matchCalls++;
                return current.Name == "initial";
            },
            current =>
            {
                updateCalls++;
                current.StringData = "updated";
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal(1, matchCalls);
        Assert.Equal(1, updateCalls);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal("updated", stored.StringData);
        Assert.Equal("tenant-a", stored.TenantId);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenTwoWritersReadTheSameSnapshot_OnlyOneWins()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", tenantId: "tenant-a"));
        using var start = new ManualResetEventSlim();
        using var bothMatched = new Barrier(2);
        var matchCalls = 0;
        var updateCalls = 0;

        var attempts = Enumerable.Range(1, 2).Select(writer => Task.Run(async () =>
        {
            Assert.True(start.Wait(TimeSpan.FromSeconds(10)));
            return await _store.TryUpdateLatestAsync(
                LatestFilter("definition"),
                current =>
                {
                    Interlocked.Increment(ref matchCalls);
                    return bothMatched.SignalAndWait(TimeSpan.FromSeconds(15)) && current.StringData == "initial graph";
                },
                current =>
                {
                    Interlocked.Increment(ref updateCalls);
                    current.StringData = $"writer-{writer}";
                    return current;
                });
        })).ToArray();

        start.Set();
        var results = await Task.WhenAll(attempts).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(2, matchCalls);
        Assert.Equal(2, updateCalls);
        Assert.Single(results, result => result.Outcome == WorkflowDefinitionUpdateOutcome.Updated);
        Assert.Single(results, result => result.Outcome == WorkflowDefinitionUpdateOutcome.Conflict);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Contains(stored.StringData, new[] { "writer-1", "writer-2" });
        Assert.Equal("tenant-a", stored.TenantId);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenUnlistedMetadataChangesAfterRead_ReturnsConflictAndKeepsConcurrentMetadata()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", tenantId: "tenant-a"));
        var matchesCalls = 0;
        var updateCalls = 0;

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            current =>
            {
                matchesCalls++;
                // CustomProperties was omitted from the original partial snapshot guard.
                _collection.UpdateOne(
                    x => x.Id == current.Id,
                    Builders<WorkflowDefinition>.Update.Set(x => x.CustomProperties, new Dictionary<string, object> { ["owner"] = "concurrent writer" }));
                return true;
            },
            current =>
            {
                updateCalls++;
                current.StringData = "stale graph";
                return current;
            });

        Assert.True(
            result.Outcome == WorkflowDefinitionUpdateOutcome.Conflict,
            "Atomic metadata guard must reject a stale full-document snapshot.");
        Assert.Equal(1, matchesCalls);
        Assert.Equal(1, updateCalls);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal("concurrent writer", stored.CustomProperties["owner"]);
        Assert.Equal("initial graph", stored.StringData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TryUpdateLatestAsync_WhenGraphOrNameChangesAfterRead_ReturnsConflictAndKeepsConcurrentWrite(bool changeName)
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", "tenant-a"));

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current =>
            {
                var concurrentUpdate = changeName
                    ? Builders<WorkflowDefinition>.Update.Set(x => x.Name, "concurrent name")
                    : Builders<WorkflowDefinition>.Update.Set(x => x.StringData, "concurrent graph");
                _collection.UpdateOne(x => x.Id == current.Id, concurrentUpdate);
                current.StringData = "stale graph";
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Definition);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal(changeName ? "concurrent name" : "initial", stored.Name);
        Assert.Equal(changeName ? "initial graph" : "concurrent graph", stored.StringData);
    }

    [Theory]
    [InlineData(112, true)]
    [InlineData(91, false)]
    public void TryUpdateLatestAsync_ClassifiesOnlyWriteConflictCodeAsAbortedConflict(int code, bool expected)
    {
        var exception = CommandException(code);
        exception.AddErrorLabel("TransientTransactionError");
        var classifier = typeof(MongoWorkflowDefinitionStore).GetMethod(
            "IsKnownAbortedConflict",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(classifier);
        var actual = (bool)classifier.Invoke(null, [exception])!;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TryUpdateLatestAsync_WhenCallbackThrowsTransientMongoError_PropagatesAndLeavesDocumentUnchanged(bool throwFromUpdate)
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", tenantId: "tenant-a"));
        // Code 112 from a user callback must propagate, even though a driver write conflict is a Conflict result.
        var callbackException = CommandException(112);
        callbackException.AddErrorLabel("TransientTransactionError");
        var matchCalls = 0;
        var updateCalls = 0;

        var actual = await Assert.ThrowsAsync<MongoCommandException>(() => _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            current =>
            {
                matchCalls++;
                if (!throwFromUpdate)
                {
                    throw callbackException;
                }

                return true;
            },
            current =>
            {
                updateCalls++;
                if (throwFromUpdate)
                {
                    throw callbackException;
                }

                return current;
            }));

        Assert.Same(callbackException, actual);
        Assert.Equal(1, matchCalls);
        Assert.Equal(throwFromUpdate ? 1 : 0, updateCalls);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal("initial graph", stored.StringData);
        Assert.Equal("tenant-a", stored.TenantId);
    }

    [Theory]
    [InlineData("tenant-a")]
    [InlineData(null)]
    public async Task TryUpdateLatestAsync_WhenTenantIsNotVisible_ReturnsNotFound_AndTenantAgnosticMutationPreservesOwner(string? owner)
    {
        await _collection.InsertOneAsync(Definition("latest", owner));
        using var tenantB = _tenantAccessor.PushContext(new Tenant { Id = "tenant-b" });
        var hidden = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current => current);

        Assert.Equal(WorkflowDefinitionUpdateOutcome.NotFound, hidden.Outcome);

        var sharedFilter = LatestFilter("definition");
        sharedFilter.TenantAgnostic = true;
        var visible = await _store.TryUpdateLatestAsync(
            sharedFilter,
            _ => true,
            current =>
            {
                current.Name = "updated across tenants";
                current.TenantId = "tenant-b";
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, visible.Outcome);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal(owner, stored.TenantId);
        Assert.Equal("updated across tenants", stored.Name);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenSharedRowIsVisible_PreservesSharedOwner()
    {
        await _collection.InsertOneAsync(Definition("latest", Tenant.AgnosticTenantId));
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current =>
            {
                current.TenantId = "tenant-a";
                current.StringData = "updated shared graph";
                return current;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal(Tenant.AgnosticTenantId, result.Definition!.TenantId);
        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal(Tenant.AgnosticTenantId, stored.TenantId);
        Assert.Equal("updated shared graph", stored.StringData);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenPublishedVersionCreatesDraft_UpdatesBothRowsAndPreservesTenant()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var published = Definition("published", tenantId: "tenant-a");
        published.IsPublished = true;
        await _collection.InsertOneAsync(published);
        var rawCollection = _collection.Database.GetCollection<BsonDocument>("workflow_definitions");
        var originalPublishedDocument = await rawCollection.Find(x => x["_id"] == "published").SingleAsync();
        var updateCalls = 0;

        var result = await _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current =>
            {
                updateCalls++;
                var draft = current.ShallowClone();
                draft.Id = "new-draft";
                draft.Version = 2;
                // A new draft is always latest, even if the callback returns it unmarked.
                draft.IsLatest = false;
                draft.IsPublished = false;
                draft.TenantId = "tenant-b";
                return draft;
            });

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, result.Outcome);
        Assert.Equal(1, updateCalls);
        var storedPublished = await _collection.Find(x => x.Id == "published").SingleAsync();
        var storedPublishedDocument = await rawCollection.Find(x => x["_id"] == "published").SingleAsync();
        var expectedPublishedDocument = originalPublishedDocument.DeepClone().AsBsonDocument;
        expectedPublishedDocument["IsLatest"] = false;
        var storedDraft = await _collection.Find(x => x.Id == "new-draft").SingleAsync();
        Assert.Equal(expectedPublishedDocument, storedPublishedDocument);
        Assert.False(storedPublished.IsLatest);
        Assert.True(storedPublished.IsPublished);
        Assert.True(storedDraft.IsLatest);
        Assert.False(storedDraft.IsPublished);
        Assert.Equal(2, storedDraft.Version);
        Assert.Equal("tenant-a", storedDraft.TenantId);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenPublishedVersionHasUnknownBsonField_RejectsWithoutMutation()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var published = Definition("published", tenantId: "tenant-a");
        published.IsPublished = true;
        var rawCollection = _collection.Database.GetCollection<BsonDocument>("workflow_definitions");
        var publishedDocument = published.ToBsonDocument();
        publishedDocument["LegacyExtra"] = new BsonDocument("source", "older Elsa process");
        await rawCollection.InsertOneAsync(publishedDocument);

        await Assert.ThrowsAsync<FormatException>(() => _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current =>
            {
                var draft = current.ShallowClone();
                draft.Id = "new-draft";
                draft.Version = 2;
                draft.IsLatest = true;
                draft.IsPublished = false;
                return draft;
            }));

        var stored = await rawCollection.Find(x => x["_id"] == "published").SingleAsync();
        Assert.Equal(publishedDocument, stored);
        Assert.False(await rawCollection.Find(x => x["_id"] == "new-draft").AnyAsync());
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenDraftInsertFails_RollsBackLatestUnmark()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        var published = Definition("published", tenantId: "tenant-a");
        published.IsPublished = true;
        await _collection.InsertOneAsync(published);
        var conflictingVersion = Definition("existing-version-2", tenantId: "tenant-a");
        conflictingVersion.Version = 2;
        conflictingVersion.IsLatest = false;
        await _collection.InsertOneAsync(conflictingVersion);
        var updateCalls = 0;

        await Assert.ThrowsAsync<MongoWriteException>(() => _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current =>
            {
                updateCalls++;
                var draft = current.ShallowClone();
                draft.Id = "new-draft";
                draft.Version = 2;
                draft.IsPublished = false;
                draft.IsLatest = true;
                return draft;
            }));

        Assert.Equal(1, updateCalls);
        var storedPublished = await _collection.Find(x => x.Id == "published").SingleAsync();
        Assert.True(storedPublished.IsLatest, "Failed draft insertion must leave prior latest unchanged.");
        Assert.True(storedPublished.IsPublished);
        Assert.False(await _collection.Find(x => x.Id == "new-draft").AnyAsync());
    }

    [Fact]
    public async Task TryUpdateLatestAsync_WhenServerIsStandalone_FailsWithoutMutationOrCallbacks()
    {
        await using var container = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();
        await container.StartAsync();
        await MongoContainerProof.VerifyAndCaptureAsync(container, expectedReplicaSet: null);
        using var client = new MongoClient(container.GetConnectionString());
        var collection = client.GetDatabase($"elsa-cas-{Guid.NewGuid():N}").GetCollection<WorkflowDefinition>("workflow_definitions");
        var store = new MongoWorkflowDefinitionStore(new MongoDbStore<WorkflowDefinition>(collection, _tenantAccessor));
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await collection.InsertOneAsync(Definition("latest", "tenant-a"));
        var callbackCalls = 0;

        var error = await Assert.ThrowsAsync<NotSupportedException>(() => store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ =>
            {
                callbackCalls++;
                return true;
            },
            current =>
            {
                callbackCalls++;
                current.StringData = "should not be saved";
                return current;
            }));

        Assert.Equal("Standalone servers do not support transactions.", error.Message);
        Assert.Equal(0, callbackCalls);
        var stored = await collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal("initial graph", stored.StringData);
        Assert.True(stored.IsLatest);
    }

    [Fact]
    public async Task TryUpdateLatestAsync_CannotChangeLogicalDefinitionId()
    {
        using var tenant = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });
        await _collection.InsertOneAsync(Definition("latest", tenantId: "tenant-a"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.TryUpdateLatestAsync(
            LatestFilter("definition"),
            _ => true,
            current =>
            {
                var next = current.ShallowClone();
                next.DefinitionId = "another-definition";
                return next;
            }));

        var stored = await _collection.Find(x => x.Id == "latest").SingleAsync();
        Assert.Equal("definition", stored.DefinitionId);
        Assert.Equal("initial graph", stored.StringData);
        Assert.Equal("tenant-a", stored.TenantId);
    }

    private static WorkflowDefinitionFilter LatestFilter(string definitionId) => new()
    {
        DefinitionId = definitionId,
        VersionOptions = VersionOptions.Latest
    };

    private static MongoCommandException CommandException(int code) => new(
        new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017)), 1),
        "Synthetic Mongo command failure",
        new BsonDocument("find", "workflow_definitions"),
        new BsonDocument { ["ok"] = 0, ["code"] = code, ["errmsg"] = "Synthetic failure" });

    private static WorkflowDefinition Definition(string id, string? tenantId) => new()
    {
        Id = id,
        DefinitionId = "definition",
        TenantId = tenantId,
        Name = "initial",
        Version = 1,
        IsLatest = true,
        MaterializerName = "test",
        StringData = "initial graph"
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

internal static class MongoContainerProof
{
    public static async Task VerifyAndCaptureAsync(MongoDbContainer container, string? expectedReplicaSet)
    {
        using var client = new MongoClient(container.GetConnectionString());
        var hello = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
        string? replicaSet = null;

        if (expectedReplicaSet is null)
        {
            Assert.False(hello.Contains("setName"));
        }
        else
        {
            replicaSet = hello["setName"].AsString;
            Assert.Equal(expectedReplicaSet, replicaSet);
        }

        var directory = Environment.GetEnvironmentVariable("ELSA_MONGO_PROOF_DIRECTORY");

        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("docker")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        foreach (var argument in new[] { "inspect", "--type", "container", "--format", "{{.Image}}", container.Id })
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        Assert.True(process.Start(), "Container image inspection could not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            throw;
        }

        var imageId = (await output).Trim();
        await error; // Drain stderr without publishing raw Docker diagnostics.
        Assert.True(process.ExitCode == 0, "Container image inspection failed.");
        Assert.Matches("^sha256:[0-9a-f]{64}$", imageId);

        Directory.CreateDirectory(directory);
        var mode = expectedReplicaSet is null ? "standalone" : "replica-set";
        var path = Path.Combine(directory, $"mongo-{mode}-{Guid.NewGuid():N}.json");
        var temporaryPath = path + ".tmp";

        try
        {
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(new { imageId, replicaSet, mode }));
            File.Move(temporaryPath, path);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
